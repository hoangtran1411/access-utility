using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AccessUtility.Models;

namespace AccessUtility.Engine
{
    public static class SectorMapAnalyzer
    {
        public const int PageSize = 2048;

        public static SectorMapReport AnalyzeSectorMap(string filePath)
        {
            var report = new SectorMapReport
            {
                FilePath = filePath
            };

            if (!File.Exists(filePath))
            {
                return report;
            }

            var fileInfo = new FileInfo(filePath);
            report.FileSizeBytes = fileInfo.Length;
            report.TotalPages = (int)(report.FileSizeBytes / PageSize);

            if (report.TotalPages == 0) return report;

            byte[] fileBytes;
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fileBytes = new byte[stream.Length];
                stream.ReadExactly(fileBytes);
            }

            // First pass: locate and parse all TDEF pages to build TDEF -> TableName dictionary
            var tdefTableNames = new Dictionary<uint, string>();
            for (uint i = 0; i < report.TotalPages; i++)
            {
                int offset = (int)(i * PageSize);
                if (offset + 2 <= fileBytes.Length && fileBytes[offset] == 0x02 && fileBytes[offset + 1] == 0x01)
                {
                    try
                    {
                        var table = Jet3BinaryReader.ParseTableDefinition(fileBytes, i);
                        if (table != null && !string.IsNullOrWhiteSpace(table.Name))
                        {
                            tdefTableNames[i] = table.Name;
                        }
                    }
                    catch
                    {
                        // Ignore parse errors on first pass
                    }
                }
            }

            // Second pass: classify all pages
            for (int i = 0; i < report.TotalPages; i++)
            {
                int pageOffset = i * PageSize;
                byte typeByte = fileBytes[pageOffset];
                byte flagByte = pageOffset + 1 < fileBytes.Length ? fileBytes[pageOffset + 1] : (byte)0;

                var pageInfo = new SectorPageInfo
                {
                    PageIndex = i
                };

                if (i == 0)
                {
                    // Header Page
                    string magic = Encoding.ASCII.GetString(fileBytes, Math.Min(4, fileBytes.Length), Math.Min(15, Math.Max(0, fileBytes.Length - 4))).TrimEnd('\0');
                    bool isValidHeader = magic.Contains("Jet DB") || magic.Contains("Standard Jet");

                    pageInfo.PageType = "Header";
                    pageInfo.Status = isValidHeader ? "Valid" : "Corrupt";
                    pageInfo.Description = isValidHeader ? "Jet 3.5 Database Header (Access 97)" : "Corrupted Header Signature";
                    report.HeaderPages++;
                    if (!isValidHeader) report.CorruptPages++;
                }
                else if (i == 1)
                {
                    // Primary Page Allocation Map (PAM)
                    pageInfo.PageType = "PAM";
                    pageInfo.Status = "Valid";
                    pageInfo.Description = "Primary Page Allocation Map (PAM)";
                    report.PamPages++;
                }
                else if (typeByte == 0x02 && flagByte == 0x01)
                {
                    // TDEF Page
                    pageInfo.PageType = "TDEF";
                    pageInfo.Status = "Valid";
                    pageInfo.TdefPage = (uint)i;

                    if (tdefTableNames.TryGetValue((uint)i, out var tableName))
                    {
                        pageInfo.OwnerTable = tableName;
                        int recCount = BitConverter.ToInt32(fileBytes, pageOffset + 8);
                        ushort numCols = BitConverter.ToUInt16(fileBytes, pageOffset + 25);
                        pageInfo.RecordCount = recCount;
                        pageInfo.Description = $"Table Definition: '{tableName}' ({numCols} cols, {recCount} records)";
                    }
                    else
                    {
                        pageInfo.Description = "Table Definition (System/Anonymous)";
                    }
                    report.TdefPages++;
                }
                else if (typeByte == 0x01 && flagByte == 0x01)
                {
                    // Data Page
                    pageInfo.PageType = "Data";
                    pageInfo.Status = "Valid";

                    uint tdefPtr = pageOffset + 8 <= fileBytes.Length ? BitConverter.ToUInt32(fileBytes, pageOffset + 4) : 0;
                    ushort rowCount = pageOffset + 10 <= fileBytes.Length ? BitConverter.ToUInt16(fileBytes, pageOffset + 8) : (ushort)0;
                    ushort freeBytes = pageOffset + 4 <= fileBytes.Length ? BitConverter.ToUInt16(fileBytes, pageOffset + 2) : (ushort)0;

                    pageInfo.TdefPage = tdefPtr;
                    pageInfo.RecordCount = rowCount;
                    pageInfo.FreeSpaceBytes = freeBytes;

                    if (tdefTableNames.TryGetValue(tdefPtr, out var tblName))
                    {
                        pageInfo.OwnerTable = tblName;
                        pageInfo.Description = $"Data Page: Table '{tblName}' ({rowCount} records, {freeBytes}B free)";
                    }
                    else
                    {
                        pageInfo.Description = $"Data Page: TDEF Page #{tdefPtr} ({rowCount} records, {freeBytes}B free)";
                    }
                    report.DataPages++;
                }
                else if (typeByte == 0x03 || typeByte == 0x04)
                {
                    // Index Page
                    pageInfo.PageType = "Index";
                    pageInfo.Status = "Valid";
                    pageInfo.Description = typeByte == 0x03 ? "B-Tree Index Interior Node Page" : "B-Tree Index Leaf Node Page";
                    report.IndexPages++;
                }
                else if (IsAllZeros(fileBytes, pageOffset, PageSize))
                {
                    // Slack / Free unallocated page
                    pageInfo.PageType = "Slack";
                    pageInfo.Status = "Free";
                    pageInfo.FreeSpaceBytes = PageSize;
                    pageInfo.Description = "Unallocated Free Page (Slack Space)";
                    report.SlackPages++;
                }
                else
                {
                    // Corrupted or unrecognized sector
                    pageInfo.PageType = "Corrupt";
                    pageInfo.Status = "Corrupt";
                    pageInfo.Description = $"Corrupt / Unrecognized Sector Header (Type: 0x{typeByte:X2}, Flag: 0x{flagByte:X2})";
                    report.CorruptPages++;
                }

                report.Pages.Add(pageInfo);
            }

