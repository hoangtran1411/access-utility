using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AccessUtility.Models;

namespace AccessUtility.Engine
{
    public class Jet3BinaryReader
    {
        public const int PageSize = 2048;

        public static AccessDatabase ReadDatabase(string filePath, out DiagnosticReport report)
        {
            var db = new AccessDatabase
            {
                FilePath = filePath,
                JetVersion = "Access 97 (Jet 3.5)",
                PageSize = PageSize
            };

            report = new DiagnosticReport
            {
                FilePath = filePath,
                LockInfo = LdbLockInspector.Inspect(filePath)
            };

            if (!File.Exists(filePath))
            {
                report.StatusSummary = "Error: File does not exist.";
                return db;
            }

            byte[] fileBytes;
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                report.FileSizeBytes = stream.Length;
                report.TotalPages = (int)(stream.Length / PageSize);
                fileBytes = new byte[stream.Length];
                stream.ReadExactly(fileBytes);
            }

            if (fileBytes.Length < PageSize)
            {
                report.StatusSummary = "Error: File is smaller than 2048-byte header page.";
                return db;
            }

            // Verify Header Signature
            byte pageType = fileBytes[0];
            string magic = Encoding.ASCII.GetString(fileBytes, 4, 15).TrimEnd('\0');
            byte version = fileBytes[0x14];

            if (!magic.Contains("Jet DB") && !magic.Contains("Standard Jet"))
            {
                report.CorruptPagesCount++;
                report.CorruptPageDetails.Add("Page 0: Invalid Header Signature");
            }

            // Scan all TDEF (Table Definition) pages
            var tdefPages = FindTdefPages(fileBytes, report);
            report.TdefPagesCount = tdefPages.Count;

            int totalRows = 0;

