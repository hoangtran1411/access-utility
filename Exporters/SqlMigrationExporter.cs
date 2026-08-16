using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AccessUtility.Engine;
using AccessUtility.Models;

namespace AccessUtility.Exporters
{
    public enum SqlDialect
    {
        Ansi,
        PostgreSql,
        MySql,
        SqlServer,
        Sqlite,
        Oracle
    }

    public class SqlMigrationOptions
    {
        public SqlDialect Dialect { get; set; } = SqlDialect.Ansi;
        public bool SchemaOnly { get; set; }
        public bool DataOnly { get; set; }
        public bool IncludeDropTable { get; set; } = true;
        public bool IncludeForeignKeys { get; set; } = true;
        public bool IncludeViews { get; set; } = true;
        public bool UseTransactions { get; set; } = true;
        public int BatchSize { get; set; } = 250;
        public string? SchemaName { get; set; }
    }

    public static class SqlMigrationExporter
    {
        public static string ExportDatabase(AccessDatabase db, string outputSqlPath, SqlMigrationOptions? options = null)
        {
            options ??= new SqlMigrationOptions();
            var sb = new StringBuilder();

            WriteHeader(sb, db, options);

            if (options.UseTransactions && options.Dialect != SqlDialect.Oracle)
            {
                sb.AppendLine(options.Dialect == SqlDialect.SqlServer ? "BEGIN TRANSACTION;" : "BEGIN;");
                sb.AppendLine();
            }

            // Phase 1: DDL Schema (Tables, Primary Keys, Constraints)
            if (!options.DataOnly)
            {
                sb.AppendLine("-- ========================================================");
                sb.AppendLine("-- 1. TABLE DEFINITIONS & CONSTRAINTS");
                sb.AppendLine("-- ========================================================");
                sb.AppendLine();

                foreach (var table in db.Tables)
                {
                    if (table.Columns.Count == 0 || table.Name.StartsWith("MSys") || table.Name.StartsWith("~")) continue;
                    WriteTableDdl(sb, table, options);
                }

                // Phase 2: Foreign Key Constraints (Emitted after tables to avoid dependency cycles)
                if (options.IncludeForeignKeys && options.Dialect != SqlDialect.Sqlite)
                {
                    sb.AppendLine("-- ========================================================");
                    sb.AppendLine("-- 2. FOREIGN KEY CONSTRAINTS");
                    sb.AppendLine("-- ========================================================");
                    sb.AppendLine();

                    var erd = ErdGenerator.GenerateErd(db);
                    foreach (var rel in erd.Relationships)
                    {
                        WriteForeignKeyConstraint(sb, rel, options);
                    }
                    sb.AppendLine();
                }

                // Phase 3: Saved Queries / Views
                if (options.IncludeViews)
                {
                    var tempDir = Path.Combine(Path.GetTempPath(), $"queries_{Guid.NewGuid():N}");
                    try
                    {
                        var report = QueryExtractor.ExtractQueries(db, tempDir);
                        if (report.Queries.Count > 0)
                        {
                            sb.AppendLine("-- ========================================================");
                            sb.AppendLine("-- 3. DATABASE VIEWS (EXTRACTED ACCESS QUERIES)");
                            sb.AppendLine("-- ========================================================");
                            sb.AppendLine();

                            foreach (var q in report.Queries)
                            {
                                WriteViewDdl(sb, q, options);
                            }
                            sb.AppendLine();
                        }
                    }
                    finally
                    {
                        if (Directory.Exists(tempDir))
                        {
                            try { Directory.Delete(tempDir, true); } catch { }
                        }
                    }
                }
            }

            // Phase 4: DML Data Inserts (Optimized Batches)
            if (!options.SchemaOnly)
            {
                sb.AppendLine("-- ========================================================");
                sb.AppendLine("-- 4. DATA INSERTS");
                sb.AppendLine("-- ========================================================");
                sb.AppendLine();

                foreach (var table in db.Tables)
                {
                    if (table.Columns.Count == 0 || table.Name.StartsWith("MSys") || table.Name.StartsWith("~")) continue;
                    if (table.Rows.Count == 0) continue;

                    WriteTableData(sb, table, options);
                }
            }

            if (options.UseTransactions && options.Dialect != SqlDialect.Oracle)
            {
                sb.AppendLine("COMMIT;");
                sb.AppendLine();
            }

            string dir = Path.GetDirectoryName(Path.GetFullPath(outputSqlPath)) ?? ".";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllText(outputSqlPath, sb.ToString(), Encoding.UTF8);
            return outputSqlPath;
        }

