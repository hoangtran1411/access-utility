using System;
using System.IO;
using System.Text;
using AccessUtility.Models;

namespace AccessUtility.Engine
{
    public class CompactResult
    {
        public string SourcePath { get; set; } = string.Empty;
        public string TargetPath { get; set; } = string.Empty;
        public long OriginalSizeBytes { get; set; }
        public long CompactedSizeBytes { get; set; }
        public long SpaceSavedBytes => OriginalSizeBytes - CompactedSizeBytes;
        public double ReductionPercentage => OriginalSizeBytes > 0 ? Math.Round((double)SpaceSavedBytes / OriginalSizeBytes * 100.0, 2) : 0;
        public int TotalTablesCompacted { get; set; }
        public int TotalRowsPreserved { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public static class Jet3Compactor
    {
        public const int PageSize = 2048;

        public static CompactResult Compact(string sourcePath, string targetPath, bool forceUnlock = false)
        {
            var result = new CompactResult
            {
                SourcePath = sourcePath,
                TargetPath = targetPath
            };

            if (!File.Exists(sourcePath))
            {
                result.Success = false;
                result.Message = $"Source file '{sourcePath}' does not exist.";
                return result;
            }

            // Lock Check
            var lockInfo = LdbLockInspector.Inspect(sourcePath);
            if (lockInfo.IsFileInUse && !forceUnlock)
            {
                result.Success = false;
                result.Message = $"Cannot compact database: File is currently locked by active users ({lockInfo.ConnectedUsers.Count} connected).";
                return result;
            }

            if (lockInfo.IsOrphanLock)
            {
                LdbLockInspector.TryCleanOrphanLock(sourcePath, out _);
            }

            try
            {
                FileInfo srcInfo = new FileInfo(sourcePath);
                result.OriginalSizeBytes = srcInfo.Length;

                // Read all database content
                var db = Jet3BinaryReader.ReadDatabase(sourcePath, out var diag);

                // Build clean target database stream
                using (var ms = new MemoryStream())
                {
                    // Write Page 0 (Header)
                    byte[] headerPage = CreateHeaderPage();
                    ms.Write(headerPage, 0, PageSize);

                    // Write Page 1 (PAM - Page Allocation Map)
                    byte[] pamPage = new byte[PageSize];
                    pamPage[0] = 0x05; // PAM page type
                    pamPage[1] = 0x01;
                    ms.Write(pamPage, 0, PageSize);

                    int tableCount = 0;
                    int rowCount = 0;

                    foreach (var table in db.Tables)
                    {
                        if (table.Columns.Count == 0) continue;

                        uint tdefPageNum = (uint)(ms.Position / PageSize);
                        table.TdefPage = tdefPageNum;

                        // Write Table Definition Page
                        byte[] tdefBytes = CreateTdefPage(table);
                        ms.Write(tdefBytes, 0, PageSize);

                        // Write Data Pages continuously
                        int rowsInPage = 0;
                        MemoryStream dataPageMs = CreateNewDataPage(tdefPageNum);

                        foreach (var row in table.Rows)
                        {
                            byte[] recordBytes = SerializeRow(row, table.Columns);
                            if (dataPageMs.Position + recordBytes.Length + 4 > PageSize - 32)
                            {
                                // Flush full data page
                                FinalizeAndWriteDataPage(ms, dataPageMs);
                                dataPageMs = CreateNewDataPage(tdefPageNum);
                            }

                            AppendRecordToDataPage(dataPageMs, recordBytes);
                            rowsInPage++;
                            rowCount++;
                        }

                        if (rowsInPage > 0 || table.Rows.Count == 0)
                        {
                            FinalizeAndWriteDataPage(ms, dataPageMs);
                        }

                        tableCount++;
                    }

                    result.TotalTablesCompacted = tableCount;
                    result.TotalRowsPreserved = rowCount;

                    // Ensure directory exists for target
                    string targetDir = Path.GetDirectoryName(Path.GetFullPath(targetPath)) ?? ".";
                    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                    // Write out clean file
                    File.WriteAllBytes(targetPath, ms.ToArray());
                }

                FileInfo destInfo = new FileInfo(targetPath);
                result.CompactedSizeBytes = destInfo.Length;
                result.Success = true;
                result.Message = $"Database successfully compacted. Reduced from {result.OriginalSizeBytes:N0} bytes to {result.CompactedSizeBytes:N0} bytes ({result.ReductionPercentage}% saved).";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Compaction error: {ex.Message}";
            }

            return result;
        }

        public static void WriteDatabase(AccessDatabase db, string targetPath)
        {
            using (var ms = new MemoryStream())
            {
                // Write Page 0 (Header)
                byte[] headerPage = CreateHeaderPage();
                ms.Write(headerPage, 0, PageSize);

                // Write Page 1 (PAM - Page Allocation Map)
                byte[] pamPage = new byte[PageSize];
                pamPage[0] = 0x05; // PAM page type
                pamPage[1] = 0x01;
                ms.Write(pamPage, 0, PageSize);

                foreach (var table in db.Tables)
                {
                    if (table.Columns.Count == 0) continue;

                    uint tdefPageNum = (uint)(ms.Position / PageSize);
                    table.TdefPage = tdefPageNum;

                    // Write Table Definition Page
                    byte[] tdefBytes = CreateTdefPage(table);
                    ms.Write(tdefBytes, 0, PageSize);

                    // Write Data Pages continuously
                    int rowsInPage = 0;
                    MemoryStream dataPageMs = CreateNewDataPage(tdefPageNum);

                    foreach (var row in table.Rows)
                    {
                        byte[] recordBytes = SerializeRow(row, table.Columns);
                        if (dataPageMs.Position + recordBytes.Length + 4 > PageSize - 32)
                        {
                            // Flush full data page
                            FinalizeAndWriteDataPage(ms, dataPageMs);
                            dataPageMs = CreateNewDataPage(tdefPageNum);
                        }

                        AppendRecordToDataPage(dataPageMs, recordBytes);
                        rowsInPage++;
                    }

                    if (rowsInPage > 0 || table.Rows.Count == 0)
                    {
                        FinalizeAndWriteDataPage(ms, dataPageMs);
                    }
                }

                string targetDir = Path.GetDirectoryName(Path.GetFullPath(targetPath)) ?? ".";
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                File.WriteAllBytes(targetPath, ms.ToArray());
            }
        }

        private static byte[] CreateHeaderPage()
        {
            byte[] header = new byte[PageSize];
            header[0] = 0x00;
            header[1] = 0x01;
            header[2] = 0x00;
            header[3] = 0x00;

            byte[] magic = Encoding.ASCII.GetBytes("Standard Jet DB\0");
            Array.Copy(magic, 0, header, 4, magic.Length);

            header[0x14] = 0x01; // Jet 3.5 engine version byte
            return header;
        }

        private static byte[] CreateTdefPage(AccessTable table)
        {
            byte[] tdef = new byte[PageSize];
            tdef[0] = 0x02; // TDEF magic
            tdef[1] = 0x01;

            // Num records
            byte[] recCountBytes = BitConverter.GetBytes(table.Rows.Count);
            Array.Copy(recCountBytes, 0, tdef, 8, 4);

            // Num cols
            ushort numCols = (ushort)table.Columns.Count;
            byte[] numColsBytes = BitConverter.GetBytes(numCols);
            Array.Copy(numColsBytes, 0, tdef, 25, 2);

            int pos = 45;
            for (int i = 0; i < table.Columns.Count; i++)
            {
                var col = table.Columns[i];
                tdef[pos] = (byte)col.DataType;

                byte[] colId = BitConverter.GetBytes((ushort)i);
                Array.Copy(colId, 0, tdef, pos + 1, 2);

                byte[] varIdx = BitConverter.GetBytes((ushort)col.VariableIndex);
                Array.Copy(varIdx, 0, tdef, pos + 3, 2);

                byte[] fixOff = BitConverter.GetBytes((ushort)col.FixedOffset);
                Array.Copy(fixOff, 0, tdef, pos + 5, 2);

                byte[] len = BitConverter.GetBytes((ushort)col.Length);
                Array.Copy(len, 0, tdef, pos + 7, 2);

                pos += 10;

                byte[] nameBytes = Encoding.ASCII.GetBytes(col.Name);
                tdef[pos] = (byte)nameBytes.Length;
                pos++;

                Array.Copy(nameBytes, 0, tdef, pos, nameBytes.Length);
                pos += nameBytes.Length;
            }

            // Write Table Name at end of columns
            byte[] tableNameBytes = Encoding.ASCII.GetBytes(table.Name);
            tdef[pos] = (byte)tableNameBytes.Length;
            Array.Copy(tableNameBytes, 0, tdef, pos + 1, tableNameBytes.Length);

            return tdef;
        }

        private static MemoryStream CreateNewDataPage(uint ownerTdef)
        {
            byte[] page = new byte[PageSize];
            page[0] = 0x01; // Data Page
            page[1] = 0x01;

            byte[] ownerBytes = BitConverter.GetBytes(ownerTdef);
            Array.Copy(ownerBytes, 0, page, 4, 4);

            var ms = new MemoryStream();
            ms.Write(page, 0, PageSize);
            ms.Position = 12; // Data row records start at offset 12
            return ms;
        }

        private static void AppendRecordToDataPage(MemoryStream dataPageMs, byte[] recordBytes)
        {
            dataPageMs.Write(recordBytes, 0, recordBytes.Length);
        }

        private static void FinalizeAndWriteDataPage(MemoryStream targetFileMs, MemoryStream dataPageMs)
        {
            byte[] pageBytes = dataPageMs.ToArray();
            targetFileMs.Write(pageBytes, 0, PageSize);
        }

        private static byte[] SerializeRow(System.Collections.Generic.Dictionary<string, object?> row, System.Collections.Generic.List<AccessColumn> columns)
        {
            using var ms = new MemoryStream();
            ms.WriteByte((byte)columns.Count);

            // Write fixed length fields
            foreach (var col in columns)
            {
                if (!col.IsVariableLength)
                {
                    row.TryGetValue(col.Name, out var val);
                    byte[] fieldBytes = SerializeFixedValue(val, col.DataType, col.Length);
                    ms.Write(fieldBytes, 0, fieldBytes.Length);
                }
            }

            // Write null mask
            int nullMaskLen = (columns.Count + 7) / 8;
            byte[] nullMask = new byte[nullMaskLen];
            for (int i = 0; i < columns.Count; i++)
            {
                var col = columns[i];
                row.TryGetValue(col.Name, out var val);
                if (val != null && val != DBNull.Value)
                {
                    nullMask[i / 8] |= (byte)(1 << (i % 8));
                }
            }
            ms.Write(nullMask, 0, nullMask.Length);

            // Write variable length fields
            var varCols = columns.FindAll(c => c.IsVariableLength);
            ms.WriteByte((byte)varCols.Count);

            using var varDataMs = new MemoryStream();
            ushort currentOff = 0;

            foreach (var col in varCols)
            {
                row.TryGetValue(col.Name, out var val);
                string text = val?.ToString() ?? "";
                byte[] textBytes = Encoding.ASCII.GetBytes(text);
                varDataMs.Write(textBytes, 0, textBytes.Length);

                currentOff += (ushort)textBytes.Length;
                byte[] offBytes = BitConverter.GetBytes(currentOff);
                ms.Write(offBytes, 0, 2);
            }

            byte[] varData = varDataMs.ToArray();
            ms.Write(varData, 0, varData.Length);

            return ms.ToArray();
        }

        private static byte[] SerializeFixedValue(object? val, JetDataType type, int length)
        {
            byte[] buffer = new byte[length];
            if (val == null || val == DBNull.Value) return buffer;

            try
            {
                switch (type)
                {
                    case JetDataType.Boolean:
                        buffer[0] = (byte)(Convert.ToBoolean(val) ? 1 : 0);
                        break;
                    case JetDataType.Byte:
                        buffer[0] = Convert.ToByte(val);
                        break;
                    case JetDataType.Integer:
                        BitConverter.GetBytes(Convert.ToInt16(val)).CopyTo(buffer, 0);
                        break;
                    case JetDataType.LongInteger or JetDataType.Autonumber:
                        BitConverter.GetBytes(Convert.ToInt32(val)).CopyTo(buffer, 0);
                        break;
                    case JetDataType.Single:
                        BitConverter.GetBytes(Convert.ToSingle(val)).CopyTo(buffer, 0);
                        break;
                    case JetDataType.Double:
                        BitConverter.GetBytes(Convert.ToDouble(val)).CopyTo(buffer, 0);
                        break;
                    case JetDataType.Currency:
                        long currVal = (long)(Convert.ToDecimal(val) * 10000m);
                        BitConverter.GetBytes(currVal).CopyTo(buffer, 0);
                        break;
                    case JetDataType.DateTime:
                        if (DateTime.TryParse(val.ToString(), out var dt))
                        {
                            BitConverter.GetBytes(dt.ToOADate()).CopyTo(buffer, 0);
                        }
                        break;
                }
            }
            catch
            {
                // Fallback to zeroed buffer
            }

            return buffer;
        }
    }
}
