using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AccessUtility.Models;

namespace AccessUtility.Exporters
{
    public static class JsonLinesExporter
    {
        public static string ExportTable(AccessTable table, string outputFilePath)
        {
            string dir = Path.GetDirectoryName(Path.GetFullPath(outputFilePath)) ?? ".";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using var fileStream = File.Create(outputFilePath);
            WriteTableToJsonLines(table, fileStream);

            return outputFilePath;
        }

        public static async Task<string> ExportTableStreamAsync(AccessTable table, IAsyncEnumerable<AccessRow> rows, string outputFilePath, CancellationToken ct = default)
        {
            string dir = Path.GetDirectoryName(Path.GetFullPath(outputFilePath)) ?? ".";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using var fileStream = File.Create(outputFilePath);
            var bufferWriter = new ArrayBufferWriter<byte>();

            await foreach (var row in rows.WithCancellation(ct))
            {
                bufferWriter.Clear();
                using (var writer = new Utf8JsonWriter(bufferWriter))
                {
                    WriteRow(writer, table.Columns, row.Values);
                }
                await fileStream.WriteAsync(bufferWriter.WrittenMemory, ct);
                fileStream.WriteByte((byte)'\n');
            }

            return outputFilePath;
        }

        public static string ExportDatabase(AccessDatabase db, string outputFilePathOrDirectory)
        {
            string fullPath = Path.GetFullPath(outputFilePathOrDirectory);

            if (Path.HasExtension(outputFilePathOrDirectory) && !Directory.Exists(outputFilePathOrDirectory))
            {
                // Single consolidated .jsonl file for all tables
                string dir = Path.GetDirectoryName(fullPath) ?? ".";
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                using var fileStream = File.Create(fullPath);
                foreach (var table in db.Tables)
                {
                    WriteTableToJsonLines(table, fileStream);
                }
                return fullPath;
            }
            else
            {
                // Directory mode: one .jsonl per table
                if (!Directory.Exists(fullPath)) Directory.CreateDirectory(fullPath);

                foreach (var table in db.Tables)
                {
                    string targetFile = Path.Combine(fullPath, $"{SanitizeFileName(table.Name)}.jsonl");
                    ExportTable(table, targetFile);
                }
                return fullPath;
            }
        }

        private static void WriteTableToJsonLines(AccessTable table, Stream stream)
        {
            var bufferWriter = new ArrayBufferWriter<byte>();
            foreach (var row in table.Rows)
            {
                bufferWriter.Clear();
                using (var writer = new Utf8JsonWriter(bufferWriter))
                {
                    WriteRow(writer, table.Columns, row);
                }
                stream.Write(bufferWriter.WrittenSpan);
                stream.WriteByte((byte)'\n');
            }
        }

        private static void WriteRow(Utf8JsonWriter writer, List<AccessColumn> columns, Dictionary<string, object?> row)
        {
            writer.WriteStartObject();
            foreach (var col in columns)
            {
                writer.WritePropertyName(col.Name);
                if (row.TryGetValue(col.Name, out var val) && val != null && val != DBNull.Value)
                {
                    WriteValue(writer, val);
                }
                else
                {
                    writer.WriteNullValue();
                }
            }
            writer.WriteEndObject();
        }

        private static void WriteValue(Utf8JsonWriter writer, object val)
        {
            switch (val)
            {
                case bool b:
                    writer.WriteBooleanValue(b);
                    break;
                case byte by:
                    writer.WriteNumberValue(by);
                    break;
                case sbyte sby:
                    writer.WriteNumberValue(sby);
                    break;
                case short s:
                    writer.WriteNumberValue(s);
                    break;
                case ushort us:
                    writer.WriteNumberValue(us);
                    break;
                case int i:
                    writer.WriteNumberValue(i);
                    break;
                case uint ui:
                    writer.WriteNumberValue(ui);
                    break;
                case long l:
                    writer.WriteNumberValue(l);
                    break;
                case ulong ul:
                    writer.WriteNumberValue(ul);
                    break;
                case float f:
                    writer.WriteNumberValue(f);
                    break;
                case double d:
                    writer.WriteNumberValue(d);
                    break;
                case decimal dec:
                    writer.WriteNumberValue(dec);
                    break;
                case DateTime dt:
                    writer.WriteStringValue(dt.ToString("yyyy-MM-ddTHH:mm:ss.fff"));
                    break;
                case DateTimeOffset dto:
                    writer.WriteStringValue(dto.ToString("o"));
                    break;
                case byte[] bytes:
                    writer.WriteBase64StringValue(bytes);
                    break;
                case Guid g:
                    writer.WriteStringValue(g.ToString());
                    break;
                default:
                    writer.WriteStringValue(val.ToString() ?? string.Empty);
                    break;
            }
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new System.Text.StringBuilder();
            foreach (char c in name)
            {
                sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            }
            return sb.ToString();
        }
    }
}