            return report;
        }

        public static PageHexView GetPageHexView(string filePath, int pageIndex)
        {
            var view = new PageHexView
            {
                PageIndex = pageIndex,
                ByteOffset = (long)pageIndex * PageSize
            };

            if (!File.Exists(filePath))
            {
                view.Description = "File not found.";
                return view;
            }

            byte[] pageBytes = new byte[PageSize];
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                long offset = (long)pageIndex * PageSize;
                if (offset < stream.Length)
                {
                    stream.Seek(offset, SeekOrigin.Begin);
                    int read = stream.Read(pageBytes, 0, PageSize);
                    if (read < PageSize)
                    {
                        Array.Clear(pageBytes, read, PageSize - read);
                    }
                }
            }

            view.RawBase64 = Convert.ToBase64String(pageBytes);

            // Format Hex Lines (16 bytes per row)
            for (int r = 0; r < PageSize; r += 16)
            {
                string offsetStr = (view.ByteOffset + r).ToString("X6");

                var sbHex1 = new StringBuilder(24);
                for (int c = 0; c < 8; c++)
                {
                    sbHex1.Append(pageBytes[r + c].ToString("X2"));
                    if (c < 7) sbHex1.Append(' ');
                }

                var sbHex2 = new StringBuilder(24);
                for (int c = 8; c < 16; c++)
                {
                    sbHex2.Append(pageBytes[r + c].ToString("X2"));
                    if (c < 15) sbHex2.Append(' ');
                }

                var sbAscii = new StringBuilder(16);
                for (int c = 0; c < 16; c++)
                {
                    byte b = pageBytes[r + c];
                    sbAscii.Append(b >= 32 && b <= 126 ? (char)b : '.');
                }

                string line = $"{offsetStr}  {sbHex1}  {sbHex2}  |{sbAscii}|";
                view.HexLines.Add(new HexLine
                {
                    Offset = offsetStr,
                    HexPart1 = sbHex1.ToString(),
                    HexPart2 = sbHex2.ToString(),
                    Ascii = sbAscii.ToString(),
                    FormattedLine = line
                });
            }

            // Classify single page for metadata
            byte typeByte = pageBytes[0];
            byte flagByte = pageBytes[1];
            if (pageIndex == 0)
            {
                view.PageType = "Header";
                view.Status = "Valid";
                view.Description = "Jet 3.5 Database Header";
            }
            else if (pageIndex == 1)
            {
                view.PageType = "PAM";
                view.Status = "Valid";
                view.Description = "Page Allocation Map";
            }
            else if (typeByte == 0x02 && flagByte == 0x01)
            {
                view.PageType = "TDEF";
                view.Status = "Valid";
                view.Description = "Table Definition Page";
            }
            else if (typeByte == 0x01 && flagByte == 0x01)
            {
                view.PageType = "Data";
                view.Status = "Valid";
                uint tdef = BitConverter.ToUInt32(pageBytes, 4);
                ushort records = BitConverter.ToUInt16(pageBytes, 8);
                view.Description = $"Data Page (TDEF #{tdef}, {records} records)";
            }
            else if (typeByte == 0x03 || typeByte == 0x04)
            {
                view.PageType = "Index";
                view.Status = "Valid";
                view.Description = "B-Tree Index Page";
            }
            else if (IsAllZeros(pageBytes, 0, PageSize))
            {
                view.PageType = "Slack";
                view.Status = "Free";
                view.Description = "Unallocated Free Page (Slack Space)";
            }
            else
            {
                view.PageType = "Corrupt";
                view.Status = "Corrupt";
                view.Description = $"Unrecognized / Damaged Sector (0x{typeByte:X2})";
            }

            return view;
        }

        private static bool IsAllZeros(byte[] bytes, int offset, int length)
        {
            int end = Math.Min(bytes.Length, offset + length);
            for (int i = offset; i < end; i++)
            {
                if (bytes[i] != 0) return false;
            }
            return true;
        }
    }
}
