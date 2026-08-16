using System;
using System.Collections.Generic;
using System.IO;
using AccessUtility.Exporters;
using AccessUtility.Models;
using Xunit;

namespace AccessUtility.Tests
{
    public class SqlMigrationTests
    {
        private AccessDatabase CreateTestDatabase()
        {
            var db = new AccessDatabase { FilePath = "test_migration.mdb" };

            var customers = new AccessTable
            {
                Name = "Customers",
                TdefPage = 2,
                Columns = new List<AccessColumn>
                {
                    new AccessColumn { Name = "CustomerID", DataType = JetDataType.Autonumber, IsAutoNumber = true },
                    new AccessColumn { Name = "CompanyName", DataType = JetDataType.Text, Length = 50 },
                    new AccessColumn { Name = "IsActive", DataType = JetDataType.Boolean },
                    new AccessColumn { Name = "Notes", DataType = JetDataType.Memo },
                    new AccessColumn { Name = "Logo", DataType = JetDataType.Binary }
                }
            };
            customers.Rows.Add(new Dictionary<string, object?>
            {
                ["CustomerID"] = 1,
                ["CompanyName"] = "Acme Corp",
                ["IsActive"] = true,
                ["Notes"] = "Key enterprise account with multiple branches.",
                ["Logo"] = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }
            });
            customers.Rows.Add(new Dictionary<string, object?>
            {
                ["CustomerID"] = 2,
                ["CompanyName"] = "Globex Inc",
                ["IsActive"] = false,
                ["Notes"] = null,
                ["Logo"] = null
            });

            var orders = new AccessTable
            {
                Name = "Orders",
                TdefPage = 3,
                Columns = new List<AccessColumn>
                {
                    new AccessColumn { Name = "OrderID", DataType = JetDataType.Autonumber, IsAutoNumber = true },
                    new AccessColumn { Name = "CustomerID", DataType = JetDataType.LongInteger },
                    new AccessColumn { Name = "OrderDate", DataType = JetDataType.DateTime },
                    new AccessColumn { Name = "Freight", DataType = JetDataType.Currency }
                }
            };
            orders.Rows.Add(new Dictionary<string, object?>
            {
                ["OrderID"] = 101,
                ["CustomerID"] = 1,
                ["OrderDate"] = new DateTime(2026, 8, 15, 10, 30, 0),
                ["Freight"] = 25.50m
            });

            db.Tables.Add(customers);
            db.Tables.Add(orders);
            return db;
        }

        [Fact]
        public void SqlMigration_Generates_Valid_PostgreSql_Script()
        {
            var db = CreateTestDatabase();
            string outPath = Path.Combine(Path.GetTempPath(), $"pg_test_{Guid.NewGuid():N}.sql");

            try
            {
                var options = new SqlMigrationOptions
                {
                    Dialect = SqlDialect.PostgreSql,
                    IncludeForeignKeys = true,
                    UseTransactions = true
                };

                SqlMigrationExporter.ExportDatabase(db, outPath, options);
                Assert.True(File.Exists(outPath));

                string sql = File.ReadAllText(outPath);
                Assert.Contains("-- Target Dialect : POSTGRESQL", sql);
                Assert.Contains("BEGIN;", sql);
                Assert.Contains("DROP TABLE IF EXISTS \"Customers\";", sql);
                Assert.Contains("\"CustomerID\" SERIAL", sql);
                Assert.Contains("\"IsActive\" BOOLEAN", sql);
                Assert.Contains("\"Notes\" TEXT", sql);
                Assert.Contains("\"Logo\" BYTEA", sql);
                Assert.Contains("ALTER TABLE \"Orders\" ADD CONSTRAINT", sql);
                Assert.Contains("REFERENCES \"Customers\"", sql);
                Assert.Contains("INSERT INTO \"Customers\"", sql);
                Assert.Contains("TRUE", sql); // Boolean in PG
                Assert.Contains("'\\xDEADBEEF'", sql); // Hex Bytea in PG
                Assert.Contains("COMMIT;", sql);
            }
            finally
            {
                if (File.Exists(outPath)) File.Delete(outPath);
            }
        }

        [Fact]
        public void SqlMigration_Generates_Valid_MySql_Script()
        {
            var db = CreateTestDatabase();
            string outPath = Path.Combine(Path.GetTempPath(), $"mysql_test_{Guid.NewGuid():N}.sql");

            try
            {
                var options = new SqlMigrationOptions
                {
                    Dialect = SqlDialect.MySql,
                    IncludeForeignKeys = true,
                    UseTransactions = true
                };

                SqlMigrationExporter.ExportDatabase(db, outPath, options);
                Assert.True(File.Exists(outPath));

                string sql = File.ReadAllText(outPath);
                Assert.Contains("-- Target Dialect : MYSQL", sql);
                Assert.Contains("`CustomerID` INT AUTO_INCREMENT", sql);
                Assert.Contains("`IsActive` TINYINT(1)", sql);
                Assert.Contains("`Notes` LONGTEXT", sql);
                Assert.Contains("`Logo` LONGBLOB", sql);
                Assert.Contains("ENGINE=InnoDB DEFAULT CHARSET=utf8mb4", sql);
                Assert.Contains("INSERT INTO `Customers`", sql);
            }
            finally
            {
                if (File.Exists(outPath)) File.Delete(outPath);
            }
        }

