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

    // ── Feature 01: Database Password & Security Inspector ────────────────────

    public enum WorkgroupAccountType { User, Group, Unknown }

    /// <summary>
    /// Result of inspecting Access 97 database security settings from Page 0.
    /// </summary>
    public class SecurityInspectionResult
    {
        public string DatabasePath { get; set; } = string.Empty;
        public bool IsValidJetDatabase { get; set; }
        public string JetVersion { get; set; } = string.Empty;

        /// <summary>Plaintext password decrypted from Page 0 offset 0x42 using Jet3 XOR mask. Null if no password set.</summary>
        public string? DatabasePassword { get; set; }
        public bool IsPasswordProtected { get; set; }

        /// <summary>Hex representation of the 8-byte owner SID at Page 0 offset 0x5A.</summary>
        public string DatabaseOwnerSid { get; set; } = string.Empty;

        /// <summary>True if User-Level Security (ULS) flag is enabled (Page 0 offset 0x5C bit 0x08).</summary>
        public bool HasUserLevelSecurity { get; set; }

        /// <summary>True if XOR/RC4 encryption-at-rest is enabled (Page 0 offset 0x12 bit 0x04).</summary>
        public bool IsEncryptedAtRest { get; set; }

        public string InspectionStatus { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// A user account parsed from a System.mdw workgroup file.
    /// </summary>
    public class WorkgroupUser
    {
        public string AccountName { get; set; } = string.Empty;
        public string Sid { get; set; } = string.Empty;
        public WorkgroupAccountType AccountType { get; set; } = WorkgroupAccountType.User;
    }

    /// <summary>
    /// A security group parsed from a System.mdw workgroup file.
    /// </summary>
    public class WorkgroupGroup
    {
        public string GroupName { get; set; } = string.Empty;
        public List<string> MemberNames { get; set; } = new();
    }

    /// <summary>
    /// Result of parsing a System.mdw Access workgroup file.
    /// </summary>
    public class WorkgroupInspectionResult
    {
        public string WorkgroupPath { get; set; } = string.Empty;
        public bool IsValidWorkgroupFile { get; set; }
        public string WorkgroupId { get; set; } = string.Empty;
        public List<WorkgroupUser> Users { get; set; } = new();
        public List<WorkgroupGroup> Groups { get; set; } = new();
        public string InspectionStatus { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    // ── Feature 02: Schema Diff & Migration Generator ─────────────────────────

    public enum TableDiffType { Added, Removed, Modified }
    public enum ColumnDiffType { Added, Removed, Modified }

    /// <summary>A snapshot of a column's schema at a point in time.</summary>
    public class ColumnSnapshot
    {
        public string Name { get; set; } = string.Empty;
        public JetDataType DataType { get; set; }
        public int Length { get; set; }
        public bool IsNullable { get; set; }
        public bool IsAutoNumber { get; set; }
        public bool IsVariableLength { get; set; }
    }

    /// <summary>Describes a single column-level schema change.</summary>
    public class ColumnDiff
    {
        public string ColumnName { get; set; } = string.Empty;
        public ColumnDiffType DiffType { get; set; }
        public JetDataType OldDataType { get; set; }
        public JetDataType NewDataType { get; set; }
        public int OldLength { get; set; }
        public int NewLength { get; set; }
        public bool IsNullable { get; set; }
        public bool TypeChanged { get; set; }
        public bool LengthChanged { get; set; }
        public bool NullableChanged { get; set; }
    }

    /// <summary>Describes a table-level schema difference (added, removed, or modified with column diffs).</summary>
    public class TableDiff
    {
        public string TableName { get; set; } = string.Empty;
        public TableDiffType DiffType { get; set; }
        public List<ColumnSnapshot> Columns { get; set; } = new();
        public List<ColumnDiff> AddedColumns { get; set; } = new();
        public List<ColumnDiff> RemovedColumns { get; set; } = new();
        public List<ColumnDiff> ModifiedColumns { get; set; } = new();
        public int SourceRowCount { get; set; }
        public int TargetRowCount { get; set; }
        public int RowCountDifference { get; set; }
    }

    /// <summary>Full result of comparing two Access 97 database schemas.</summary>
    public class SchemaDiffResult
    {
        public string SourcePath { get; set; } = string.Empty;
        public string TargetPath { get; set; } = string.Empty;
        public bool HasDifferences { get; set; }
        public List<TableDiff> AddedTables { get; set; } = new();
        public List<TableDiff> RemovedTables { get; set; } = new();
        public List<TableDiff> ModifiedTables { get; set; } = new();
    }

    // ── Feature 03: OLE Object & Embedded File Extractor ──────────────────────

    public class ExtractedOleFile
    {
        public string TableName { get; set; } = string.Empty;
        public string ColumnName { get; set; } = string.Empty;
        public int RowIndex { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty; // e.g. bmp, pdf, doc
        public int SizeBytes { get; set; }
    }

    public class OleExtractionReport
    {
        public string OutputDirectory { get; set; } = string.Empty;
        public List<ExtractedOleFile> ExtractedFiles { get; set; } = new();
    }

    // ── Feature 04: Access Query SQL Extractor ───────────────────────────────

    public class ExtractedQuery
    {
        public string Name { get; set; } = string.Empty;
        public int ObjectId { get; set; }
        public string SqlText { get; set; } = string.Empty;
    }

    public class QueryExtractionReport
    {
        public string OutputDirectory { get; set; } = string.Empty;
        public List<ExtractedQuery> Queries { get; set; } = new();
    }
}
