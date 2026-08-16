using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AccessUtility.Models;
using Parquet;
using Parquet.Schema;

namespace AccessUtility.Exporters
{
    public static class ParquetExporter
    {
        public static async Task<string> ExportTableAsync(AccessTable table, string outputFilePath, CompressionMethod compression = CompressionMethod.Snappy, CancellationToken ct = default)
        {
            string dir = Path.GetDirectoryName(Path.GetFullPath(outputFilePath)) ?? ".";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            if (File.Exists(outputFilePath)) File.Delete(outputFilePath);

            var fields = new List<Field>();
            foreach (var col in table.Columns)
            {
                fields.Add(MapToDataField(col));
            }

            var schema = new ParquetSchema(fields);

            using var fileStream = File.Create(outputFilePath);
            var options = new ParquetOptions
            {
                CompressionMethod = compression
            };

            await using var parquetWriter = await ParquetWriter.CreateAsync(schema, fileStream, options, cancellationToken: ct);
            using var groupWriter = parquetWriter.CreateRowGroup();

            foreach (var col in table.Columns)
            {
                var field = (DataField)schema.DataFields.First(f => f.Name == col.Name);
                await WriteColumnDataAsync(groupWriter, field, col, table.Rows, ct);
            }

            return outputFilePath;
        }

        public static string ExportTable(AccessTable table, string outputFilePath, CompressionMethod compression = CompressionMethod.Snappy)
        {
            return ExportTableAsync(table, outputFilePath, compression).GetAwaiter().GetResult();
        }

        public static async Task<string> ExportTableStreamAsync(AccessTable table, IAsyncEnumerable<AccessRow> rows, string outputFilePath, CompressionMethod compression = CompressionMethod.Snappy, CancellationToken ct = default)
        {
            var rowList = new List<Dictionary<string, object?>>();
            await foreach (var row in rows.WithCancellation(ct))
            {
                rowList.Add(row.Values);
            }

            var memTable = new AccessTable
            {
                Name = table.Name,
                Columns = table.Columns,
                Rows = rowList,
                RecordCount = rowList.Count
            };

            return await ExportTableAsync(memTable, outputFilePath, compression, ct);
        }

        public static async Task<string> ExportDatabaseAsync(AccessDatabase db, string outputDirectoryOrFilePath, CompressionMethod compression = CompressionMethod.Snappy, CancellationToken ct = default)
        {
            string fullPath = Path.GetFullPath(outputDirectoryOrFilePath);

            if (Path.HasExtension(outputDirectoryOrFilePath) && !Directory.Exists(outputDirectoryOrFilePath) && db.Tables.Count <= 1)
            {
                var table = db.Tables.Count > 0 ? db.Tables[0] : new AccessTable { Name = "Table1" };
                return await ExportTableAsync(table, fullPath, compression, ct);
            }

            if (!Directory.Exists(fullPath)) Directory.CreateDirectory(fullPath);

            foreach (var table in db.Tables)
            {
                if (table.Columns.Count == 0) continue;
                string targetFile = Path.Combine(fullPath, $"{SanitizeFileName(table.Name)}.parquet");
                await ExportTableAsync(table, targetFile, compression, ct);
            }

            return fullPath;
        }

        public static string ExportDatabase(AccessDatabase db, string outputDirectoryOrFilePath, CompressionMethod compression = CompressionMethod.Snappy)
        {
            return ExportDatabaseAsync(db, outputDirectoryOrFilePath, compression).GetAwaiter().GetResult();
        }

        private static DataField MapToDataField(AccessColumn col) => col.DataType switch
        {
            JetDataType.Boolean => new DataField<bool?>(col.Name),
            JetDataType.Byte => new DataField<byte?>(col.Name),
            JetDataType.Integer => new DataField<short?>(col.Name),
            JetDataType.LongInteger or JetDataType.Autonumber => new DataField<int?>(col.Name),
            JetDataType.Single => new DataField<float?>(col.Name),
            JetDataType.Double => new DataField<double?>(col.Name),
            JetDataType.Currency => new DataField<decimal?>(col.Name),
            JetDataType.DateTime => new DataField<DateTime?>(col.Name),
            JetDataType.Binary => new DataField<byte[]>(col.Name),
            _ => new DataField<string>(col.Name)
        };