        [Fact]
        public void SqlMigration_Generates_Valid_SqlServer_Script()
        {
            var db = CreateTestDatabase();
            string outPath = Path.Combine(Path.GetTempPath(), $"mssql_test_{Guid.NewGuid():N}.sql");

            try
            {
                var options = new SqlMigrationOptions
                {
                    Dialect = SqlDialect.SqlServer,
                    IncludeForeignKeys = true,
                    UseTransactions = true
                };

                SqlMigrationExporter.ExportDatabase(db, outPath, options);
                Assert.True(File.Exists(outPath));

                string sql = File.ReadAllText(outPath);
                Assert.Contains("-- Target Dialect : SQLSERVER", sql);
                Assert.Contains("BEGIN TRANSACTION;", sql);
                Assert.Contains("[CustomerID] INT IDENTITY(1,1)", sql);
                Assert.Contains("[IsActive] BIT", sql);
                Assert.Contains("[Notes] NVARCHAR(MAX)", sql);
                Assert.Contains("[Logo] VARBINARY(MAX)", sql);
                Assert.Contains("SET IDENTITY_INSERT [Customers] ON;", sql);
                Assert.Contains("SET IDENTITY_INSERT [Customers] OFF;", sql);
                Assert.Contains("0xDEADBEEF", sql);
            }
            finally
            {
                if (File.Exists(outPath)) File.Delete(outPath);
            }
        }

        [Fact]
        public void SqlMigration_Generates_Valid_Oracle_Script()
        {
            var db = CreateTestDatabase();
            string outPath = Path.Combine(Path.GetTempPath(), $"oracle_test_{Guid.NewGuid():N}.sql");

            try
            {
                var options = new SqlMigrationOptions
                {
                    Dialect = SqlDialect.Oracle,
                    IncludeForeignKeys = true
                };

                SqlMigrationExporter.ExportDatabase(db, outPath, options);
                Assert.True(File.Exists(outPath));

                string sql = File.ReadAllText(outPath);
                Assert.Contains("-- Target Dialect : ORACLE", sql);
                Assert.Contains("\"CustomerID\" NUMBER(10) GENERATED ALWAYS AS IDENTITY", sql);
                Assert.Contains("\"IsActive\" NUMBER(1)", sql);
                Assert.Contains("\"Notes\" CLOB", sql);
                Assert.Contains("\"Logo\" BLOB", sql);
            }
            finally
            {
                if (File.Exists(outPath)) File.Delete(outPath);
            }
        }

        [Fact]
        public void SqlMigration_Supports_SchemaOnly_And_DataOnly_Modes()
        {
            var db = CreateTestDatabase();
            string schemaOut = Path.Combine(Path.GetTempPath(), $"schema_only_{Guid.NewGuid():N}.sql");
            string dataOut = Path.Combine(Path.GetTempPath(), $"data_only_{Guid.NewGuid():N}.sql");

            try
            {
                // Schema Only
                SqlMigrationExporter.ExportDatabase(db, schemaOut, new SqlMigrationOptions { Dialect = SqlDialect.PostgreSql, SchemaOnly = true });
                string schemaSql = File.ReadAllText(schemaOut);
                Assert.Contains("CREATE TABLE", schemaSql);
                Assert.DoesNotContain("INSERT INTO", schemaSql);

                // Data Only
                SqlMigrationExporter.ExportDatabase(db, dataOut, new SqlMigrationOptions { Dialect = SqlDialect.PostgreSql, DataOnly = true });
                string dataSql = File.ReadAllText(dataOut);
                Assert.DoesNotContain("CREATE TABLE", dataSql);
                Assert.Contains("INSERT INTO", dataSql);
            }
            finally
            {
                if (File.Exists(schemaOut)) File.Delete(schemaOut);
                if (File.Exists(dataOut)) File.Delete(dataOut);
            }
        }

        [Theory]
        [InlineData("postgres", SqlDialect.PostgreSql)]
        [InlineData("pgsql", SqlDialect.PostgreSql)]
        [InlineData("mysql", SqlDialect.MySql)]
        [InlineData("mariadb", SqlDialect.MySql)]
        [InlineData("mssql", SqlDialect.SqlServer)]
        [InlineData("tsql", SqlDialect.SqlServer)]
        [InlineData("oracle", SqlDialect.Oracle)]
        [InlineData("sqlite", SqlDialect.Sqlite)]
        [InlineData("ansi", SqlDialect.Ansi)]
        public void ParseDialect_Maps_Correctly(string input, SqlDialect expected)
        {
            Assert.Equal(expected, SqlMigrationExporter.ParseDialect(input));
        }
    }
}
