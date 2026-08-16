using System;
using System.Collections.Generic;
using System.IO;
using AccessUtility.Models;
using DuckDB.NET.Data;

namespace AccessUtility.Exporters
{
    public static class DuckDbExporter
    {
        public static string ExportDatabase(AccessDatabase db, string outputDuckDbPath)
        {
            if (File.Exists(outputDuckDbPath)) File.Delete(outputDuckDbPath);

            string dir = Path.GetDirectoryName(Path.GetFullPath(outputDuckDbPath)) ?? ".";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using var connection = new DuckDBConnection($"Data Source={outputDuckDbPath}");
            connection.Open();

            foreach (var table in db.Tables)
            {
                if (table.Columns.Count == 0) continue;
                ExportTableToDuckDb(table, connection);
            }

            connection.Close();
            return outputDuckDbPath;
        }

        public static string ExportTable(AccessTable table, string outputDuckDbPath)
        {
            if (File.Exists(outputDuckDbPath)) File.Delete(outputDuckDbPath);

            string dir = Path.GetDirectoryName(Path.GetFullPath(outputDuckDbPath)) ?? ".";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using var connection = new DuckDBConnection($"Data Source={outputDuckDbPath}");
            connection.Open();

            ExportTableToDuckDb(table, connection);

            connection.Close();
            return outputDuckDbPath;
        }

        private static void ExportTableToDuckDb(AccessTable table, DuckDBConnection connection)
        {
            string sanitizeTableName = SanitizeSqlName(table.Name);
            var colDefs = new List<string>();

            foreach (var col in table.Columns)
            {
                string colName = SanitizeSqlName(col.Name);
                string duckDbType = MapToDuckDbType(col.DataType);
                colDefs.Add($"\"{colName}\" {duckDbType}");
            }

            string createTableSql = $"CREATE TABLE \"{sanitizeTableName}\" ({string.Join(", ", colDefs)});";
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = createTableSql;
                cmd.ExecuteNonQuery();
            }

            if (table.Rows.Count == 0) return;

            using var appender = connection.CreateAppender(sanitizeTableName);
            foreach (var row in table.Rows)
            {
                var appenderRow = appender.CreateRow();
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    var col = table.Columns[i];
                    row.TryGetValue(col.Name, out var val);
                    AppendValue(appenderRow, col.DataType, val);
                }
                appenderRow.EndRow();
            }
        }

        private static void AppendValue(IDuckDBAppenderRow appenderRow, JetDataType type, object? val)
        {
            if (val == null || val == DBNull.Value)
            {
                appenderRow.AppendNullValue();
                return;
            }

            switch (type)
            {
                case JetDataType.Boolean:
                    if (val is bool b) appenderRow.AppendValue(b);
                    else if (bool.TryParse(val.ToString(), out var pb)) appenderRow.AppendValue(pb);
                    else appenderRow.AppendNullValue();
                    break;
                case JetDataType.Byte:
                    if (val is byte by) appenderRow.AppendValue(by);
                    else if (byte.TryParse(val.ToString(), out var pby)) appenderRow.AppendValue(pby);
                    else appenderRow.AppendNullValue();
                    break;
                case JetDataType.Integer:
                    if (val is short s) appenderRow.AppendValue(s);
                    else if (short.TryParse(val.ToString(), out var ps)) appenderRow.AppendValue(ps);
                    else appenderRow.AppendNullValue();
                    break;
                case JetDataType.LongInteger:
                case JetDataType.Autonumber:
                    if (val is int i) appenderRow.AppendValue(i);
                    else if (int.TryParse(val.ToString(), out var pi)) appenderRow.AppendValue(pi);
                    else appenderRow.AppendNullValue();
                    break;
                case JetDataType.Single:
                    if (val is float f) appenderRow.AppendValue(f);
                    else if (float.TryParse(val.ToString(), out var pf)) appenderRow.AppendValue(pf);
                    else appenderRow.AppendNullValue();
                    break;
                case JetDataType.Double:
                    if (val is double d) appenderRow.AppendValue(d);
                    else if (double.TryParse(val.ToString(), out var pd)) appenderRow.AppendValue(pd);
                    else appenderRow.AppendNullValue();
                    break;
                case JetDataType.Currency:
                    if (val is decimal dec) appenderRow.AppendValue(dec);
                    else if (decimal.TryParse(val.ToString(), out var pdec)) appenderRow.AppendValue(pdec);
                    else appenderRow.AppendNullValue();
                    break;
                case JetDataType.DateTime:
                    if (val is DateTime dt) appenderRow.AppendValue(dt);
                    else if (DateTime.TryParse(val.ToString(), out var pdt)) appenderRow.AppendValue(pdt);
                    else appenderRow.AppendNullValue();
                    break;
                case JetDataType.Binary:
                    if (val is byte[] bytes) appenderRow.AppendValue(bytes);
                    else appenderRow.AppendNullValue();
                    break;
                default:
                    appenderRow.AppendValue(val.ToString());
                    break;
            }
        }

        private static string MapToDuckDbType(JetDataType type) => type switch
        {
            JetDataType.Boolean => "BOOLEAN",
            JetDataType.Byte => "UTINYINT",
            JetDataType.Integer => "SMALLINT",
            JetDataType.LongInteger or JetDataType.Autonumber => "INTEGER",
            JetDataType.Single => "FLOAT",
            JetDataType.Double => "DOUBLE",
            JetDataType.Currency => "DECIMAL(18,4)",
            JetDataType.DateTime => "TIMESTAMP",
            JetDataType.Binary => "BLOB",
            JetDataType.Guid => "VARCHAR",
            JetDataType.Text or JetDataType.Memo => "VARCHAR",
            _ => "VARCHAR"
        };

        private static string SanitizeSqlName(string name)
        {
            return name.Replace("\"", "").Trim();
        }
    }
}
