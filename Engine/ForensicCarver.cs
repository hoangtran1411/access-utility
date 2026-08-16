using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using AccessUtility.Models;
using Microsoft.Data.Sqlite;

namespace AccessUtility.Engine
{
    public static class ForensicCarver
    {
        public const int PageSize = 2048;

        public static ForensicCarveReport CarveDatabase(string filePath, string? targetTableName = null)
        {
            var report = new ForensicCarveReport
            {
                DatabasePath = filePath
            };

            if (!File.Exists(filePath))
            {
                report.SummaryMessage = "Error: Database file does not exist.";
                return report;
            }

            byte[] fileBytes;
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                report.TotalPagesScanned = (int)(stream.Length / PageSize);
                fileBytes = new byte[stream.Length];
                stream.ReadExactly(fileBytes);
            }

            if (report.TotalPagesScanned == 0)
            {
                report.SummaryMessage = "Error: File is smaller than 2048 bytes.";
                return report;
            }

            // Step 1: Discover all TDEFs
            var tdefs = new List<AccessTable>();
            for (uint i = 0; i < report.TotalPagesScanned; i++)
            {
                int offset = (int)(i * PageSize);
                if (offset + 2 <= fileBytes.Length && fileBytes[offset] == 0x02 && fileBytes[offset + 1] == 0x01)
                {
                    try
                    {
                        var t = Jet3BinaryReader.ParseTableDefinition(fileBytes, i);
                        if (t != null && !string.IsNullOrWhiteSpace(t.Name) && !t.Name.StartsWith("MSys") && !t.Name.StartsWith("~"))
                        {
                            if (string.IsNullOrEmpty(targetTableName) || t.Name.Equals(targetTableName, StringComparison.OrdinalIgnoreCase))
                            {
                                tdefs.Add(t);
                            }
                        }
                    }
                    catch
                    {
                        // Ignore corrupted TDEF headers
                    }
                }
            }

            var tableStats = new Dictionary<string, CarvedTableSummary>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in tdefs)
            {
                tableStats[t.Name] = new CarvedTableSummary { TableName = t.Name };
            }

            // Step 2: Scan all pages for active rows, deleted slots, and unreferenced slack regions
            var carvedRecords = new List<CarvedRecord>();

