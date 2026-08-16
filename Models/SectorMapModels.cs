using System;
using System.Collections.Generic;

namespace AccessUtility.Models
{
    public class SectorPageInfo
    {
        public int PageIndex { get; set; }
        public string PageType { get; set; } = "Slack"; // Header, PAM, TDEF, Data, Index, Slack, Corrupt
        public string Status { get; set; } = "Valid";   // Valid, Free, Corrupt
        public string Description { get; set; } = string.Empty;
        public string? OwnerTable { get; set; }
        public uint? TdefPage { get; set; }
        public int? RecordCount { get; set; }
        public int? FreeSpaceBytes { get; set; }
    }

    public class SectorMapReport
    {
        public string FilePath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public int TotalPages { get; set; }
        public int HeaderPages { get; set; }
        public int PamPages { get; set; }
        public int TdefPages { get; set; }
        public int DataPages { get; set; }
        public int IndexPages { get; set; }
        public int SlackPages { get; set; }
        public int CorruptPages { get; set; }
        public List<SectorPageInfo> Pages { get; set; } = new();
    }

    public class HexLine
    {
        public string Offset { get; set; } = string.Empty;
        public string HexPart1 { get; set; } = string.Empty;
        public string HexPart2 { get; set; } = string.Empty;
        public string Ascii { get; set; } = string.Empty;
        public string FormattedLine { get; set; } = string.Empty;
    }

    public class PageHexView
    {
        public int PageIndex { get; set; }
        public long ByteOffset { get; set; }
        public string PageType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<HexLine> HexLines { get; set; } = new();
        public string RawBase64 { get; set; } = string.Empty;
    }

    public class ErdTableColumn
    {
        public string Name { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool IsPrimaryKey { get; set; }
        public bool IsForeignKey { get; set; }
        public string? ForeignTable { get; set; }
        public string? ForeignColumn { get; set; }
    }

    public class ErdTable
    {
        public string Name { get; set; } = string.Empty;
        public List<ErdTableColumn> Columns { get; set; } = new();
    }

    public class ErdRelationship
    {
        public string FromTable { get; set; } = string.Empty;
        public string ToTable { get; set; } = string.Empty;
        public string RelationshipType { get; set; } = "||--o{";
        public string Label { get; set; } = "references";
    }

    public class ErdDiagramResult
    {
        public string DatabasePath { get; set; } = string.Empty;
        public string MermaidCode { get; set; } = string.Empty;
        public string MarkdownContent { get; set; } = string.Empty;
        public int TableCount { get; set; }
        public int RelationshipCount { get; set; }
        public List<ErdTable> Tables { get; set; } = new();
        public List<ErdRelationship> Relationships { get; set; } = new();
    }
}
