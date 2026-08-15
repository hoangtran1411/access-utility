using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using AccessUtility.Models;

namespace AccessUtility.Engine
{
    /// <summary>
    /// Feature 09: Zero-Copy MemoryMappedFile &amp; Streaming Engine
    /// Provides zero-allocation 2048-byte page slicing directly from the OS page cache
    /// and IAsyncEnumerable record streaming.
    /// </summary>
    public sealed unsafe class Jet3MemoryReader : IDisposable
    {
        public const int PageSize = 2048;

        private readonly FileStream _fileStream;
        private readonly MemoryMappedFile _mmf;
        private readonly MemoryMappedViewAccessor _accessor;
        private byte* _pointer;
        private bool _disposed;

        public string FilePath { get; }
        public long FileSizeBytes { get; }
        public int TotalPages { get; }
        public bool IsValid => !_disposed && _pointer != null && FileSizeBytes >= PageSize;

        public Jet3MemoryReader(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Database file not found: {filePath}", filePath);
            }

            FilePath = filePath;
            var fileInfo = new FileInfo(filePath);
            FileSizeBytes = fileInfo.Length;
            TotalPages = (int)(FileSizeBytes / PageSize);

            _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _mmf = MemoryMappedFile.CreateFromFile(_fileStream, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, leaveOpen: false);
            _accessor = _mmf.CreateViewAccessor(0, FileSizeBytes, MemoryMappedFileAccess.Read);