            for (int pageIdx = 0; pageIdx < report.TotalPagesScanned; pageIdx++)
            {
                int pageOffset = pageIdx * PageSize;
                byte pageType = fileBytes[pageOffset];

                if (pageType == 0x01)
                {
                    report.ActivePagesCount++;
                    uint ownerTdef = BitConverter.ToUInt32(fileBytes, pageOffset + 4);
                    var matchingTable = tdefs.FirstOrDefault(t => t.TdefPage == ownerTdef);

                    ushort numSlots = BitConverter.ToUInt16(fileBytes, pageOffset + 8);
                    var activeOffsets = new HashSet<int>();
                    var deletedSlotOffsets = new List<int>();

                    for (int slot = 0; slot < numSlots; slot++)
                    {
                        int ptrPos = pageOffset + PageSize - ((slot + 1) * 2);
                        if (ptrPos < pageOffset + 12 || ptrPos >= pageOffset + PageSize) continue;

                        ushort rawOffset = BitConverter.ToUInt16(fileBytes, ptrPos);
                        if (rawOffset == 0) continue;

                        bool isDeleted = (rawOffset & 0x8000) != 0;
                        int cleanOffset = rawOffset & 0x7FFF;

                        if (cleanOffset >= 12 && cleanOffset < PageSize - (numSlots * 2))
                        {
                            if (isDeleted)
                            {
                                deletedSlotOffsets.Add(cleanOffset);
                            }
                            else
                            {
                                activeOffsets.Add(cleanOffset);
                                if (matchingTable != null && tableStats.TryGetValue(matchingTable.Name, out var ts))
                                {
                                    ts.ActiveRowsCount++;
                                    report.ActiveRowsCount++;
                                }
                            }
                        }
                    }

                    // 2a. Carve records from explicit deleted slots
                    foreach (int delOffset in deletedSlotOffsets)
                    {
                        int rowAbsPos = pageOffset + delOffset;
                        var candidateTables = matchingTable != null ? new List<AccessTable> { matchingTable } : tdefs;

                        foreach (var schema in candidateTables)
                        {
                            if (TryCarveRecordAt(fileBytes, pageIdx, delOffset, rowAbsPos, schema, isDeletedSlot: true, out var carvedRec))
                            {
                                carvedRecords.Add(carvedRec);
                                if (tableStats.TryGetValue(schema.Name, out var ts))
                                {
                                    ts.DeletedRowsSalvaged++;
                                }
                                break;
                            }
                        }
                    }

                    // 2b. Carve unreferenced slack space gaps
                    int slotTableStart = PageSize - (numSlots * 2);
                    int scanPos = 12; // Start after 12-byte data page header

                    while (scanPos < slotTableStart - 8)
                    {
                        if (activeOffsets.Contains(scanPos) || deletedSlotOffsets.Contains(scanPos))
                        {
                            scanPos += 4;
                            continue;
                        }

                        int rowAbsPos = pageOffset + scanPos;
                        var candidateTables = matchingTable != null ? new List<AccessTable> { matchingTable } : tdefs;

                        bool carved = false;
                        foreach (var schema in candidateTables)
                        {
                            if (TryCarveRecordAt(fileBytes, pageIdx, scanPos, rowAbsPos, schema, isDeletedSlot: false, out var carvedRec))
                            {
                                // Avoid exact duplicate offsets
                                if (!carvedRecords.Any(r => r.PageIndex == pageIdx && r.ByteOffset == scanPos))
                                {
                                    carvedRecords.Add(carvedRec);
                                    if (tableStats.TryGetValue(schema.Name, out var ts))
                                    {
                                        ts.DeletedRowsSalvaged++;
                                    }
                                    carved = true;
                                    scanPos += Math.Max(8, EstimateRecordLength(schema));
                                    break;
                                }
                            }
                        }

                        if (!carved) scanPos += 2;
                    }
                }
                else if (pageType == 0x00)
                {
                    // Scan unallocated / slack pages for orphaned records
                    report.SlackPagesScanned++;
                    int scanPos = 12;
                    while (scanPos < PageSize - 16)
                    {
                        int rowAbsPos = pageOffset + scanPos;
                        bool carved = false;
                        foreach (var schema in tdefs)
                        {
                            if (TryCarveRecordAt(fileBytes, pageIdx, scanPos, rowAbsPos, schema, isDeletedSlot: false, out var carvedRec))
                            {
                                if (!carvedRecords.Any(r => r.PageIndex == pageIdx && r.ByteOffset == scanPos))
                                {
                                    carvedRecords.Add(carvedRec);
                                    if (tableStats.TryGetValue(schema.Name, out var ts))
                                    {
                                        ts.DeletedRowsSalvaged++;
                                    }
                                    carved = true;
                                    scanPos += Math.Max(8, EstimateRecordLength(schema));
                                    break;
                                }
                            }
                        }

                        if (!carved) scanPos += 4;
                    }
                }
            }

            report.CarvedRecords = carvedRecords;
            report.SalvagedDeletedRowsCount = carvedRecords.Count;
            report.HighConfidenceCount = carvedRecords.Count(r => r.ConfidenceScore >= 0.80);
            report.MediumConfidenceCount = carvedRecords.Count(r => r.ConfidenceScore >= 0.50 && r.ConfidenceScore < 0.80);
            report.LowConfidenceCount = carvedRecords.Count(r => r.ConfidenceScore < 0.50);

