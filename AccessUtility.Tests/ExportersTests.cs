using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AccessUtility.Engine;
using AccessUtility.Exporters;
using AccessUtility.Models;
using DuckDB.NET.Data;
using Xunit;

namespace AccessUtility.Tests
{
    public class ExportersTests
    {
        private AccessDatabase CreateTestDb()
        {
            var db = new AccessDatabase { FilePath = "test.mdb" };
            var table = new AccessTable
            {
                Name = "Customers97",
                TdefPage = 2,
                Columns = new List<AccessColumn>
                {
                    new AccessColumn { Name = "CustomerID", DataType = JetDataType.LongInteger },
                    new AccessColumn { Name = "CompanyName", DataType = JetDataType.Text },
                    new AccessColumn { Name = "Balance", DataType = JetDataType.Currency },
                    new AccessColumn { Name = "IsActive", DataType = JetDataType.Boolean },
                    new AccessColumn { Name = "CreatedDate", DataType = JetDataType.DateTime },
                    new AccessColumn { Name = "Rating", DataType = JetDataType.Double },
                    new AccessColumn { Name = "FlagByte", DataType = JetDataType.Byte },
                    new AccessColumn { Name = "Notes", DataType = JetDataType.Memo }
                }
            };
            table.Rows.Add(new Dictionary<string, object?>
            {
                ["CustomerID"] = 101,
                ["CompanyName"] = "Acme Corp",
                ["Balance"] = 1250.50m,
                ["IsActive"] = true,
                ["CreatedDate"] = new DateTime(2026, 8, 16, 9, 0, 0),
                ["Rating"] = 4.85,
                ["FlagByte"] = (byte)1,
                ["Notes"] = "Priority customer."
            });
            table.Rows.Add(new Dictionary<string, object?>
            {
                ["CustomerID"] = 102,
                ["CompanyName"] = "Beta LLC",
                ["Balance"] = 0.00m,
                ["IsActive"] = false,
                ["CreatedDate"] = new DateTime(2026, 8, 15, 14, 30, 0),
                ["Rating"] = 3.20,
                ["FlagByte"] = (byte)0,
                ["Notes"] = null
            });
            db.Tables.Add(table);
            return db;
        }

        private async IAsyncEnumerable<AccessRow> GetTestRowsAsync(AccessTable table)
        {
            foreach (var r in table.Rows)
            {
                yield return new AccessRow { Values = r };
            }
            await Task.CompletedTask;
        }

        [Fact]
        public void Export_ToSqlite_CreatesValidSqliteFile()
        {
            string sqliteDb = Path.Combine(Path.GetTempPath(), $"export_target_{Guid.NewGuid():N}.sqlite");

            try
            {
                var db = CreateTestDb();
                string outPath = SqliteExporter.ExportDatabase(db, sqliteDb);
                Assert.True(File.Exists(outPath));
                Assert.True(new FileInfo(outPath).Length > 0);
            }
            finally
            {
                if (File.Exists(sqliteDb)) File.Delete(sqliteDb);
            }
        }

        [Fact]
        public void Export_ToSqlScript_CreatesValidSqlFile()
        {
            string sqlScript = Path.Combine(Path.GetTempPath(), $"export_target_{Guid.NewGuid():N}.sql");

            try
            {
                var db = CreateTestDb();
                string outPath = SqlScriptExporter.ExportDatabase(db, sqlScript);
                Assert.True(File.Exists(outPath));
                string content = File.ReadAllText(outPath);
                Assert.Contains("CREATE TABLE", content);
                Assert.Contains("Customers97", content);
            }
            finally
            {
                if (File.Exists(sqlScript)) File.Delete(sqlScript);
            }
        }

        [Fact]
        public void Export_ToCsv_CreatesValidCsvFile()
        {
            string csvFile = Path.Combine(Path.GetTempPath(), $"export_target_{Guid.NewGuid():N}.csv");

            try
            {
                var db = CreateTestDb();
                string outPath = CsvExporter.ExportTable(db.Tables[0], csvFile);
                Assert.True(File.Exists(outPath));
                string content = File.ReadAllText(outPath);
                Assert.Contains("CustomerID", content);
                Assert.Contains("Acme Corp", content);
                Assert.Contains("Beta LLC", content);
            }
            finally
            {
                if (File.Exists(csvFile)) File.Delete(csvFile);
            }
        }

        [Fact]
        public void Export_ToJsonLines_CreatesValidJsonlFile()
        {
            string jsonlFile = Path.Combine(Path.GetTempPath(), $"export_target_{Guid.NewGuid():N}.jsonl");

            try
            {
                var db = CreateTestDb();
                string outPath = JsonLinesExporter.ExportTable(db.Tables[0], jsonlFile);
                Assert.True(File.Exists(outPath));

                var lines = File.ReadAllLines(outPath);
                Assert.Equal(2, lines.Length);

                using var doc1 = JsonDocument.Parse(lines[0]);
                Assert.Equal(101, doc1.RootElement.GetProperty("CustomerID").GetInt32());
                Assert.Equal("Acme Corp", doc1.RootElement.GetProperty("CompanyName").GetString());
                Assert.True(doc1.RootElement.GetProperty("IsActive").GetBoolean());
                Assert.Equal(1250.50m, doc1.RootElement.GetProperty("Balance").GetDecimal());

                using var doc2 = JsonDocument.Parse(lines[1]);
                Assert.Equal(102, doc2.RootElement.GetProperty("CustomerID").GetInt32());
                Assert.Equal("Beta LLC", doc2.RootElement.GetProperty("CompanyName").GetString());
                Assert.False(doc2.RootElement.GetProperty("IsActive").GetBoolean());
                Assert.Equal(JsonValueKind.Null, doc2.RootElement.GetProperty("Notes").ValueKind);
            }
            finally
            {
                if (File.Exists(jsonlFile)) File.Delete(jsonlFile);
            }
        }

