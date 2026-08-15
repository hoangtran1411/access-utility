# 13 - Phase 4: Forensic Record Carving & Deleted Data Recovery

This guide outlines the technical implementation details for **Phase 4: Forensic Record Carving and Deep Deleted Data Salvaging** in AccessUtility.

---

## 🎯 Goal & Problem Statement

When tables or records are accidentally dropped or deleted in Microsoft Access 97, Jet 3.5 does not immediately wipe the underlying bytes on disk:
1. Deleted records have their pointer offsets removed from the data page slot table at the end of the 2048-byte page.
2. The raw record body remains in unallocated **slack space** until overwritten by new inserts.
3. Dropped tables have their TDEF pointer cleared from `MSysObjects`, but the physical TDEF and data pages remain scattered throughout the database file.

Phase 4 adds a deep **Forensic Record Carver** to scan unreferenced sectors and reconstruct lost data without requiring active TDEF references.

---

## 🔍 How Jet 3.5 Record Carving Works

```
+─────────────────────────────────────────────────────────────────────────────+
|                    2048-Byte Data Page Structure (Type 0x01)                |
+──────────────────────────┬──────────────────────────────────────┬───────────+
| Page Header (4 Bytes)    | Record Bodies (Slack Space)          | Row Slots |
| [Type 0x01] [Free Ptr]   | [Rec 1] [Rec 2 (Deleted)] [Rec 3]    | [OffsetN] |
+──────────────────────────┴──────────────────────────────────────┴───────────+
                                    ▲
                         Carver scans here for
                       intact column value chains
```

---

## 💻 Implementation Blueprint

### 1. Carver Engine (`ForensicCarver.cs`)
Scans all data pages (`0x01`) and slack pages (`0x00`) for continuous text, numeric, and date sequences:

```csharp
public class CarvedRecord
{
    public int PageIndex { get; set; }
    public int ByteOffset { get; set; }
    public Dictionary<string, object?> SalvagedColumns { get; set; } = new();
    public double ConfidenceScore { get; set; }
}

public static class ForensicCarver
{
    public static List<CarvedRecord> CarvePage(ReadOnlySpan<byte> pageSpan, int pageIndex, AccessTable? expectedSchema)
    {
        var records = new List<CarvedRecord>();

        // 1. Identify active row pointers in slot table (starts from end of page, offset 2046 backward)
        ushort rowCount = BinaryPrimitives.ReadUInt16LittleEndian(pageSpan.Slice(2, 2));
        var activeOffsets = ExtractActiveOffsets(pageSpan, rowCount);

        // 2. Identify unreferenced byte ranges (slack space)
        var slackRanges = CalculateSlackRanges(pageSpan, activeOffsets);

        // 3. Pattern match column definitions against slack byte ranges
        foreach (var range in slackRanges)
        {
            var candidateSpan = pageSpan.Slice(range.Start, range.Length);
            if (TryDecodeRecord(candidateSpan, expectedSchema, out var record))
            {
                record.PageIndex = pageIndex;
                record.ByteOffset = range.Start;
                records.Add(record);
            }
        }

        return records;
    }
}
```

### 2. CLI Carving Command
Users can trigger deep carving on damaged or deleted datasets:

```bash
# Carve deleted rows matching a specific table schema
AccessUtility.exe repair damaged.mdb --carve-deleted --output ./recovered/

# Forensic deep scan over all unallocated space
AccessUtility.exe diagnose database.mdb --forensic-scan
```

### 3. Recovery Output Report
Generates a structured recovery log with confidence metrics:

```
[FORENSIC CARVE REPORT]
Database: sample97.mdb (1,024 pages scanned)
- Active Rows Found: 1,420
- Deleted Rows Salvaged: 86 (Confidence: High > 90%)
- Fragmented Chunks Identified: 14
- Exported salvaged records to: ./recovered/carved_records.sqlite
```

---

## 🛡️ Best Practices for Forensic Data Recovery

1. **Always Work on a Read-Only Copy**: Never carve or repair directly on the original production `.mdb` file.
2. **Examine Orphan Lock Files (`.ldb`)**: Often contains the last connected user and computer name before corruption occurred.
3. **Cross-Reference with OLE Extractor**: Large image or document blobs stored in Long Binary fields often preserve file signatures (`%PDF`, `JFIF`, `BM`) even when table headers are completely destroyed.

---

## ⏩ Navigation
- ⬅️ **Previous:** [12 - Phase 3: Web Dashboard Visualizer & ERD](12-phase3-web-visualizer-and-erd.md)
- 🔄 **Return to Start:** [00 - Beginner's Guide to AccessUtility](00-beginner-guide.md)