            _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref _pointer);
        }

        /// <summary>
        /// Retrieves a zero-copy ReadOnlySpan over the specified 2048-byte page.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> GetPage(int pageIndex)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            long offset = (long)pageIndex * PageSize;
            if (offset < 0 || offset + PageSize > FileSizeBytes || _pointer == null)
            {
                return ReadOnlySpan<byte>.Empty;
            }

            return new ReadOnlySpan<byte>(_pointer + offset, PageSize);
        }

        /// <summary>
        /// Retrieves a zero-copy ReadOnlySpan over the specified 2048-byte page.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> GetPage(uint pageIndex) => GetPage((int)pageIndex);

        /// <summary>
        /// Copies the specified page to a new byte array when an isolated buffer is required.
        /// </summary>
        public byte[] ReadPageCopy(int pageIndex)
        {
            var span = GetPage(pageIndex);
            return span.IsEmpty ? Array.Empty<byte>() : span.ToArray();
        }

        /// <summary>
        /// Streams table rows record-by-record without buffering all records in memory.
        /// </summary>
        public IEnumerable<AccessRow> StreamTableRows(AccessTable table)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            for (int i = 1; i < TotalPages; i++)
            {
                var rows = ParsePageRows(i, table);
                foreach (var row in rows)
                {
                    yield return row;
                }
            }
        }

        /// <summary>
        /// Asynchronously streams table rows record-by-record with cancellation support.
        /// </summary>
        public async IAsyncEnumerable<AccessRow> StreamTableRowsAsync(AccessTable table, [EnumeratorCancellation] CancellationToken ct = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            for (int i = 1; i < TotalPages; i++)
            {
                if (ct.IsCancellationRequested) yield break;

                var rows = ParsePageRows(i, table);
                foreach (var row in rows)
                {
                    if (ct.IsCancellationRequested) yield break;
                    yield return row;
                }

                await Task.Yield();
            }
        }

        private List<AccessRow> ParsePageRows(int pageIndex, AccessTable table)
        {
            var results = new List<AccessRow>();
            var pageSpan = GetPage(pageIndex);
            if (pageSpan.IsEmpty || pageSpan[0] != 0x01) return results; // Must be Data Page (0x01)

            uint ownerTdef = BinaryPrimitives.ReadUInt32LittleEndian(pageSpan.Slice(4, 4));
            if (ownerTdef != table.TdefPage) return results;

            ushort numSlots = BinaryPrimitives.ReadUInt16LittleEndian(pageSpan.Slice(8, 2));
            for (int slot = 0; slot < numSlots; slot++)
            {
                int pointerPos = PageSize - ((slot + 1) * 2);
                if (pointerPos < 12 || pointerPos >= PageSize) continue;

                ushort recOffset = BinaryPrimitives.ReadUInt16LittleEndian(pageSpan.Slice(pointerPos, 2));
                if ((recOffset & 0x8000) != 0 || recOffset == 0 || recOffset >= PageSize) continue;

                var row = ParseRowFromSpan(pageSpan, recOffset, table.Columns);
                if (row != null && row.Count > 0)
                {
                    results.Add(row);
                }
            }

            return results;
        }

        private static AccessRow? ParseRowFromSpan(ReadOnlySpan<byte> pageSpan, int rowOffset, List<AccessColumn> columns)
        {
            try
            {
                if (rowOffset >= pageSpan.Length) return null;

                var row = new AccessRow();
                byte colCount = pageSpan[rowOffset];
                int nullMaskBytesCount = (colCount + 7) / 8;
                int pos = rowOffset + 1;

                int fixedStartPos = pos;
                int nullMaskPos = fixedStartPos;

                foreach (var col in columns)
                {
                    if (!col.IsVariableLength)
                    {
                        nullMaskPos += col.Length;
                    }
                }

                if (nullMaskPos + nullMaskBytesCount > pageSpan.Length) return null;
                var nullMask = pageSpan.Slice(nullMaskPos, nullMaskBytesCount);

                // Fixed Length Columns
                foreach (var col in columns)
                {
                    if (!col.IsVariableLength)
                    {
                        bool isNull = IsColumnNull(nullMask, col.ColumnId);
                        if (isNull)
                        {
                            row.Values[col.Name] = DBNull.Value;
                        }
                        else
                        {
                            int dataPos = fixedStartPos + col.FixedOffset;
                            row.Values[col.Name] = ReadFixedFieldSpan(pageSpan, dataPos, col.DataType, col.Length);
                        }
                    }
                }

                // Variable Length Columns
                int varStartPos = nullMaskPos + nullMaskBytesCount;
                if (varStartPos < pageSpan.Length)
                {
                    byte varColCount = pageSpan[varStartPos];
                    int varOffsetsPos = varStartPos + 1;

                    var varCols = columns.FindAll(c => c.IsVariableLength);
                    for (int v = 0; v < varCols.Count && v < varColCount; v++)
                    {
                        var col = varCols[v];
                        int endOffPos = varOffsetsPos + (v * 2);
                        int startOffPos = v == 0 ? varOffsetsPos + (varColCount * 2) : varOffsetsPos + ((v - 1) * 2);

                        if (endOffPos + 2 <= pageSpan.Length)
                        {
                            ushort endOffset = BinaryPrimitives.ReadUInt16LittleEndian(pageSpan.Slice(endOffPos, 2));
                            ushort startOffset = v == 0 ? (ushort)0 : BinaryPrimitives.ReadUInt16LittleEndian(pageSpan.Slice(startOffPos, 2));

                            int varDataStart = varOffsetsPos + (varColCount * 2) + startOffset;
                            int varDataLen = endOffset - startOffset;

                            if (varDataStart + varDataLen <= pageSpan.Length && varDataLen >= 0)
                            {
                                if (col.DataType == JetDataType.Binary)
                                {
                                    row.Values[col.Name] = pageSpan.Slice(varDataStart, varDataLen).ToArray();
                                }
                                else
                                {
                                    string textVal = Encoding.ASCII.GetString(pageSpan.Slice(varDataStart, varDataLen)).Replace("\0", "").Trim();
                                    row.Values[col.Name] = textVal;
                                }
                            }
                            else
                            {
                                row.Values[col.Name] = DBNull.Value;
                            }
                        }
                    }
                }

                return row;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsColumnNull(ReadOnlySpan<byte> nullMask, int colId)
        {
            int byteIdx = colId / 8;
            int bitIdx = colId % 8;
            if (byteIdx >= nullMask.Length) return false;
            return (nullMask[byteIdx] & (1 << bitIdx)) == 0;
        }

        private static object? ReadFixedFieldSpan(ReadOnlySpan<byte> span, int pos, JetDataType type, int length)
        {
            if (pos + length > span.Length) return DBNull.Value;

            return type switch
            {
                JetDataType.Boolean => span[pos] != 0,
                JetDataType.Byte => span[pos],
                JetDataType.Integer => BinaryPrimitives.ReadInt16LittleEndian(span.Slice(pos, 2)),
                JetDataType.LongInteger or JetDataType.Autonumber => BinaryPrimitives.ReadInt32LittleEndian(span.Slice(pos, 4)),
                JetDataType.Single => BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(pos, 4))),
                JetDataType.Double => BitConverter.UInt64BitsToDouble(BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(pos, 8))),
                JetDataType.Currency => BinaryPrimitives.ReadInt64LittleEndian(span.Slice(pos, 8)) / 10000.0m,
                JetDataType.DateTime => ParseOaDate(BitConverter.UInt64BitsToDouble(BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(pos, 8)))),
                _ => BinaryPrimitives.ReadInt32LittleEndian(span.Slice(pos, Math.Min(4, length)))
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

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_pointer != null)
            {
                _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                _pointer = null;
            }

            _accessor.Dispose();
            _mmf.Dispose();
            _fileStream.Dispose();
        }
    }
}
