using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AccessUtility.Engine;
using AccessUtility.Models;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AccessUtility.Tests
{
    public class ForensicCarverTests
    {
        private string CreateSynthesizedDatabaseWithDeletedRecords()
        {
            string tempDb = Path.Combine(Path.GetTempPath(), $"forensic_test_{Guid.NewGuid():N}.mdb");
            byte[] data = new byte[2048 * 4];

            // Page 0: Header
            data[0] = 0x00;
            data[1] = 0x01;
            var magic = System.Text.Encoding.ASCII.GetBytes("Standard Jet DB");
            Array.Copy(magic, 0, data, 4, magic.Length);

            // Page 1: PAM
            data[2048] = 0x01;
            data[2049] = 0x01;

            // Page 2: TDEF for 'Employees'
            int tdefOffset = 2048 * 2;
            data[tdefOffset] = 0x02;
            data[tdefOffset + 1] = 0x01;
            BitConverter.GetBytes(10).CopyTo(data, tdefOffset + 8); // 10 rows
            BitConverter.GetBytes((ushort)2).CopyTo(data, tdefOffset + 25); // 2 cols
            BitConverter.GetBytes((ushort)1).CopyTo(data, tdefOffset + 27); // 1 var col
            BitConverter.GetBytes((ushort)1).CopyTo(data, tdefOffset + 29); // 1 fixed col

            // Column 1: EmpID (Long Integer, ColId=1, FixedOffset=0, Length=4)
            int col1Pos = tdefOffset + 45;
            data[col1Pos] = (byte)JetDataType.LongInteger;
            BitConverter.GetBytes((ushort)1).CopyTo(data, col1Pos + 1); // ColId
            BitConverter.GetBytes((ushort)0).CopyTo(data, col1Pos + 3); // VarIdx
            BitConverter.GetBytes((ushort)0).CopyTo(data, col1Pos + 5); // FixedOff
            BitConverter.GetBytes((ushort)4).CopyTo(data, col1Pos + 7); // Len
            data[col1Pos + 9] = 0x00; // flags
            data[col1Pos + 10] = 5;   // name length
            System.Text.Encoding.ASCII.GetBytes("EmpID").CopyTo(data, col1Pos + 11);

            // Column 2: EmpName (Text, ColId=2, VarIdx=0, Length=50, VarLen=true)
            int col2Pos = col1Pos + 16;
            data[col2Pos] = (byte)JetDataType.Text;
            BitConverter.GetBytes((ushort)2).CopyTo(data, col2Pos + 1);
            BitConverter.GetBytes((ushort)0).CopyTo(data, col2Pos + 3);
            BitConverter.GetBytes((ushort)0).CopyTo(data, col2Pos + 5);
            BitConverter.GetBytes((ushort)50).CopyTo(data, col2Pos + 7);
            data[col2Pos + 9] = 0x01; // IsVariableLength
            data[col2Pos + 10] = 7;   // name length
            System.Text.Encoding.ASCII.GetBytes("EmpName").CopyTo(data, col2Pos + 11);

            // Table Name at end of TDEF definition
            int tblNamePos = col2Pos + 18;
            data[tblNamePos] = 9;
            System.Text.Encoding.ASCII.GetBytes("Employees").CopyTo(data, tblNamePos + 1);

            // Page 3: Data Page with Active and Deleted Slot Records
            int dataPageOffset = 2048 * 3;
            data[dataPageOffset] = 0x01;
            data[dataPageOffset + 1] = 0x01;
            BitConverter.GetBytes((uint)2).CopyTo(data, dataPageOffset + 4); // TDEF = 2
            BitConverter.GetBytes((ushort)2).CopyTo(data, dataPageOffset + 8); // 2 slots

            // Record 1 (Active at relative offset 12)
            int rec1Pos = dataPageOffset + 12;
            data[rec1Pos] = 2; // 2 columns
            BitConverter.GetBytes(1001).CopyTo(data, rec1Pos + 1); // EmpID = 1001
            data[rec1Pos + 5] = 0x00; // Null mask
            data[rec1Pos + 6] = 1;    // 1 var column
            BitConverter.GetBytes((ushort)8).CopyTo(data, rec1Pos + 7); // Var offset end = 8
            System.Text.Encoding.ASCII.GetBytes("John Doe").CopyTo(data, rec1Pos + 9);

            // Record 2 (Deleted at relative offset 64)
            int rec2Pos = dataPageOffset + 64;
            data[rec2Pos] = 2; // 2 columns
            BitConverter.GetBytes(1002).CopyTo(data, rec2Pos + 1); // EmpID = 1002
            data[rec2Pos + 5] = 0x00; // Null mask
            data[rec2Pos + 6] = 1;    // 1 var column
            BitConverter.GetBytes((ushort)11).CopyTo(data, rec2Pos + 7); // Var offset end = 11 ("Alice Smith")
            System.Text.Encoding.ASCII.GetBytes("Alice Smith").CopyTo(data, rec2Pos + 9);

            // Slot Directory at end of page 3
            // Slot 0 -> Record 1 (Active offset 12)
            BitConverter.GetBytes((ushort)12).CopyTo(data, dataPageOffset + 2048 - 2);
            // Slot 1 -> Record 2 (Deleted offset 64 with 0x8000 mask: 64 | 0x8000 = 0x8040 = 32832)
            BitConverter.GetBytes((ushort)(64 | 0x8000)).CopyTo(data, dataPageOffset + 2048 - 4);

            File.WriteAllBytes(tempDb, data);
            return tempDb;
        }

        [Fact]
        public void ForensicCarver_Carves_Deleted_Records_With_High_Confidence()
        {
            string dbPath = CreateSynthesizedDatabaseWithDeletedRecords();
            try
            {
                var report = ForensicCarver.CarveDatabase(dbPath);

                Assert.True(report.TotalPagesScanned >= 4);
                Assert.True(report.SalvagedDeletedRowsCount >= 1);
                Assert.True(report.HighConfidenceCount >= 1);

                var delRec = report.CarvedRecords.FirstOrDefault(r => r.IsDeletedSlot);
                Assert.NotNull(delRec);
                Assert.Equal("Employees", delRec.TableName);
                Assert.Equal(3, delRec.PageIndex);
                Assert.Equal(64, delRec.ByteOffset);
                Assert.True(delRec.ConfidenceScore >= 0.80);
                Assert.Equal("High", delRec.ConfidenceRating);

                // Assert recovered values
                Assert.Equal("Alice Smith", delRec["EmpName"]?.ToString());
                Assert.Equal(1002, Convert.ToInt32(delRec["EmpID"]));
            }
            finally
            {
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }

        [Fact]
        public void ForensicCarver_Exports_To_Sqlite_And_Json()
        {
            string dbPath = CreateSynthesizedDatabaseWithDeletedRecords();
            string outSqlite = Path.Combine(Path.GetTempPath(), $"carved_export_{Guid.NewGuid():N}.sqlite");
            string outJson = Path.Combine(Path.GetTempPath(), $"carved_export_{Guid.NewGuid():N}.json");

            try
            {
                var report = ForensicCarver.CarveDatabase(dbPath);

                // Test SQLite Export
                string sqliteResult = ForensicCarver.ExportCarvedRecordsToSqlite(report, outSqlite);
                Assert.True(File.Exists(sqliteResult));

                using (var conn = new SqliteConnection($"Data Source={outSqlite}"))
                {
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT [_Carved_IsDeletedSlot], [_Carved_Confidence], [EmpID], [EmpName] FROM [Carved_Employees] WHERE [EmpID] = '1002';";
                    using var reader = cmd.ExecuteReader();
                    Assert.True(reader.Read());
                    Assert.Equal(1, reader.GetInt32(0)); // IsDeletedSlot = 1
                    Assert.True(reader.GetDouble(1) >= 0.80);
                    Assert.Equal("1002", reader.GetString(2));
                    Assert.Equal("Alice Smith", reader.GetString(3));
                    conn.Close();
                }

                // Test JSON Export
                string jsonResult = ForensicCarver.ExportCarvedRecordsToJson(report, outJson);
                Assert.True(File.Exists(jsonResult));
                string jsonContent = File.ReadAllText(jsonResult);
                using var jsonDoc = JsonDocument.Parse(jsonContent);
                Assert.True(jsonDoc.RootElement.GetProperty("SalvagedDeletedRowsCount").GetInt32() >= 1);
                Assert.Equal("Employees", jsonDoc.RootElement.GetProperty("Records")[0].GetProperty("TableName").GetString());
            }
            finally
            {
                if (File.Exists(dbPath)) File.Delete(dbPath);
                if (File.Exists(outSqlite)) File.Delete(outSqlite);
                if (File.Exists(outJson)) File.Delete(outJson);
            }
        }

        [Fact]
        public void AxAssistant_Interprets_Carve_Queries()
        {
            var plan = AxAssistant.InterpretQuery("Carve all deleted records from database.mdb and salvage data");
            Assert.Contains("carve", plan.ActionSteps);
        }
    }
}
