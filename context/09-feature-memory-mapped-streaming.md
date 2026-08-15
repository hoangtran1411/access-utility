# Feature 09 - Zero-Copy MemoryMappedFile & Streaming Engine

## 📌 Overview
Currently, `Jet3BinaryReader` and `Jet3Repairer` read full `.mdb` files into heap memory (`byte[] fileBytes = File.ReadAllBytes(path)`). For large legacy databases nearing 1GB–2GB, this creates massive Large Object Heap (LOH) pressure and garbage collection overhead. 

This feature refactors the binary parsing engine to leverage `MemoryMappedFile` and `ReadOnlySpan<byte>` for zero-allocation page access, along with `IAsyncEnumerable<AccessRow>` for streaming table exports.

---

## 📐 Technical Specification

### 1. Memory-Mapped Access (`Engine/Jet3MemoryReader.cs`)
- Wrap `MemoryMappedFile` and `MemoryMappedViewAccessor` in an `IDisposable` container.
- Implement `ReadOnlySpan<byte> GetPage(int pageIndex)` using unsafe pointer views to avoid any byte array allocations.
- Validate file boundaries, page alignment (2048 bytes), and handle read-only file access cleanly.

### 2. Streaming Row Parser (`Engine/Jet3BinaryReader.cs`)
- Refactor `ReadTableRows` to support `IAsyncEnumerable<AccessRow> StreamRowsAsync(string tableName)`.
- Stream individual rows directly to disk/exporters without buffering millions of rows in memory.

### 3. Exporter Streaming Integration
- Update `CsvExporter`, `SqliteExporter`, and `SqlScriptExporter` to consume streaming record enumerators.

---

## 🎯 User Interface Integration

### CLI Behavior
- Seamlessly activated for all commands (`diagnose`, `compact`, `repair`, `export`).
- Add memory usage metrics to `--verbose` diagnostic output:
```bash
AccessUtility.exe diagnose database.mdb --verbose
```
