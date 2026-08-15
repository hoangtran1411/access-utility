# 10 - Phase 1: Memory & Streaming Optimizations

This guide outlines the technical implementation details for **Phase 1: Zero-Copy Parsing and Streaming Architecture** in AccessUtility.

---

## 🎯 Goal & Problem Statement

Jet 3.5 databases can scale up to 1GB–2GB in production environments. Reading entire files with `File.ReadAllBytes()` or large byte arrays causes:
1. Significant memory allocation spikes on the Large Object Heap (LOH).
2. Garbage Collection (GC) pauses during bulk exports.
3. High working-set memory consumption in containerized or memory-constrained environments.

Phase 1 refactors the core parser to use **`MemoryMappedFile`** and **`ReadOnlySpan<byte>`**, delivering zero heap allocations during binary scanning.

---

## 🏗️ Technical Architecture

```
+───────────────────────────────────────────────────────────────+
|                      OS Page Cache / Disk                     |
+───────────────────────────────┬───────────────────────────────+
                                │ MemoryMappedFile.CreateFromFile
+───────────────────────────────▼───────────────────────────────+
|                      MemoryMappedViewAccessor                 |
|               (Direct pointer to 2048-byte pages)             |
+───────────────────────────────┬───────────────────────────────+
                                │ ReadOnlySpan<byte> (0 heap alloc)
+───────────────────────────────▼───────────────────────────────+
|                   Jet3BinaryReader & Parser                   |
|          • Page 0 Header        • TDEF Table Headers          |
|          • PAM Allocation Map   • Data Row Pointers           |
+───────────────────────────────┬───────────────────────────────+
                                │ IAsyncEnumerable<AccessRow>
+───────────────────────────────▼───────────────────────────────+
|                    Streaming Exporters (Disk)                 |
+───────────────────────────────────────────────────────────────+
```

---

## 💻 Implementation Blueprint

### 1. Memory-Mapped Page Slicing
Instead of loading the entire byte array, read individual 2048-byte pages via pointer view:

```csharp
using System.IO.MemoryMappedFiles;

public sealed class Jet3MemoryReader : IDisposable
{
    private readonly MemoryMappedFile _mmf;
    private readonly MemoryMappedViewAccessor _accessor;
    private readonly long _fileLength;
    private const int PageSize = 2048;

    public Jet3MemoryReader(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        _fileLength = fileInfo.Length;
        _mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
        _accessor = _mmf.CreateViewAccessor(0, _fileLength, MemoryMappedFileAccess.Read);
    }

    public unsafe ReadOnlySpan<byte> GetPage(int pageIndex)
    {
        long offset = (long)pageIndex * PageSize;
        if (offset + PageSize > _fileLength) return ReadOnlySpan<byte>.Empty;

        byte* ptr = null;
        _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        return new ReadOnlySpan<byte>(ptr + offset, PageSize);
    }

    public void Dispose()
    {
        _accessor.Dispose();
        _mmf.Dispose();
    }
}
```

### 2. Streaming Row Extraction (`IAsyncEnumerable`)
Stream rows record-by-record directly to exporters without caching all rows in memory:

```csharp
public async IAsyncEnumerable<AccessRow> StreamTableRowsAsync(string tableName, CancellationToken ct = default)
{
    var tdef = FindTableDefinition(tableName);
    foreach (var dataPage in tdef.DataPagePointers)
    {
        var pageSpan = _memoryReader.GetPage(dataPage);
        foreach (var row in ParseRowsFromPageSpan(pageSpan, tdef))
        {
            if (ct.IsCancellationRequested) yield break;
            yield return row;
        }
    }
}
```

---

## 📈 Benchmarks & Expected Gains

| Metric | Before (Full Array) | After (MemoryMapped + Span) | Improvement |
| :--- | :--- | :--- | :--- |
| **Peak RAM (500MB `.mdb`)** | ~650 MB | **< 25 MB** | **96% Reduction** |
| **GC Gen 2 Collections** | Frequent (LOH fragmentation) | **Zero Gen 2 collections** | **Eliminated** |
| **Startup to First Row** | ~1.2s | **< 15ms** | **80x Faster** |

---

## ⏩ Navigation
- ⬅️ **Previous:** [09 - Recommendations & Future Roadmap](09-recommendations-and-future-roadmap.md)
- ➡️ **Next:** [11 - Phase 2: Modern Analytical Exporters](11-phase2-modern-exporters.md)
- 🔄 **Return to Start:** [00 - Beginner's Guide to AccessUtility](00-beginner-guide.md)
