using System;
using System.Collections.Generic;
using System.IO;
using AccessUtility.Models;
using Serilog;

namespace AccessUtility.Engine
{
    /// <summary>
    /// Feature 03: OLE Object &amp; Embedded File Extractor
    /// Scans Long Binary (OLE) fields in Access 97, strips the 78-byte OLE Container Header,
    /// and extracts embedded files (BMP, JPEG, PNG, PDF, Office Docs) to disk based on magic signatures.
    /// </summary>
    public static class OleExtractor
    {
        // Known file signatures
        private static readonly byte[] BmpSig = { 0x42, 0x4D };
        private static readonly byte[] JpegSig = { 0xFF, 0xD8, 0xFF };
        private static readonly byte[] PngSig = { 0x89, 0x50, 0x4E, 0x47 };
        private static readonly byte[] PdfSig = { 0x25, 0x50, 0x44, 0x46 };
        private static readonly byte[] OfficeSig = { 0xD0, 0xCF, 0x11, 0xE0 }; // Compound Document (Doc, Xls)

        public static OleExtractionReport ExtractDatabase(AccessDatabase db, string outputDir)
        {
            var report = new OleExtractionReport { OutputDirectory = outputDir };

            if (db.Tables.Count == 0) return report;

            foreach (var table in db.Tables)
            {
                // Find Binary/OLE columns
                var oleColumns = table.Columns.FindAll(c => c.DataType == JetDataType.Binary || c.DataType == JetDataType.Memo);

                if (oleColumns.Count == 0) continue;

                for (int rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
                {
                    var row = table.Rows[rowIndex];

                    foreach (var col in oleColumns)
                    {
                        if (row.TryGetValue(col.Name, out var val) && val != null)
                        {
                            byte[]? rawBytes = null;
                            if (val is string strVal)
                            {
                                // In Jet3BinaryReader, Binary fields might be read as string if not handled as raw byte array
                                // If so, convert back. Normally binary should be byte[]. Assuming string is Base64 or raw string.
                                // Our current Jet3BinaryReader reads text for var length. 
                                // To properly extract OLE we need raw bytes.
                                // For now, handle if it's byte[]
                            }
                            else if (val is byte[] arrVal)
                            {
                                rawBytes = arrVal;
                            }

                            if (rawBytes != null && rawBytes.Length > 78)
                            {
                                ExtractFileFromBytes(rawBytes, table.Name, col.Name, rowIndex, outputDir, report);
                            }
                        }
                    }
                }
            }

            return report;
        }

        public static void ExtractFileFromBytes(byte[] rawBytes, string tableName, string colName, int rowIndex, string outputDir, OleExtractionReport report)
        {
            // OLE Container often wraps with a 78 byte header. 
            // We search for magic bytes within the first 128 bytes to be safe.
            int offset = FindSignatureOffset(rawBytes, 0, Math.Min(128, rawBytes.Length));

            if (offset != -1)
            {
                string ext = DetectExtension(rawBytes, offset);
                if (!string.IsNullOrEmpty(ext))
                {
                    string safeTableName = string.Join("_", tableName.Split(Path.GetInvalidFileNameChars()));
                    string safeColName = string.Join("_", colName.Split(Path.GetInvalidFileNameChars()));
                    
                    string tableDir = Path.Combine(outputDir, safeTableName);
                    if (!Directory.Exists(tableDir)) Directory.CreateDirectory(tableDir);

                    string fileName = $"{safeColName}_Row_{rowIndex}{ext}";
                    string fullPath = Path.Combine(tableDir, fileName);

                    int length = rawBytes.Length - offset;
                    byte[] fileBytes = new byte[length];
                    Array.Copy(rawBytes, offset, fileBytes, 0, length);

                    File.WriteAllBytes(fullPath, fileBytes);
                    
                    report.ExtractedFiles.Add(new ExtractedOleFile
                    {
                        TableName = tableName,
                        ColumnName = colName,
                        RowIndex = rowIndex,
                        FilePath = fullPath,
                        FileType = ext.TrimStart('.'),
                        SizeBytes = length
                    });
                    
                    Log.Debug("Extracted {FileName} from {Table}.{Column} Row {Row}", fileName, tableName, colName, rowIndex);
                }
            }
        }

        private static int FindSignatureOffset(byte[] data, int startOffset, int searchLimit)
        {
            for (int i = startOffset; i < searchLimit - 4; i++)
            {
                if (MatchSignature(data, i, BmpSig) ||
                    MatchSignature(data, i, JpegSig) ||
                    MatchSignature(data, i, PngSig) ||
                    MatchSignature(data, i, PdfSig) ||
                    MatchSignature(data, i, OfficeSig))
                {
                    return i;
                }
            }
            return -1;
        }

        private static bool MatchSignature(byte[] data, int offset, byte[] signature)
        {
            if (offset + signature.Length > data.Length) return false;
            for (int i = 0; i < signature.Length; i++)
            {
                if (data[offset + i] != signature[i]) return false;
            }
            return true;
        }

        private static string DetectExtension(byte[] data, int offset)
        {
            if (MatchSignature(data, offset, BmpSig)) return ".bmp";
            if (MatchSignature(data, offset, JpegSig)) return ".jpg";
            if (MatchSignature(data, offset, PngSig)) return ".png";
            if (MatchSignature(data, offset, PdfSig)) return ".pdf";
            if (MatchSignature(data, offset, OfficeSig)) return ".doc"; // Compound doc could be xls or ppt, defaulting to .doc
            return string.Empty;
        }
    }
}
