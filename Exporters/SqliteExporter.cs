using System;
using System.IO;
using AccessUtility.Models;
using Microsoft.Data.Sqlite;

namespace AccessUtility.Exporters
{
    public static class SqliteExporter
    {
        public static string ExportDatabase(AccessDatabase db, string outputSqlitePath)
        {
            if (File.Exists(outputSqlitePath)) File.Delete(outputSqlitePath);

            string dir = Path.GetDirectoryName(Path.GetFullPath(outputSqlitePath)) ?? ".";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string cs = new SqliteConnectionStringBuilder
            {
                DataSource = outputSqlitePath,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();

            using var conn = new SqliteConnection(cs);
            conn.Open();

            using var transaction = conn.BeginTransaction();

            foreach (var table in db.Tables)
            {
                if (table.Columns.Count == 0) continue;

                // Build CREATE TABLE SQL
                string sanitizeName = SanitizeSqlName(table.Name);
                var colDefs = new System.Collections.Generic.List<string>();

                foreach (var col in table.Columns)
                {
                    string colName = SanitizeSqlName(col.Name);
                    string sqliteType = MapToSqliteType(col.DataType);
                    colDefs.Add($"[{colName}] {sqliteType}");
                }

                string createTableSql = $"CREATE TABLE [{sanitizeName}] ({string.Join(", ", colDefs)});";
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = createTableSql;
                    cmd.ExecuteNonQuery();
                }

                // Insert Rows
                foreach (var row in table.Rows)
                {
                    var paramNames = new System.Collections.Generic.List<string>();
                    var colNames = new System.Collections.Generic.List<string>();

                    using var insertCmd = conn.CreateCommand();
                    insertCmd.Transaction = transaction;

                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        var col = table.Columns[i];
                        string colName = SanitizeSqlName(col.Name);
                        string paramName = $"@p{i}";

                        colNames.Add($"[{colName}]");
                        paramNames.Add(paramName);

                        row.TryGetValue(col.Name, out var val);
                        insertCmd.Parameters.AddWithValue(paramName, val ?? DBNull.Value);
                    }

                    insertCmd.CommandText = $"INSERT INTO [{sanitizeName}] ({string.Join(", ", colNames)}) VALUES ({string.Join(", ", paramNames)});";
                    insertCmd.ExecuteNonQuery();
                }
            }

            transaction.Commit();
            return outputSqlitePath;
        }

        private static string MapToSqliteType(JetDataType type) => type switch
        {
            JetDataType.Boolean => "INTEGER",
            JetDataType.Byte => "INTEGER",
            JetDataType.Integer => "INTEGER",
            JetDataType.LongInteger or JetDataType.Autonumber => "INTEGER",
            JetDataType.Single => "REAL",
            JetDataType.Double => "REAL",
            JetDataType.Currency => "REAL",
            JetDataType.DateTime => "TEXT",
            JetDataType.Text or JetDataType.Memo => "TEXT",
            _ => "TEXT"
        };

        private static string SanitizeSqlName(string name)
        {
            return name.Replace("]", "").Replace("[", "").Trim();
        }
    }
}
