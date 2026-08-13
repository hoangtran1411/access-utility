using System;
using System.IO;
using AccessUtility.Engine;
using AccessUtility.Exporters;
using AccessUtility.Models;
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
                Columns = new System.Collections.Generic.List<AccessColumn>
                {
                    new AccessColumn { Name = "CustomerID", DataType = JetDataType.LongInteger },
                    new AccessColumn { Name = "CompanyName", DataType = JetDataType.Text }
                }
            };
            table.Rows.Add(new System.Collections.Generic.Dictionary<string, object?>
            {
                ["CustomerID"] = 101,
                ["CompanyName"] = "Acme Corp"
            });
            db.Tables.Add(table);
            return db;
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
            }
            finally
            {
                if (File.Exists(csvFile)) File.Delete(csvFile);
            }
        }
    }
}
