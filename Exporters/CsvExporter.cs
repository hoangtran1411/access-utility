using System;
using System.IO;
using System.Text;
using AccessUtility.Models;

namespace AccessUtility.Exporters
{
    public static class CsvExporter
    {
        public static string ExportTable(AccessTable table, string outputFilePath)
        {
            var sb = new StringBuilder();

            // Header
            for (int i = 0; i < table.Columns.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append(EscapeCsv(table.Columns[i].Name));
            }
            sb.AppendLine();

            // Rows
            foreach (var row in table.Rows)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    if (i > 0) sb.Append(",");
                    row.TryGetValue(table.Columns[i].Name, out var val);
                    sb.Append(EscapeCsv(val?.ToString() ?? ""));
                }
                sb.AppendLine();
            }

            string dir = Path.GetDirectoryName(Path.GetFullPath(outputFilePath)) ?? ".";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllText(outputFilePath, sb.ToString(), Encoding.UTF8);
            return outputFilePath;
        }

        private static string EscapeCsv(string field)
        {
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }
    }
}