        public static SqlDialect ParseDialect(string? dialectStr)
        {
            if (string.IsNullOrWhiteSpace(dialectStr)) return SqlDialect.Ansi;

            return dialectStr.Trim().ToLower() switch
            {
                "postgres" or "postgresql" or "pgsql" or "psql" => SqlDialect.PostgreSql,
                "mysql" or "mariadb" => SqlDialect.MySql,
                "mssql" or "sqlserver" or "tsql" => SqlDialect.SqlServer,
                "sqlite" or "sqlite3" => SqlDialect.Sqlite,
                "oracle" or "ora" => SqlDialect.Oracle,
                _ => SqlDialect.Ansi
            };
        }

        private static void WriteHeader(StringBuilder sb, AccessDatabase db, SqlMigrationOptions options)
        {
            sb.AppendLine($"-- ==========================================================================");
            sb.AppendLine($"-- ACCESS 97 SCHEMA & MIGRATION SCRIPT");
            sb.AppendLine($"-- Target Dialect : {options.Dialect.ToString().ToUpper()}");
            sb.AppendLine($"-- Source File    : {Path.GetFileName(db.FilePath ?? "database.mdb")}");
            sb.AppendLine($"-- Export Mode    : {(options.SchemaOnly ? "Schema Only (DDL)" : (options.DataOnly ? "Data Only (DML)" : "Full Schema & Data"))}");
            sb.AppendLine($"-- Generator      : AccessUtility (.NET 10 Native AOT)");
            sb.AppendLine($"-- Timestamp      : {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"-- ==========================================================================");
            sb.AppendLine();

            if (options.Dialect == SqlDialect.MySql)
            {
                sb.AppendLine("SET NAMES utf8mb4;");
                sb.AppendLine("SET FOREIGN_KEY_CHECKS = 0;");
                sb.AppendLine();
            }
            else if (options.Dialect == SqlDialect.PostgreSql)
            {
                sb.AppendLine("SET client_encoding = 'UTF8';");
                sb.AppendLine("SET check_function_bodies = false;");
                sb.AppendLine();
            }
        }

        private static void WriteTableDdl(StringBuilder sb, AccessTable table, SqlMigrationOptions options)
        {
            string quotedTable = QuoteIdentifier(table.Name, options.Dialect, options.SchemaName);

            if (options.IncludeDropTable)
            {
                sb.AppendLine(GetDropTableStatement(table.Name, options));
            }

            sb.AppendLine($"CREATE TABLE {quotedTable} (");

            var colLines = new List<string>();
            AccessColumn? pkCol = table.Columns.FirstOrDefault(c => c.IsAutoNumber) ??
                                 table.Columns.FirstOrDefault(c => c.Name.Equals("ID", StringComparison.OrdinalIgnoreCase) || c.Name.Equals($"{table.Name}ID", StringComparison.OrdinalIgnoreCase));

            for (int i = 0; i < table.Columns.Count; i++)
            {
                var col = table.Columns[i];
                string quotedCol = QuoteIdentifier(col.Name, options.Dialect);
                bool isPk = (col == pkCol);
                string typeDef = MapDataType(col, options.Dialect, isPk);

                string colDef = $"    {quotedCol} {typeDef}";
                if (options.Dialect == SqlDialect.Sqlite && isPk && col.IsAutoNumber)
                {
                    colDef += " PRIMARY KEY AUTOINCREMENT";
                }
                else if (isPk && options.Dialect == SqlDialect.Ansi)
                {
                    colDef += " PRIMARY KEY";
                }

                colLines.Add(colDef);
            }

            // Inline Primary Key constraint for non-SQLite/ANSI if not auto-inlined
            if (pkCol != null && options.Dialect != SqlDialect.Sqlite && options.Dialect != SqlDialect.Ansi)
            {
                string pkQuoted = QuoteIdentifier(pkCol.Name, options.Dialect);
                colLines.Add($"    CONSTRAINT pk_{SanitizeName(table.Name)} PRIMARY KEY ({pkQuoted})");
            }

            sb.AppendLine(string.Join(",\n", colLines));

            string tableSuffix = options.Dialect switch
            {
                SqlDialect.MySql => ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",
                _ => ");"
            };

            sb.AppendLine(tableSuffix);
            sb.AppendLine();
        }

        private static void WriteForeignKeyConstraint(StringBuilder sb, ErdRelationship rel, SqlMigrationOptions options)
        {
            string fromTable = QuoteIdentifier(rel.ToTable, options.Dialect, options.SchemaName);
            string toTable = QuoteIdentifier(rel.FromTable, options.Dialect, options.SchemaName);
            string fromCol = QuoteIdentifier(rel.ToColumn, options.Dialect);
            string toCol = QuoteIdentifier(rel.FromColumn, options.Dialect);
            string fkName = $"fk_{SanitizeName(rel.ToTable)}_{SanitizeName(rel.ToColumn)}";

            sb.AppendLine($"ALTER TABLE {fromTable} ADD CONSTRAINT {fkName} FOREIGN KEY ({fromCol}) REFERENCES {toTable} ({toCol});");
        }

        private static void WriteViewDdl(StringBuilder sb, ExtractedQuery q, SqlMigrationOptions options)
        {
            string quotedView = QuoteIdentifier(q.Name, options.Dialect, options.SchemaName);
            sb.AppendLine($"-- View: {q.Name}");
            if (options.IncludeDropTable)
            {
                sb.AppendLine($"DROP VIEW IF EXISTS {quotedView};");
            }

            string cleanSql = (q.SqlText ?? string.Empty).TrimEnd(';', ' ', '\r', '\n');
            if (string.IsNullOrWhiteSpace(cleanSql))
            {
                cleanSql = $"SELECT 1 AS placeholder";
            }
            sb.AppendLine($"CREATE VIEW {quotedView} AS");
            sb.AppendLine(cleanSql + ";");
            sb.AppendLine();
        }

        private static void WriteTableData(StringBuilder sb, AccessTable table, SqlMigrationOptions options)
        {
            string quotedTable = QuoteIdentifier(table.Name, options.Dialect, options.SchemaName);
            var colNames = table.Columns.Select(c => QuoteIdentifier(c.Name, options.Dialect)).ToList();
            string colsHeader = string.Join(", ", colNames);

            sb.AppendLine($"-- Table Data: {table.Name} ({table.Rows.Count} rows)");

            // If SQL Server and Table has Identity, enable IDENTITY_INSERT
            bool hasIdentity = table.Columns.Any(c => c.IsAutoNumber) && options.Dialect == SqlDialect.SqlServer;
            if (hasIdentity)
            {
                sb.AppendLine($"SET IDENTITY_INSERT {quotedTable} ON;");
            }

            int batchSize = Math.Max(1, options.BatchSize);
            for (int r = 0; r < table.Rows.Count; r += batchSize)
            {
                int currentBatchCount = Math.Min(batchSize, table.Rows.Count - r);

                if (options.Dialect == SqlDialect.Oracle)
                {
                    // Oracle individual inserts
                    for (int i = 0; i < currentBatchCount; i++)
                    {
                        var row = table.Rows[r + i];
                        var rowVals = table.Columns.Select(c => {
                            row.TryGetValue(c.Name, out var v);
                            return FormatValue(v, c.DataType, options.Dialect);
                        });
                        sb.AppendLine($"INSERT INTO {quotedTable} ({colsHeader}) VALUES ({string.Join(", ", rowVals)});");
                    }
                }
                else
                {
                    // Standard multi-row batch insert
                    sb.AppendLine($"INSERT INTO {quotedTable} ({colsHeader}) VALUES");
                    for (int i = 0; i < currentBatchCount; i++)
                    {
                        var row = table.Rows[r + i];
                        var rowVals = table.Columns.Select(c => {
                            row.TryGetValue(c.Name, out var v);
                            return FormatValue(v, c.DataType, options.Dialect);
                        });

                        string commaOrSemi = (i == currentBatchCount - 1) ? ";" : ",";
                        sb.AppendLine($"    ({string.Join(", ", rowVals)}){commaOrSemi}");
                    }
                }
            }

            if (hasIdentity)
            {
                sb.AppendLine($"SET IDENTITY_INSERT {quotedTable} OFF;");
            }

            sb.AppendLine();
        }

        public static string QuoteIdentifier(string name, SqlDialect dialect, string? schema = null)
        {
            string clean = name.Replace("\"", "").Replace("`", "").Replace("[", "").Replace("]", "");
            string q = dialect switch
            {
                SqlDialect.MySql => $"`{clean}`",
                SqlDialect.SqlServer => $"[{clean}]",
                _ => $"\"{clean}\""
            };

            if (!string.IsNullOrWhiteSpace(schema))
            {
                string s = dialect switch
                {
                    SqlDialect.MySql => $"`{schema}`",
                    SqlDialect.SqlServer => $"[{schema}]",
                    _ => $"\"{schema}\""
                };
                return $"{s}.{q}";
            }

            return q;
        }

        public static string MapDataType(AccessColumn col, SqlDialect dialect, bool isPk = false)
        {
            return (col.DataType, dialect) switch
            {
                // AutoNumber / Identity
                (JetDataType.Autonumber, SqlDialect.PostgreSql) => isPk ? "SERIAL" : "INTEGER",
                (JetDataType.Autonumber, SqlDialect.MySql) => isPk ? "INT AUTO_INCREMENT" : "INT",
                (JetDataType.Autonumber, SqlDialect.SqlServer) => isPk ? "INT IDENTITY(1,1)" : "INT",
                (JetDataType.Autonumber, SqlDialect.Oracle) => isPk ? "NUMBER(10) GENERATED ALWAYS AS IDENTITY" : "NUMBER(10)",
                (JetDataType.Autonumber, SqlDialect.Sqlite) => "INTEGER",
                (JetDataType.Autonumber, _) => "INT",

                // Boolean
                (JetDataType.Boolean, SqlDialect.PostgreSql) => "BOOLEAN",
                (JetDataType.Boolean, SqlDialect.MySql) => "TINYINT(1)",
                (JetDataType.Boolean, SqlDialect.SqlServer) => "BIT",
                (JetDataType.Boolean, SqlDialect.Sqlite) => "INTEGER",
                (JetDataType.Boolean, SqlDialect.Oracle) => "NUMBER(1)",
                (JetDataType.Boolean, _) => "BOOLEAN",

                // Byte
                (JetDataType.Byte, SqlDialect.PostgreSql) => "SMALLINT",
                (JetDataType.Byte, SqlDialect.MySql) => "TINYINT UNSIGNED",
                (JetDataType.Byte, SqlDialect.SqlServer) => "TINYINT",
                (JetDataType.Byte, SqlDialect.Sqlite) => "INTEGER",
                (JetDataType.Byte, SqlDialect.Oracle) => "NUMBER(3)",
                (JetDataType.Byte, _) => "SMALLINT",

                // Integer (16-bit)
                (JetDataType.Integer, SqlDialect.Oracle) => "NUMBER(5)",
                (JetDataType.Integer, _) => "SMALLINT",

                // Long Integer (32-bit)
                (JetDataType.LongInteger, SqlDialect.Oracle) => "NUMBER(10)",
                (JetDataType.LongInteger, SqlDialect.PostgreSql) => "INTEGER",
                (JetDataType.LongInteger, _) => "INT",

                // Single (Float 32-bit)
                (JetDataType.Single, SqlDialect.PostgreSql) => "REAL",
                (JetDataType.Single, SqlDialect.SqlServer) => "REAL",
                (JetDataType.Single, SqlDialect.Sqlite) => "REAL",
                (JetDataType.Single, SqlDialect.Oracle) => "FLOAT(24)",
                (JetDataType.Single, _) => "FLOAT",

                // Double (Float 64-bit)
                (JetDataType.Double, SqlDialect.PostgreSql) => "DOUBLE PRECISION",
                (JetDataType.Double, SqlDialect.SqlServer) => "FLOAT(53)",
                (JetDataType.Double, SqlDialect.Sqlite) => "REAL",
                (JetDataType.Double, SqlDialect.Oracle) => "FLOAT(53)",
                (JetDataType.Double, _) => "DOUBLE",

                // Currency
                (JetDataType.Currency, SqlDialect.PostgreSql) => "NUMERIC(19,4)",
                (JetDataType.Currency, SqlDialect.SqlServer) => "MONEY",
                (JetDataType.Currency, SqlDialect.Sqlite) => "NUMERIC",
                (JetDataType.Currency, SqlDialect.Oracle) => "NUMBER(19,4)",
                (JetDataType.Currency, _) => "DECIMAL(19,4)",

                // DateTime
                (JetDataType.DateTime, SqlDialect.PostgreSql) => "TIMESTAMP",
                (JetDataType.DateTime, SqlDialect.MySql) => "DATETIME(6)",
                (JetDataType.DateTime, SqlDialect.SqlServer) => "DATETIME2",
                (JetDataType.DateTime, SqlDialect.Sqlite) => "TEXT",
                (JetDataType.DateTime, SqlDialect.Oracle) => "TIMESTAMP",
                (JetDataType.DateTime, _) => "DATETIME",

                // Text
                (JetDataType.Text, SqlDialect.SqlServer) => $"NVARCHAR({Math.Max(1, col.Length)})",
                (JetDataType.Text, SqlDialect.Oracle) => $"VARCHAR2({Math.Max(1, col.Length)})",
                (JetDataType.Text, SqlDialect.Sqlite) => "TEXT",
                (JetDataType.Text, _) => $"VARCHAR({Math.Max(1, col.Length)})",

                // Memo
                (JetDataType.Memo, SqlDialect.MySql) => "LONGTEXT",
                (JetDataType.Memo, SqlDialect.SqlServer) => "NVARCHAR(MAX)",
                (JetDataType.Memo, SqlDialect.Oracle) => "CLOB",
                (JetDataType.Memo, _) => "TEXT",

                // Binary
                (JetDataType.Binary, SqlDialect.PostgreSql) => "BYTEA",
                (JetDataType.Binary, SqlDialect.MySql) => "LONGBLOB",
                (JetDataType.Binary, SqlDialect.SqlServer) => "VARBINARY(MAX)",
                (JetDataType.Binary, SqlDialect.Sqlite) => "BLOB",
                (JetDataType.Binary, SqlDialect.Oracle) => "BLOB",
                (JetDataType.Binary, _) => "BLOB",

                // Default
                _ => "VARCHAR(255)"
            };
        }

        private static string FormatValue(object? val, JetDataType type, SqlDialect dialect)
        {
            if (val == null || val == DBNull.Value) return "NULL";

            if (type == JetDataType.Boolean)
            {
                bool b = Convert.ToBoolean(val);
                if (dialect == SqlDialect.PostgreSql) return b ? "TRUE" : "FALSE";
                return b ? "1" : "0";
            }

            if (type is JetDataType.Integer or JetDataType.LongInteger or JetDataType.Byte or JetDataType.Autonumber)
            {
                return val.ToString() ?? "0";
            }

            if (type is JetDataType.Single or JetDataType.Double or JetDataType.Currency)
            {
                return Convert.ToString(val, System.Globalization.CultureInfo.InvariantCulture) ?? "0";
            }

            if (type == JetDataType.DateTime)
            {
                if (val is DateTime dt)
                {
                    return $"'{dt:yyyy-MM-dd HH:mm:ss.fff}'";
                }
                return $"'{val}'";
            }

            if (type == JetDataType.Binary && val is byte[] bytes)
            {
                if (dialect == SqlDialect.PostgreSql)
                {
                    return $"'\\x{Convert.ToHexString(bytes)}'";
                }
                if (dialect == SqlDialect.SqlServer)
                {
                    return $"0x{Convert.ToHexString(bytes)}";
                }
                if (dialect == SqlDialect.MySql)
                {
                    return $"X'{Convert.ToHexString(bytes)}'";
                }
                if (dialect == SqlDialect.Sqlite)
                {
                    return $"X'{Convert.ToHexString(bytes)}'";
                }
                return $"'{Convert.ToBase64String(bytes)}'";
            }

            string str = val.ToString() ?? "";
            return $"'{str.Replace("'", "''")}'";
        }

        private static string GetDropTableStatement(string tableName, SqlMigrationOptions options)
        {
            string quoted = QuoteIdentifier(tableName, options.Dialect, options.SchemaName);

            return options.Dialect switch
            {
                SqlDialect.SqlServer => $"IF OBJECT_ID(N'{quoted}', N'U') IS NOT NULL DROP TABLE {quoted};",
                SqlDialect.Oracle => $"-- DROP TABLE {quoted} CASCADE CONSTRAINTS;",
                _ => $"DROP TABLE IF EXISTS {quoted};"
            };
        }

        private static string SanitizeName(string name)
        {
            var sb = new StringBuilder();
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_') sb.Append(c);
                else sb.Append('_');
            }
            return sb.ToString().ToLower();
        }
    }
}
