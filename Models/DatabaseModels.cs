using System;
using System.Collections.Generic;

namespace AccessUtility.Models
{
    public enum JetDataType : byte
    {
        Unknown = 0,
        Boolean = 1,
        Byte = 2,
        Integer = 3,
        LongInteger = 4,
        Currency = 5,
        Single = 6,
        Double = 7,
        DateTime = 8,
        Binary = 9,
        Text = 10,
        Memo = 11,
        Autonumber = 12,
        Guid = 13
    }

    public class AccessColumn
    {
        public string Name { get; set; } = string.Empty;
        public JetDataType DataType { get; set; } = JetDataType.Unknown;
        public int ColumnId { get; set; }
        public int VariableIndex { get; set; }
        public int FixedOffset { get; set; }
        public int Length { get; set; }
        public bool IsVariableLength { get; set; }
        public bool IsAutoNumber { get; set; }
        public bool IsNullable { get; set; }
    }

    public class AccessTable
    {
        public string Name { get; set; } = string.Empty;
        public int RecordCount { get; set; }
        public uint TdefPage { get; set; }
        public List<AccessColumn> Columns { get; set; } = new();
        public List<Dictionary<string, object?>> Rows { get; set; } = new();
    }

    public class AccessDatabase
    {
        public string FilePath { get; set; } = string.Empty;
        public string JetVersion { get; set; } = "Jet 3.5 (Access 97)";
        public int PageSize { get; set; } = 2048;
        public List<AccessTable> Tables { get; set; } = new();
    }

    public class LdbLockEntry
    {
        public int EntryIndex { get; set; }
        public string ComputerName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class LockFileInfo
    {
        public string LdbPath { get; set; } = string.Empty;
        public bool Exists { get; set; }
        public bool IsFileInUse { get; set; }
        public bool IsOrphanLock { get; set; }
        public List<LdbLockEntry> ConnectedUsers { get; set; } = new();
    }

    public class CleanLockResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ExportResponse
    {
        public bool Success { get; set; }
        public string OutputPath { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
    }

    public class DiagnosticReport
    {
        public string FilePath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public int TotalPages { get; set; }
        public int DataPagesCount { get; set; }
        public int TdefPagesCount { get; set; }
        public int FreeSlackPagesCount { get; set; }
        public int CorruptPagesCount { get; set; }
        public double FragmentationPercentage { get; set; }
        public LockFileInfo LockInfo { get; set; } = new();
        public List<string> CorruptPageDetails { get; set; } = new();
        public List<string> TableSummaries { get; set; } = new();
        public string StatusSummary { get; set; } = string.Empty;
    }
}