            foreach (var tdefPage in tdefPages)
            {
                try
                {
                    var table = ParseTableDefinition(fileBytes, tdefPage);
                    if (table != null && !string.IsNullOrWhiteSpace(table.Name))
                    {
                        // Exclude system tables unless requested, but store schema
                        if (!table.Name.StartsWith("MSys") && !table.Name.StartsWith("~"))
                        {
                            ReadTableRows(fileBytes, table, report);
                            db.Tables.Add(table);
                            totalRows += table.Rows.Count;
                            report.TableSummaries.Add($"Table '{table.Name}': {table.Columns.Count} cols, {table.Rows.Count} rows.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    report.CorruptPageDetails.Add($"TDEF Page {tdefPage}: Failed to parse table definition ({ex.Message})");
                    report.CorruptPagesCount++;
                }
            }

            // Calculate fragmentation & slack pages
            int usedDataPages = 0;
            for (int i = 1; i < report.TotalPages; i++)
            {
                byte pType = fileBytes[i * PageSize];
                if (pType == 0x01) usedDataPages++;
                else if (pType == 0x00) report.FreeSlackPagesCount++;
            }
            report.DataPagesCount = usedDataPages;

            int totalActivePages = report.TdefPagesCount + report.DataPagesCount;
            if (report.TotalPages > 0)
            {
                report.FragmentationPercentage = Math.Round((double)report.FreeSlackPagesCount / report.TotalPages * 100.0, 2);
            }

            report.StatusSummary = report.CorruptPagesCount > 0
                ? $"Database parsed with {report.CorruptPagesCount} corrupted page warnings. Total tables: {db.Tables.Count}, Rows: {totalRows}."
                : $"Healthy Access 97 Database parsed. Total tables: {db.Tables.Count}, Total rows: {totalRows}, Fragmentation: {report.FragmentationPercentage}%.";

            return db;
        }

        private static List<uint> FindTdefPages(byte[] fileBytes, DiagnosticReport report)
        {
            var tdefs = new List<uint>();
            int totalPages = fileBytes.Length / PageSize;

            for (uint i = 1; i < totalPages; i++)
            {
                int offset = (int)(i * PageSize);
                if (fileBytes[offset] == 0x02 && fileBytes[offset + 1] == 0x01)
                {
                    tdefs.Add(i);
                }
            }
            return tdefs;
        }

        public static AccessTable? ParseTableDefinition(byte[] fileBytes, uint pageNum)
        {
            int pageOffset = (int)(pageNum * PageSize);
            if (pageOffset + PageSize > fileBytes.Length) return null;

            if (fileBytes[pageOffset] != 0x02 || fileBytes[pageOffset + 1] != 0x01) return null;

            int numRecords = BitConverter.ToInt32(fileBytes, pageOffset + 8);
            ushort numCols = BitConverter.ToUInt16(fileBytes, pageOffset + 25);
            ushort numVarCols = BitConverter.ToUInt16(fileBytes, pageOffset + 27);
            ushort numFixedCols = BitConverter.ToUInt16(fileBytes, pageOffset + 29);

            var table = new AccessTable
            {
                TdefPage = pageNum,
                RecordCount = numRecords
            };

            // Column metadata array starts at offset 45 or 0x2D in TDEF page header
            int pos = pageOffset + 45;

            // Read column specifications
            var columns = new List<AccessColumn>();
            for (int i = 0; i < numCols; i++)
            {
                if (pos + 12 >= pageOffset + PageSize) break;

                byte colTypeByte = fileBytes[pos];
                ushort colId = BitConverter.ToUInt16(fileBytes, pos + 1);
                ushort varIdx = BitConverter.ToUInt16(fileBytes, pos + 3);
                ushort fixedOff = BitConverter.ToUInt16(fileBytes, pos + 5);
                ushort len = BitConverter.ToUInt16(fileBytes, pos + 7);
                byte flags = fileBytes[pos + 9];

                pos += 10; // pos now points to column name length

                int nameLen = fileBytes[pos];
                pos++;

                string colName = "Column_" + i;
                if (nameLen > 0 && pos + nameLen <= pageOffset + PageSize)
                {
                    colName = Encoding.ASCII.GetString(fileBytes, pos, nameLen).Replace("\0", "").Trim();
                    pos += nameLen;
                }

                columns.Add(new AccessColumn
                {
                    ColumnId = colId,
                    Name = string.IsNullOrWhiteSpace(colName) ? $"Col_{i}" : colName,
                    DataType = (JetDataType)colTypeByte,
                    VariableIndex = varIdx,
                    FixedOffset = fixedOff,
                    Length = len,
                    IsVariableLength = (flags & 0x01) != 0,
                    IsAutoNumber = (flags & 0x02) != 0,
                    IsNullable = (flags & 0x10) != 0
                });
            }

            table.Columns = columns;

            // Table Name location in TDEF page (after columns/indexes block)
            // Or extract from catalog table. We attempt to read name from end of page or generate clean name.
            if (pos + 2 < pageOffset + PageSize)
            {
                int tableNameLen = fileBytes[pos];
                if (tableNameLen > 0 && tableNameLen < 128 && pos + 1 + tableNameLen <= pageOffset + PageSize)
                {
                    string rawName = Encoding.ASCII.GetString(fileBytes, pos + 1, tableNameLen).Replace("\0", "").Trim();
                    if (!string.IsNullOrWhiteSpace(rawName) && IsValidName(rawName))
                    {
                        table.Name = rawName;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(table.Name))
            {
                table.Name = $"Table_Tdef_{pageNum}";
            }

            return table;
        }

        private static bool IsValidName(string name)
        {
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_' || c == ' ') continue;
                return false;
            }
            return true;
        }

        public static void ReadTableRows(byte[] fileBytes, AccessTable table, DiagnosticReport report)
        {
            int totalPages = fileBytes.Length / PageSize;
            table.Rows.Clear();

            for (uint i = 1; i < totalPages; i++)
            {
                int pageOffset = (int)(i * PageSize);
                if (fileBytes[pageOffset] == 0x01) // Data Page
                {
                    uint ownerTdef = BitConverter.ToUInt32(fileBytes, pageOffset + 4);
                    if (ownerTdef == table.TdefPage)
                    {
                        ushort numSlots = BitConverter.ToUInt16(fileBytes, pageOffset + 8);
                        for (int slot = 0; slot < numSlots; slot++)
                        {
                            int pointerPos = pageOffset + PageSize - ((slot + 1) * 2);
                            if (pointerPos < pageOffset + 12 || pointerPos >= pageOffset + PageSize) continue;

                            ushort recOffset = BitConverter.ToUInt16(fileBytes, pointerPos);
                            // Low bit 0x8000 indicates deleted slot
                            if ((recOffset & 0x8000) != 0 || recOffset == 0) continue;

                            int rowPos = pageOffset + recOffset;
                            if (rowPos >= pageOffset + PageSize) continue;

                            try
                            {
                                var rowData = ParseRowRecord(fileBytes, rowPos, pageOffset + PageSize, table.Columns);
                                if (rowData != null && rowData.Count > 0)
                                {
                                    table.Rows.Add(rowData);
                                }
                            }
                            catch
                            {
                                // Skip corrupted record slot
                            }
                        }
                    }
                }
            }
        }

        private static Dictionary<string, object?> ParseRowRecord(byte[] fileBytes, int rowPos, int pageEndPos, List<AccessColumn> columns)
        {
            var row = new Dictionary<string, object?>();
            if (rowPos >= pageEndPos) return row;

            byte colCount = fileBytes[rowPos];
            int nullMaskBytesCount = (colCount + 7) / 8;
            int pos = rowPos + 1;

            // Fixed columns block starts after row header
            int fixedStartPos = pos;

            // Null mask
            int nullMaskPos = fixedStartPos;
            foreach (var col in columns)
            {
                if (!col.IsVariableLength)
                {
                    nullMaskPos += col.Length;
                }
            }

            byte[] nullMask = new byte[nullMaskBytesCount];
            if (nullMaskPos + nullMaskBytesCount <= pageEndPos)
            {
                Array.Copy(fileBytes, nullMaskPos, nullMask, 0, nullMaskBytesCount);
            }

            // Parse Fixed Length Columns
            foreach (var col in columns)
            {
                if (!col.IsVariableLength)
                {
                    bool isNull = IsColumnNull(nullMask, col.ColumnId);
                    if (isNull)
                    {
                        row[col.Name] = DBNull.Value;
                    }
                    else
                    {
                        int dataPos = fixedStartPos + col.FixedOffset;
                        row[col.Name] = ReadFixedFieldValue(fileBytes, dataPos, col.DataType, col.Length);
                    }
                }
            }

            // Variable Length Columns
            int varStartPos = nullMaskPos + nullMaskBytesCount;
            if (varStartPos < pageEndPos)
            {
                byte varColCount = fileBytes[varStartPos];
                int varOffsetsPos = varStartPos + 1;

                var varCols = columns.FindAll(c => c.IsVariableLength);
                for (int v = 0; v < varCols.Count && v < varColCount; v++)
                {
                    var col = varCols[v];
                    int endOffPos = varOffsetsPos + (v * 2);
                    int startOffPos = v == 0 ? varOffsetsPos + (varColCount * 2) : varOffsetsPos + ((v - 1) * 2);

                    if (endOffPos + 2 <= pageEndPos)
                    {
                        ushort endOffset = BitConverter.ToUInt16(fileBytes, endOffPos);
                        ushort startOffset = v == 0 ? (ushort)0 : BitConverter.ToUInt16(fileBytes, startOffPos);

                        int varDataStart = varOffsetsPos + (varColCount * 2) + startOffset;
                        int varDataLen = endOffset - startOffset;

                        if (varDataStart + varDataLen <= pageEndPos && varDataLen >= 0)
                        {
                            string textVal = Encoding.ASCII.GetString(fileBytes, varDataStart, varDataLen).Replace("\0", "").Trim();
                            row[col.Name] = textVal;
                        }
                        else
                        {
                            row[col.Name] = DBNull.Value;
                        }
                    }
                }
            }

            return row;
        }

        private static bool IsColumnNull(byte[] nullMask, int colId)
        {
            int byteIdx = colId / 8;
            int bitIdx = colId % 8;
            if (byteIdx >= nullMask.Length) return false;
            return (nullMask[byteIdx] & (1 << bitIdx)) == 0;
        }

        private static object? ReadFixedFieldValue(byte[] bytes, int pos, JetDataType type, int length)
        {
            if (pos + length > bytes.Length) return DBNull.Value;

            return type switch
            {
                JetDataType.Boolean => bytes[pos] != 0,
                JetDataType.Byte => bytes[pos],
                JetDataType.Integer => BitConverter.ToInt16(bytes, pos),
                JetDataType.LongInteger or JetDataType.Autonumber => BitConverter.ToInt32(bytes, pos),
                JetDataType.Single => BitConverter.ToSingle(bytes, pos),
                JetDataType.Double => BitConverter.ToDouble(bytes, pos),
                JetDataType.Currency => BitConverter.ToInt64(bytes, pos) / 10000.0m,
                JetDataType.DateTime => ParseOaDate(BitConverter.ToDouble(bytes, pos)),
                _ => BitConverter.ToInt32(bytes, pos)
            };
        }

        private static object ParseOaDate(double oaDate)
        {
            try
            {
                if (oaDate == 0 || double.IsNaN(oaDate)) return DBNull.Value;
                return DateTime.FromOADate(oaDate).ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch
            {
                return DBNull.Value;
            }
        }
    }
}