        private static async Task WriteColumnDataAsync(ParquetRowGroupWriter groupWriter, DataField field, AccessColumn col, List<Dictionary<string, object?>> rows, CancellationToken ct)
        {
            int count = rows.Count;
            switch (col.DataType)
            {
                case JetDataType.Boolean:
                {
                    var arr = new bool?[count];
                    for (int i = 0; i < count; i++)
                    {
                        if (rows[i].TryGetValue(col.Name, out var v) && v is bool b) arr[i] = b;
                        else if (v != null && bool.TryParse(v.ToString(), out var pb)) arr[i] = pb;
                        else arr[i] = null;
                    }
                    await groupWriter.WriteAsync<bool>(field, arr.AsMemory(), cancellationToken: ct);
                    break;
                }
                case JetDataType.Byte:
                {
                    var arr = new byte?[count];
                    for (int i = 0; i < count; i++)
                    {
                        if (rows[i].TryGetValue(col.Name, out var v) && v != null && byte.TryParse(v.ToString(), out var b)) arr[i] = b;
                        else arr[i] = null;
                    }
                    await groupWriter.WriteAsync<byte>(field, arr.AsMemory(), cancellationToken: ct);
                    break;
                }
                case JetDataType.Integer:
                {
                    var arr = new short?[count];
                    for (int i = 0; i < count; i++)
                    {
                        if (rows[i].TryGetValue(col.Name, out var v) && v != null && short.TryParse(v.ToString(), out var s)) arr[i] = s;
                        else arr[i] = null;
                    }
                    await groupWriter.WriteAsync<short>(field, arr.AsMemory(), cancellationToken: ct);
                    break;
                }
                case JetDataType.LongInteger:
                case JetDataType.Autonumber:
                {
                    var arr = new int?[count];
                    for (int i = 0; i < count; i++)
                    {
                        if (rows[i].TryGetValue(col.Name, out var v) && v != null && int.TryParse(v.ToString(), out var val)) arr[i] = val;
                        else arr[i] = null;
                    }
                    await groupWriter.WriteAsync<int>(field, arr.AsMemory(), cancellationToken: ct);
                    break;
                }
                case JetDataType.Single:
                {
                    var arr = new float?[count];
                    for (int i = 0; i < count; i++)
                    {
                        if (rows[i].TryGetValue(col.Name, out var v) && v != null && float.TryParse(v.ToString(), out var f)) arr[i] = f;
                        else arr[i] = null;
                    }
                    await groupWriter.WriteAsync<float>(field, arr.AsMemory(), cancellationToken: ct);
                    break;
                }
                case JetDataType.Double:
                {
                    var arr = new double?[count];
                    for (int i = 0; i < count; i++)
                    {
                        if (rows[i].TryGetValue(col.Name, out var v) && v != null && double.TryParse(v.ToString(), out var d)) arr[i] = d;
                        else arr[i] = null;
                    }
                    await groupWriter.WriteAsync<double>(field, arr.AsMemory(), cancellationToken: ct);
                    break;
                }
                case JetDataType.Currency:
                {
                    var arr = new decimal?[count];
                    for (int i = 0; i < count; i++)
                    {
                        if (rows[i].TryGetValue(col.Name, out var v) && v != null && decimal.TryParse(v.ToString(), out var dec)) arr[i] = dec;
                        else arr[i] = null;
                    }
                    await groupWriter.WriteAsync<decimal>(field, arr.AsMemory(), cancellationToken: ct);
                    break;
                }
                case JetDataType.DateTime:
                {
                    var arr = new DateTime?[count];
                    for (int i = 0; i < count; i++)
                    {
                        if (rows[i].TryGetValue(col.Name, out var v) && v is DateTime dt) arr[i] = dt;
                        else if (v != null && DateTime.TryParse(v.ToString(), out var parsedDt)) arr[i] = parsedDt;
                        else arr[i] = null;
                    }
                    await groupWriter.WriteAsync<DateTime>(field, arr.AsMemory(), cancellationToken: ct);
                    break;
                }
                case JetDataType.Binary:
                {
                    var arr = new byte[]?[count];
                    for (int i = 0; i < count; i++)
                    {
                        if (rows[i].TryGetValue(col.Name, out var v) && v is byte[] b) arr[i] = b;
                        else arr[i] = null;
                    }
                    await groupWriter.WriteAsync(field, (IReadOnlyCollection<byte[]?>)arr);
                    break;
                }
                case JetDataType.Text:
                case JetDataType.Memo:
                case JetDataType.Guid:
                default:
                {
                    var arr = new string?[count];
                    for (int i = 0; i < count; i++)
                    {
                        if (rows[i].TryGetValue(col.Name, out var v) && v != null) arr[i] = v.ToString();
                        else arr[i] = null;
                    }
                    await groupWriter.WriteAsync(field, (IReadOnlyCollection<string?>)arr);
                    break;
                }
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