        [Fact]
        public async Task Export_ToJsonLines_Stream_Async_Succeeds()
        {
            string jsonlFile = Path.Combine(Path.GetTempPath(), $"export_stream_{Guid.NewGuid():N}.jsonl");

            try
            {
                var db = CreateTestDb();
                var table = db.Tables[0];
                string outPath = await JsonLinesExporter.ExportTableStreamAsync(table, GetTestRowsAsync(table), jsonlFile);
                Assert.True(File.Exists(outPath));

                var lines = File.ReadAllLines(outPath);
                Assert.Equal(2, lines.Length);
            }
            finally
            {
                if (File.Exists(jsonlFile)) File.Delete(jsonlFile);
            }
        }

        [Fact]
        public async Task Export_ToParquet_CreatesValidParquetFile()
        {
            string parquetFile = Path.Combine(Path.GetTempPath(), $"export_target_{Guid.NewGuid():N}.parquet");

            try
            {
                var db = CreateTestDb();
                string outPath = await ParquetExporter.ExportTableAsync(db.Tables[0], parquetFile);
                Assert.True(File.Exists(outPath));
                Assert.True(new FileInfo(outPath).Length > 0);
            }
            finally
            {
                if (File.Exists(parquetFile)) File.Delete(parquetFile);
            }
        }

        [Fact]
        public async Task Export_ToParquet_Stream_Async_Succeeds()
        {
            string parquetFile = Path.Combine(Path.GetTempPath(), $"export_stream_{Guid.NewGuid():N}.parquet");

            try
            {
                var db = CreateTestDb();
                var table = db.Tables[0];
                string outPath = await ParquetExporter.ExportTableStreamAsync(table, GetTestRowsAsync(table), parquetFile);
                Assert.True(File.Exists(outPath));
                Assert.True(new FileInfo(outPath).Length > 0);
            }
            finally
            {
                if (File.Exists(parquetFile)) File.Delete(parquetFile);
            }
        }

        [Fact]
        public void Export_ToDuckDb_CreatesValidDuckDbDatabase()
        {
            string duckDbFile = Path.Combine(Path.GetTempPath(), $"export_target_{Guid.NewGuid():N}.duckdb");

            try
            {
                var db = CreateTestDb();
                string outPath = DuckDbExporter.ExportDatabase(db, duckDbFile);
                Assert.True(File.Exists(outPath));
                Assert.True(new FileInfo(outPath).Length > 0);

                // Verify by connecting to DuckDB and querying the created table
                using var conn = new DuckDBConnection($"Data Source={duckDbFile}");
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM \"Customers97\";";
                var count = Convert.ToInt32(cmd.ExecuteScalar());
                Assert.Equal(2, count);

                using var cmdSelect = conn.CreateCommand();
                cmdSelect.CommandText = "SELECT \"CustomerID\", \"CompanyName\", \"IsActive\" FROM \"Customers97\" ORDER BY \"CustomerID\";";
                using var reader = cmdSelect.ExecuteReader();
                Assert.True(reader.Read());
                Assert.Equal(101, reader.GetInt32(0));
                Assert.Equal("Acme Corp", reader.GetString(1));
                Assert.True(reader.GetBoolean(2));

                Assert.True(reader.Read());
                Assert.Equal(102, reader.GetInt32(0));
                Assert.Equal("Beta LLC", reader.GetString(1));
                Assert.False(reader.GetBoolean(2));

                conn.Close();
            }
            finally
            {
                if (File.Exists(duckDbFile))
                {
                    try { File.Delete(duckDbFile); } catch { }
                }
            }
        }

        [Fact]
        public void AxAssistant_Interprets_Modern_Exporter_Queries()
        {
            var planParquet = AxAssistant.InterpretQuery("Export inventory.mdb to parquet format");
            Assert.Contains("export", planParquet.ActionSteps);
            Assert.Equal("parquet", planParquet.ExportFormat);

            var planDuck = AxAssistant.InterpretQuery("Convert data97.mdb to duckdb");
            Assert.Contains("export", planDuck.ActionSteps);
            Assert.Equal("duckdb", planDuck.ExportFormat);

            var planJsonl = AxAssistant.InterpretQuery("Export legacy.mdb to jsonl lines");
            Assert.Contains("export", planJsonl.ActionSteps);
            Assert.Equal("jsonl", planJsonl.ExportFormat);
        }
    }
}
