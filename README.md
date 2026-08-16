# AccessUtility 97 (.NET 10 Native AOT)

[![CI/CD Pipeline](https://github.com/hoangtran1411/access-utility/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/hoangtran1411/access-utility/actions/workflows/ci-cd.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10.0%20Native%20AOT-purple.svg)](https://dotnet.microsoft.com/)
[![Tests](https://img.shields.io/badge/Tests-80%20Passed-brightgreen.svg)](AccessUtility.Tests/)

**AccessUtility** is a high-performance, standalone diagnostics, repair, forensic recovery, and migration suite built in **.NET 10 (Native AOT)** designed specifically for legacy **Microsoft Access 97 (`.mdb` / Jet 3.5)** databases.

Built completely independent of legacy COM drivers, Access runtime components, or `System.Data.OleDb`, AccessUtility parses, analyzes, and reconstructs 2048-byte Jet 3.5 binary pages directly with zero-allocation memory-mapped streaming.

---

## ✨ Key Features & Capabilities

- ⚡ **Native AOT Performance**: Instantaneous startup time (<10ms), minimal RAM footprint, and zero runtime dependencies.
- 🗄️ **Multi-Dialect SQL Migration Exporter**: Translates legacy Jet 3.5 tables, primary keys, non-cyclic foreign keys, and saved query views into optimized DDL and batched DML scripts for:
  - **PostgreSQL** (`SERIAL`, `BYTEA`, `TIMESTAMP`, `DOUBLE PRECISION`)
  - **MySQL / MariaDB** (`AUTO_INCREMENT`, `TINYINT(1)`, `LONGTEXT`, `ENGINE=InnoDB`)
  - **Microsoft SQL Server (T-SQL)** (`IDENTITY(1,1)`, `BIT`, `DATETIME2`, `NVARCHAR(MAX)`)
  - **SQLite** (`INTEGER PRIMARY KEY AUTOINCREMENT`, `TEXT`, `BLOB`)
  - **Oracle** (`GENERATED ALWAYS AS IDENTITY`, `VARCHAR2`, `CLOB`, `BLOB`)
  - **ANSI SQL** (Universal portable DDL)
- 📊 **Modern Analytical Exporters**: Direct physical export to **Apache Parquet** (with Snappy compression), **DuckDB** analytics database, and streaming **JSON Lines** (`.jsonl`).
- 🔬 **Forensic Record Carver**: Scans unallocated slack space and deleted slot directories to salvage dropped or deleted records with confidence scoring (High/Medium/Low).
- 🗺️ **Interactive Physical Sector Map & Hex Inspector**: Classifies every 2KB page (`Header`, `PAM`, `TDEF`, `Data`, `Index`, `Slack`, `Corrupt`) and displays formatted 16-byte hexadecimal and ASCII character views.
- 📈 **Mermaid ERD Visualizer**: Generates entity-relationship diagrams and Markdown documentation directly from database schemas.
- 🛡️ **Deep Compact & Sector Repair**: Purges slack space, isolates corrupt byte ranges, rebuilds page structures, and cleans up stale orphan `.ldb` lock files.
- 🔑 **Password & Security Decryptor**: Decrypts Jet 3.5 database passwords and parses `System.mdw` Workgroup information files.
- 🖼️ **Media & Query Extractors**: Extracts embedded OLE blobs (BMP, JPG, PDF, Word) and parses `MSysQueries` into standalone `.sql` files.
- 🤖 **AX AI Assistant & Web UI**: Interactive natural language CLI assistant and single-page embedded web dashboard.

---

## 🚀 Quick Start

Ensure you have the [.NET 10 SDK](https://dotnet.microsoft.com/) installed.

```bash
# Clone the repository
git clone https://github.com/hoangtran1411/access-utility.git
cd access-utility

# Build and run all 80+ unit tests
dotnet test

# Publish Native AOT single-file binary
dotnet publish -c Release
```

---

## 💻 Command Reference

| Command | Description | Example |
| :--- | :--- | :--- |
| `diagnose` | Run health check, PAM fragmentation, and optional forensic scan | `AccessUtility.exe diagnose Main.mdb --forensic-scan` |
| `compact` | Defragment database, rebuild pages, and shrink file size | `AccessUtility.exe compact Main.mdb --output Clean.mdb` |
| `repair` | Deep sector recovery with optional deleted record carving | `AccessUtility.exe repair Damaged.mdb --carve-deleted` |
| `carve` | Standalone forensic carver for slack space and deleted rows | `AccessUtility.exe carve Damaged.mdb --output ./salvaged.sqlite` |
| `export` | Export to Parquet, DuckDB, JSONL, Multi-Dialect SQL, SQLite, or CSV | `AccessUtility.exe export Main.mdb --format sql --dialect postgres` |
| `schema` | Export clean DDL schema script with primary and foreign keys | `AccessUtility.exe schema Main.mdb --dialect mysql --output ./schema.sql` |
| `erd` | Generate Mermaid entity-relationship diagram and markdown | `AccessUtility.exe erd Main.mdb --output ./schema.md` |
| `hex` | Inspect low-level 2048-byte page hexadecimal and ASCII dump | `AccessUtility.exe hex Main.mdb --page 0` |
| `password` | Decrypt database password and inspect `.mdw` workgroups | `AccessUtility.exe password Protected.mdb` |
| `extract-ole` | Extract embedded OLE media (BMP, JPG, PDF, DOC) to disk | `AccessUtility.exe extract-ole Products.mdb --output ./extracted/` |
| `extract-queries` | Reconstruct saved Access queries to `.sql` files | `AccessUtility.exe extract-queries Sales.mdb --output ./queries/` |
| `lockstat` | Inspect connected users and clean up orphan `.ldb` lock files | `AccessUtility.exe lockstat Shared.mdb --clean` |
| `diff` | Compare two database schemas and generate migration scripts | `AccessUtility.exe diff Dev.mdb Prod.mdb --dialect postgres` |
| `daemon` | Run background automated compaction and rolling backups | `AccessUtility.exe daemon Main.mdb --interval 24h` |
| `logs` | View and filter persistent SQLite audit logs | `AccessUtility.exe logs --tail 50 --level Warning` |
| `web` | Start embedded web dashboard with interactive visualizer | `AccessUtility.exe web --port 5000` |
| `ax` | Ask AX AI Assistant to execute maintenance tasks | `AccessUtility.exe ax "Export schema to PostgreSQL SQL"` |

---

## 📖 Documentation Index

For deep architectural dives and developer guides, explore the `docs/` directory:

- [00 - Beginner's Guide to AccessUtility](docs/00-beginner-guide.md)
- [01 - Introduction & Architecture](docs/01-introduction-and-architecture.md)
- [02 - Lock File (.ldb) Inspector Guide](docs/02-lock-file-inspector-guide.md)
- [03 - Compact & Repair Engine](docs/03-compact-and-repair-engine.md)
- [04 - CLI & Web UI Guide](docs/04-cli-and-web-ui-guide.md)
- [05 - Building, Testing & CI/CD](docs/05-building-testing-and-cicd.md)
- [06 - Complete CLI Usage Commands](docs/06-cli-usage.md)
- [07 - Serilog Configuration & Telemetry](docs/07-serilog-configuration.md)
- [08 - SQLite Log Viewer Guide](docs/08-log-viewer-guide.md)
- [09 - Recommendations & Future Roadmap](docs/09-recommendations-and-future-roadmap.md)
- [10 - Phase 1: Memory & Streaming Optimizations](docs/10-phase1-memory-and-streaming.md)
- [11 - Phase 2: Modern Analytical Exporters (Parquet, DuckDB, JSONL)](docs/11-phase2-modern-exporters.md)
- [12 - Phase 3: Web Dashboard Visualizer & ERD Diagrams](docs/12-phase3-web-visualizer-and-erd.md)
- [13 - Phase 4: Forensic Record Carving & Deleted Data Recovery](docs/13-phase4-forensic-carving.md)
- [14 - Multi-Dialect SQL Schema & Migration Guide](docs/14-sql-migration-and-schema-guide.md)

---

## 🤝 Contributing
Contributions are welcome! Please check out [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## 📝 License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