            foreach (var kvp in tableStats)
            {
                var recsForTable = carvedRecords.Where(r => r.TableName.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase)).ToList();
                kvp.Value.AverageConfidence = recsForTable.Count > 0 ? Math.Round(recsForTable.Average(r => r.ConfidenceScore) * 100.0, 1) : 0.0;
                report.TableSummaries.Add(kvp.Value);
            }

            report.SummaryMessage = $"Forensic carving completed on {report.TotalPagesScanned} pages. " +
                                   $"Salvaged {report.SalvagedDeletedRowsCount} deleted records " +
                                   $"({report.HighConfidenceCount} High Confidence, {report.MediumConfidenceCount} Medium, {report.LowConfidenceCount} Low).";

            return report;
        }

        public static string ExportCarvedRecordsToSqlite(ForensicCarveReport report, string outputSqlitePath)
        {
            if (File.Exists(outputSqlitePath)) File.Delete(outputSqlitePath);

            string dir = Path.GetDirectoryName(Path.GetFullPath(outputSqlitePath)) ?? ".";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using var conn = new SqliteConnection($"Data Source={outputSqlitePath};Mode=ReadWriteCreate;");
            conn.Open();

            using var tx = conn.BeginTransaction();

            var groups = report.CarvedRecords.GroupBy(r => r.TableName, StringComparer.OrdinalIgnoreCase);

            foreach (var grp in groups)
            {
                string tableName = $"Carved_{grp.Key}";
                var colNames = grp.SelectMany(r => r.Values.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                var colDefs = new List<string>
                {
                    "[_Carved_Id] INTEGER PRIMARY KEY AUTOINCREMENT",
                    "[_Carved_PageIndex] INTEGER",
                    "[_Carved_ByteOffset] INTEGER",
                    "[_Carved_IsDeletedSlot] INTEGER",
                    "[_Carved_Confidence] REAL",
                    "[_Carved_Rating] TEXT"
                };

                foreach (var c in colNames)
                {
                    colDefs.Add($"[{c}] TEXT");
                }

                string createSql = $"CREATE TABLE [{tableName}] ({string.Join(", ", colDefs)});";
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = createSql;
                    cmd.ExecuteNonQuery();
                }

                foreach (var rec in grp)
                {
                    var insertCols = new List<string>
                    {
                        "[_Carved_PageIndex]", "[_Carved_ByteOffset]", "[_Carved_IsDeletedSlot]", "[_Carved_Confidence]", "[_Carved_Rating]"
                    };
                    var paramNames = new List<string>
                    {
                        "@p_idx", "@p_off", "@p_del", "@p_conf", "@p_rate"
                    };

                    using var insertCmd = conn.CreateCommand();
                    insertCmd.Transaction = tx;
                    insertCmd.Parameters.AddWithValue("@p_idx", rec.PageIndex);
                    insertCmd.Parameters.AddWithValue("@p_off", rec.ByteOffset);
                    insertCmd.Parameters.AddWithValue("@p_del", rec.IsDeletedSlot ? 1 : 0);
                    insertCmd.Parameters.AddWithValue("@p_conf", rec.ConfidenceScore);
                    insertCmd.Parameters.AddWithValue("@p_rate", rec.ConfidenceRating);

                    int pIdx = 0;
                    foreach (var col in colNames)
                    {
                        string pName = $"@val_{pIdx++}";
                        insertCols.Add($"[{col}]");
                        paramNames.Add(pName);

                        rec.Values.TryGetValue(col, out var val);
                        insertCmd.Parameters.AddWithValue(pName, val?.ToString() ?? (object)DBNull.Value);
                    }

                    insertCmd.CommandText = $"INSERT INTO [{tableName}] ({string.Join(", ", insertCols)}) VALUES ({string.Join(", ", paramNames)});";
                    insertCmd.ExecuteNonQuery();
                }
            }

            tx.Commit();
            conn.Close();
            SqliteConnection.ClearAllPools();

            report.ExportedPath = outputSqlitePath;
            return outputSqlitePath;
        }

        public static string ExportCarvedRecordsToJson(ForensicCarveReport report, string outputJsonPath)
        {
            string dir = Path.GetDirectoryName(Path.GetFullPath(outputJsonPath)) ?? ".";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using var stream = File.Create(outputJsonPath);
            var writerOptions = new JsonWriterOptions { Indented = true };
            using var writer = new Utf8JsonWriter(stream, writerOptions);

            writer.WriteStartObject();
            writer.WriteString("DatabasePath", report.DatabasePath);
            writer.WriteNumber("TotalPagesScanned", report.TotalPagesScanned);
            writer.WriteNumber("ActivePagesCount", report.ActivePagesCount);
            writer.WriteNumber("SlackPagesScanned", report.SlackPagesScanned);
            writer.WriteNumber("ActiveRowsCount", report.ActiveRowsCount);
            writer.WriteNumber("SalvagedDeletedRowsCount", report.SalvagedDeletedRowsCount);
            writer.WriteNumber("HighConfidenceCount", report.HighConfidenceCount);
            writer.WriteNumber("MediumConfidenceCount", report.MediumConfidenceCount);
            writer.WriteNumber("LowConfidenceCount", report.LowConfidenceCount);
            writer.WriteString("SummaryMessage", report.SummaryMessage);

            writer.WriteStartArray("TableSummaries");
            foreach (var ts in report.TableSummaries)
            {
                writer.WriteStartObject();
                writer.WriteString("TableName", ts.TableName);
                writer.WriteNumber("ActiveRowsCount", ts.ActiveRowsCount);
                writer.WriteNumber("DeletedRowsSalvaged", ts.DeletedRowsSalvaged);
                writer.WriteNumber("AverageConfidence", ts.AverageConfidence);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WriteStartArray("Records");
            foreach (var r in report.CarvedRecords)
            {
                writer.WriteStartObject();
                writer.WriteString("TableName", r.TableName);
                writer.WriteNumber("PageIndex", r.PageIndex);
                writer.WriteNumber("ByteOffset", r.ByteOffset);
                writer.WriteBoolean("IsDeletedSlot", r.IsDeletedSlot);
                writer.WriteNumber("ConfidenceScore", r.ConfidenceScore);
                writer.WriteString("ConfidenceRating", r.ConfidenceRating);

                writer.WriteStartObject("Values");
                foreach (var kvp in r.Values)
                {
                    writer.WritePropertyName(kvp.Key);
                    if (kvp.Value == null || kvp.Value == DBNull.Value)
                    {
                        writer.WriteNullValue();
                    }
                    else
                    {
                        writer.WriteStringValue(kvp.Value.ToString() ?? "");
                    }
                }
                writer.WriteEndObject();

                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
            writer.Flush();

            report.ExportedPath = outputJsonPath;
            return outputJsonPath;
        }

        private static bool TryCarveRecordAt(byte[] fileBytes, int pageIndex, int relOffset, int rowAbsPos, AccessTable schema, bool isDeletedSlot, out CarvedRecord record)
        {
            record = new CarvedRecord
            {
                TableName = schema.Name,
                PageIndex = pageIndex,
                ByteOffset = relOffset,
                IsDeletedSlot = isDeletedSlot
            };

            int pageEndPos = (pageIndex + 1) * PageSize;
            if (rowAbsPos + 2 >= pageEndPos || rowAbsPos >= fileBytes.Length) return false;

            byte colCount = fileBytes[rowAbsPos];
            if (colCount == 0 || colCount > 100) return false;

            // Compute confidence factors
            double score = 0.0;
            int checksPassed = 0;
            int totalChecks = 0;

            if (schema.Columns.Count > 0)
            {
                totalChecks++;
                if (Math.Abs(colCount - schema.Columns.Count) <= 2)
                {
                    checksPassed++;
                    score += 0.25;
                }
            }

            int nullMaskBytes = (colCount + 7) / 8;
            int pos = rowAbsPos + 1;
            int fixedStartPos = pos;

            int nullMaskPos = fixedStartPos;
            foreach (var col in schema.Columns)
            {
                if (!col.IsVariableLength) nullMaskPos += col.Length;
            }

            if (nullMaskPos + nullMaskBytes > pageEndPos) return false;

            byte[] nullMask = new byte[nullMaskBytes];
            Array.Copy(fileBytes, nullMaskPos, nullMask, 0, nullMaskBytes);

            // Read Fixed Columns
            int validFixedCount = 0;
            foreach (var col in schema.Columns)
            {
                if (!col.IsVariableLength)
                {
                    totalChecks++;
                    bool isNull = IsColumnNull(nullMask, col.ColumnId);
                    if (isNull)
                    {
                        record.Values[col.Name] = DBNull.Value;
                        checksPassed++;
                        validFixedCount++;
                    }
                    else
                    {
                        int dataPos = fixedStartPos + col.FixedOffset;
                        if (dataPos + col.Length <= pageEndPos)
                        {
                            var val = ReadFixedValue(fileBytes, dataPos, col.DataType, col.Length, out bool validVal);
                            record.Values[col.Name] = val;
                            if (validVal)
                            {
                                checksPassed++;
                                validFixedCount++;
                            }
                        }
                    }
                }
            }

            // Read Variable Length Columns
            int varStartPos = nullMaskPos + nullMaskBytes;
            if (varStartPos < pageEndPos)
            {
                byte varColCount = fileBytes[varStartPos];
                int varOffsetsPos = varStartPos + 1;
                var varCols = schema.Columns.FindAll(c => c.IsVariableLength);

                if (varColCount >= 0 && varColCount <= 50 && varOffsetsPos + (varColCount * 2) <= pageEndPos)
                {
                    totalChecks++;
                    checksPassed++;
                    score += 0.20;

                    ushort prevEnd = 0;
                    bool monotonicOffsets = true;

                    for (int v = 0; v < varCols.Count && v < varColCount; v++)
                    {
                        var col = varCols[v];
                        int endOffPos = varOffsetsPos + (v * 2);
                        int startOffPos = v == 0 ? varOffsetsPos + (varColCount * 2) : varOffsetsPos + ((v - 1) * 2);

                        if (endOffPos + 2 <= pageEndPos)
                        {
                            ushort endOffset = BitConverter.ToUInt16(fileBytes, endOffPos);
                            ushort startOffset = v == 0 ? (ushort)0 : BitConverter.ToUInt16(fileBytes, startOffPos);

                            if (endOffset < startOffset) monotonicOffsets = false;

                            int varDataStart = varOffsetsPos + (varColCount * 2) + startOffset;
                            int varDataLen = endOffset - startOffset;

                            if (varDataStart + varDataLen <= pageEndPos && varDataLen >= 0 && varDataLen <= 255)
                            {
                                string textVal = Encoding.ASCII.GetString(fileBytes, varDataStart, varDataLen).Replace("\0", "").Trim();
                                totalChecks++;
                                if (IsPrintableText(textVal))
                                {
                                    checksPassed++;
                                }
                                record.Values[col.Name] = textVal;
                            }
                            prevEnd = endOffset;
                        }
                    }

                    if (monotonicOffsets && varCols.Count > 0) score += 0.25;
                }
            }

            // Compute overall confidence score
            if (totalChecks > 0)
            {
                double ratio = (double)checksPassed / totalChecks;
                score += (ratio * 0.30);
            }

            if (isDeletedSlot) score += 0.15; // Pointer existed in deleted slot table

            record.ConfidenceScore = Math.Min(1.0, Math.Max(0.1, Math.Round(score, 2)));
            record.ConfidenceRating = record.ConfidenceScore >= 0.80 ? "High" : (record.ConfidenceScore >= 0.50 ? "Medium" : "Low");

            // Require at least 1 non-empty column value and confidence >= 0.30
            return record.Values.Values.Any(v => v != null && v != DBNull.Value && !string.IsNullOrWhiteSpace(v.ToString())) && record.ConfidenceScore >= 0.30;
        }

        private static object? ReadFixedValue(byte[] bytes, int pos, JetDataType type, int len, out bool valid)
        {
            valid = true;
            if (pos + len > bytes.Length) { valid = false; return null; }

            switch (type)
            {
                case JetDataType.Boolean:
                    byte b = bytes[pos];
                    valid = (b == 0 || b == 1 || b == 255);
                    return b != 0;

                case JetDataType.Byte:
                    return bytes[pos];

                case JetDataType.Integer:
                    return BitConverter.ToInt16(bytes, pos);

                case JetDataType.LongInteger:
                case JetDataType.Autonumber:
                    int i = BitConverter.ToInt32(bytes, pos);
                    return i;

                case JetDataType.Single:
                    float f = BitConverter.ToSingle(bytes, pos);
                    valid = !float.IsNaN(f) && !float.IsInfinity(f);
                    return f;

                case JetDataType.Double:
                    double d = BitConverter.ToDouble(bytes, pos);
                    valid = !double.IsNaN(d) && !double.IsInfinity(d);
                    return d;

                case JetDataType.Currency:
                    long currRaw = BitConverter.ToInt64(bytes, pos);
                    return (decimal)currRaw / 10000m;

                case JetDataType.DateTime:
                    double dtDays = BitConverter.ToDouble(bytes, pos);
                    if (dtDays > 0 && dtDays < 100000)
                    {
                        valid = true;
                        return new DateTime(1899, 12, 30).AddDays(dtDays);
                    }
                    valid = false;
                    return null;

                default:
                    return bytes[pos];
            }
        }

        private static bool IsColumnNull(byte[] nullMask, int colId)
        {
            if (colId <= 0) return false;
            int byteIndex = (colId - 1) / 8;
            int bitIndex = (colId - 1) % 8;
            if (byteIndex >= nullMask.Length) return false;
            return (nullMask[byteIndex] & (1 << bitIndex)) != 0;
        }

        private static bool IsPrintableText(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;
            int printable = 0;
            foreach (char c in text)
            {
                if (c >= 32 && c <= 126 || c == '\r' || c == '\n' || c == '\t') printable++;
            }
            return (double)printable / text.Length >= 0.80;
        }

        private static int EstimateRecordLength(AccessTable table)
        {
            int len = 1; // row col count
            foreach (var col in table.Columns)
            {
                len += col.IsVariableLength ? 16 : col.Length;
            }
            return Math.Max(12, len);
        }
    }
}
