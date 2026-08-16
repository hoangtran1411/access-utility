# Agent Guidelines & Engineering Standards for AccessUtility

Welcome to the **AccessUtility** codebase. When contributing, refactoring, or generating code for this repository, you must adhere to the following technical principles and architectural standards.

---

## 🏛️ 1. Native AOT & .NET 10 Requirements

- **Zero Reflection & Dynamic Code Generation**: This project compiles with `<PublishAot>true</PublishAot>`. Do not use `Type.GetType()`, runtime emit, `Activator.CreateInstance()`, or unannotated reflection.
- **System.Text.Json Source Generation**:
  - Any model serialized/deserialized in the Web API or Exporters must be registered with `[JsonSerializable(typeof(...))]` on [`Web/AppJsonContext.cs`](file:///E:/Workspace/Dev/Tool/access-utility/Web/AppJsonContext.cs).
  - For streaming JSON export, prefer [`Utf8JsonWriter`](https://learn.microsoft.com/dotnet/api/system.text.json.utf8jsonwriter) for zero-allocation and optimal throughput.
- **Dependency Minimization**: Keep external NuGet package dependencies minimal and verify that all packages explicitly support Native AOT trimming (`IsTrimmable=true`).

---

## 💾 2. Jet 3.5 Binary Engine Specifications

- **Page Sizing**: All Jet 3.5 (Access 97) pages are strictly **2048 bytes (2KB)**.
- **Physical Signatures**:
  - Page 0: Database header with magic ASCII string `Standard Jet DB\0` starting at byte offset `0x04` and engine version `0x01` at `0x14`.
  - Password Block: 14-byte XOR-encrypted key at byte offset `0x42` on Page 0.
  - Page Type Bytes (Offset 0): `0x00` (Header / Free), `0x01` (PAM / Data), `0x02` (TDEF Table Definition), `0x03`/`0x04` (Index B-Tree).
- **Slot Directory**:
  - Data pages store row pointers in reverse order at the end of the page (`2048 - (slotIndex + 1) * 2`).
  - Bit `0x8000` indicates a deleted slot. Use `recOffset & 0x7FFF` to recover deleted row offsets for forensic carving.
- **Memory Safety**:
  - Use `MemoryMappedFile` and `ReadOnlySpan<byte>` via [`Engine/Jet3MemoryReader.cs`](file:///E:/Workspace/Dev/Tool/access-utility/Engine/Jet3MemoryReader.cs) when processing large database files to prevent Out-Of-Memory (OOM) crashes.

---

## 🗄️ 3. Multi-Dialect SQL & Exporters

- **Dialect Handling**: [`Exporters/SqlMigrationExporter.cs`](file:///E:/Workspace/Dev/Tool/access-utility/Exporters/SqlMigrationExporter.cs) supports PostgreSQL, MySQL/MariaDB, SQL Server, SQLite, Oracle, and ANSI SQL.
- **Constraint Ordering**: Always emit base `CREATE TABLE` DDL first, followed by non-cyclic `ALTER TABLE ADD CONSTRAINT ... FOREIGN KEY` statements to prevent circular dependency errors.
- **Batched Ingestion**: Emit multi-row `INSERT INTO table (cols) VALUES (...), (...)` with configurable `--batch-size` (default: 250 rows).

---

## 🧪 4. Testing & Verification Protocols

- **100% Test Passing Rate**: All changes must pass `dotnet test` with 0 failures before opening a pull request or creating a git tag.
- **Synthesized Binary Fixtures**: When testing binary edge cases, synthesize deterministic 2048-byte byte buffers with exact Jet 3.5 headers.
- **Resource Cleanup**: Always delete temporary `.mdb`, `.sqlite`, `.parquet`, `.duckdb`, `.json`, and `.sql` test artifacts in `finally` blocks.

---

## 🤖 5. Assistant & Command Registry Alignment

- Whenever adding a new CLI command or flag:
  1. Register the command descriptor, usage, flags, and examples in [`Engine/CommandRegistry.cs`](file:///E:/Workspace/Dev/Tool/access-utility/Engine/CommandRegistry.cs).
  2. Implement command routing in [`Program.cs`](file:///E:/Workspace/Dev/Tool/access-utility/Program.cs).
  3. Add natural language intent parsing in [`Engine/AxAssistant.cs`](file:///E:/Workspace/Dev/Tool/access-utility/Engine/AxAssistant.cs).
  4. Update [`README.md`](file:///E:/Workspace/Dev/Tool/access-utility/README.md) and [`context/progress.md`](file:///E:/Workspace/Dev/Tool/access-utility/context/progress.md).
