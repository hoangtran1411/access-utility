using System;
using System.IO;
using System.Text;
using AccessUtility.Engine;
using AccessUtility.Models;

namespace AccessUtility.Tests
{
    public static class TestRunner
    {
        public static void CreateSampleDatabase(string mdbPath)
        {
            var db = new AccessDatabase
            {
                FilePath = mdbPath,
                JetVersion = "Jet 3.5 (Access 97)",
                PageSize = 2048
            };

            var table = new AccessTable
            {
                Name = "Customers97",
                TdefPage = 2,
                Columns = new System.Collections.Generic.List<AccessColumn>
                {
                    new AccessColumn { Name = "CustomerID", DataType = JetDataType.LongInteger, ColumnId = 0, FixedOffset = 0, Length = 4, IsVariableLength = false },
                    new AccessColumn { Name = "CompanyName", DataType = JetDataType.Text, ColumnId = 1, VariableIndex = 0, Length = 50, IsVariableLength = true },
                    new AccessColumn { Name = "ContactTitle", DataType = JetDataType.Text, ColumnId = 2, VariableIndex = 1, Length = 30, IsVariableLength = true },
                    new AccessColumn { Name = "Balance", DataType = JetDataType.Currency, ColumnId = 3, FixedOffset = 4, Length = 8, IsVariableLength = false }
                }
            };

            for (int i = 1; i <= 25; i++)
            {
                var row = new System.Collections.Generic.Dictionary<string, object?>
                {
                    ["CustomerID"] = 1000 + i,
                    ["CompanyName"] = $"Legacy Corp {i}",
                    ["ContactTitle"] = i % 2 == 0 ? "Manager" : "Director",
                    ["Balance"] = 150.50m * i
                };
                table.Rows.Add(row);
            }

            db.Tables.Add(table);

            // Write initial header page to bootstrap source file
            byte[] initialBuffer = new byte[2048 * 4]; // 4 pages
            initialBuffer[0] = 0x00;
            initialBuffer[1] = 0x01;
            byte[] magic = Encoding.ASCII.GetBytes("Standard Jet DB\0");
            Array.Copy(magic, 0, initialBuffer, 4, magic.Length);
            initialBuffer[0x14] = 0x01; // Jet 3.5 version byte

            File.WriteAllBytes(mdbPath, initialBuffer);

            // Now perform compact to build full defragmented schema and rows
            Jet3Compactor.Compact(mdbPath, mdbPath, forceUnlock: true);
        }

        public static void CreateSampleLockFile(string mdbPath)
        {
            string ldbPath = Path.ChangeExtension(mdbPath, ".ldb");
            byte[] lockData = new byte[128]; // 2 x 64-byte records

            // Record 1: WORKSTATION1 / Admin
            byte[] comp1 = Encoding.ASCII.GetBytes("WORKSTATION1");
            byte[] user1 = Encoding.ASCII.GetBytes("Admin");
            Array.Copy(comp1, 0, lockData, 0, comp1.Length);
            Array.Copy(user1, 0, lockData, 32, user1.Length);

            // Record 2: LAPTOP-DEV / Hoang
            byte[] comp2 = Encoding.ASCII.GetBytes("LAPTOP-DEV");
            byte[] user2 = Encoding.ASCII.GetBytes("Hoang");
            Array.Copy(comp2, 0, lockData, 64, comp2.Length);
            Array.Copy(user2, 0, lockData, 96, user2.Length);

            File.WriteAllBytes(ldbPath, lockData);
        }
    }
}
