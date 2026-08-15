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

        public static async Task<string> ExportTableStreamAsync(AccessTable table, IAsyncEnumerable<AccessRow> rows, string outputFilePath, CancellationToken ct = default)
        {
            string dir = Path.GetDirectoryName(Path.GetFullPath(outputFilePath)) ?? ".";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using var writer = new StreamWriter(outputFilePath, append: false, Encoding.UTF8);

            // Header
            var headerLine = string.Join(",", table.Columns.ConvertAll(c => EscapeCsv(c.Name)));
            await writer.WriteLineAsync(headerLine.AsMemory(), ct);

            // Rows Stream
            await foreach (var row in rows.WithCancellation(ct))
            {
                var line = string.Join(",", table.Columns.ConvertAll(c =>
                {
                    row.Values.TryGetValue(c.Name, out var val);
                    return EscapeCsv(val?.ToString() ?? "");
                }));
                await writer.WriteLineAsync(line.AsMemory(), ct);
            }

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
