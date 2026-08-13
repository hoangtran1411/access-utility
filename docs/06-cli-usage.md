# 06 - Complete CLI Usage Commands

AccessUtility features a Cobra-style command-line interface with full support for flags, arguments, command aliases, natural language AI assist, interactive TUI, and embedded Web Dashboard.

## General Usage

```bash
AccessUtility.exe [command] [arguments] [flags]
```

---

## 📋 Available Commands & Reference

### 1. `lockstat` (Aliases: `ls`, `locks`)
Inspect the `.ldb` lock file to identify active workstation logins, usernames, and detect orphan locks.
```bash
AccessUtility.exe lockstat legacy.mdb
AccessUtility.exe ls legacy.mdb
```

### 2. `diagnose` (Aliases: `diag`, `health`)
Scan Jet 3.5 database page allocation map (PAM), TDEF tables, data pages, fragmentation %, and corrupted page counts.
```bash
AccessUtility.exe diagnose legacy.mdb
AccessUtility.exe health legacy.mdb
```

### 3. `compact` (Aliases: `cmp`, `defrag`)
Purge deleted record slack space, defragment pages, and rebuild continuous page allocations.
```bash
AccessUtility.exe compact legacy.mdb --output clean.mdb
AccessUtility.exe cmp legacy.mdb --force-unlock
```

### 4. `repair` (Aliases: `rep`, `recover`)
Deep sector scan to recover orphaned TDEFs, salvage active record rows, and reconstruct healthy `.mdb` database.
```bash
AccessUtility.exe repair corrupted.mdb --output fixed.mdb --force-unlock
AccessUtility.exe rep corrupted.mdb
```

### 5. `export` (Aliases: `exp`, `convert`)
Export database tables and schemas to SQLite, SQL scripts, or CSV files.
```bash
AccessUtility.exe export legacy.mdb --format sqlite
AccessUtility.exe export legacy.mdb --format sql
AccessUtility.exe export legacy.mdb --format csv
```

### 6. `password` (Aliases: `pw`, `security`)
Decrypt Page 0 14-byte XOR database password, inspect User-Level Security (ULS) flags, encryption at rest, owner SID, and optionally parse `System.mdw` workgroups.
```bash
AccessUtility.exe password legacy.mdb
AccessUtility.exe pw legacy.mdb --workgroup System.mdw
```

### 7. `diff` (Aliases: `compare`)
Compare two Access 97 schemas (source vs target) and generate ANSI SQL, SQLite, PostgreSQL, or SQL Server DDL migration scripts.
```bash
AccessUtility.exe diff dev.mdb prod.mdb --dialect pgsql --output migration.sql
AccessUtility.exe compare dev.mdb prod.mdb --dialect sqlite
```

### 8. `extract-ole` (Aliases: `extract`)
Scan Long Binary (OLE) fields, strip Access OLE container headers (78 bytes), and extract media/documents (BMP, JPG, PNG, PDF, Word) to disk.
```bash
AccessUtility.exe extract-ole legacy.mdb --output ./extracted_files
```

### 9. `extract-queries` (Aliases: `queries`)
Parse `MSysQueries` and `MSysObjects` system tables to reconstruct saved queries into `.sql` files.
```bash
AccessUtility.exe extract-queries legacy.mdb --output ./extracted_queries
```

### 10. `daemon` (Aliases: `maintain`)
Run continuous automated background maintenance (clean orphan locks, auto compact if fragmentation > 15%, ZIP backups).
```bash
AccessUtility.exe daemon legacy.mdb --interval 12h --backup-dir ./backups
```

### 11. `logs` (Aliases: `log`, `tail`)
View and filter color-coded logs from the local SQLite log database.
```bash
AccessUtility.exe logs --tail 20 --level error
AccessUtility.exe logs --db custom_logs.sqlite
```

### 12. `update`
Query GitHub releases for the latest version and update the running binary automatically.
```bash
AccessUtility.exe update
```

### 13. `ax` (Aliases: `ai`, `ask`)
AX (AI Assistant) interprets natural language prompts and auto-executes database operations.
```bash
AccessUtility.exe ax "Clean orphan locks and compact C:\Data\legacy.mdb"
```

### 14. `tui`
Launch interactive Terminal UI (launched by default when running without command-line arguments).
```bash
AccessUtility.exe tui
```

### 15. `web` (Aliases: `ui`, `dashboard`)
Launch embedded ASP.NET Core Native AOT web dashboard on specified port (default `5000`).
```bash
AccessUtility.exe web --port 5000
AccessUtility.exe dashboard --port 8080
```

---

## ⏩ Navigation
- ⬅️ **Previous:** [05 - Building, Testing & CI/CD Guide](05-building-testing-and-cicd.md)
- ➡️ **Next:** [07 - Serilog Configuration & Telemetry](07-serilog-configuration.md)
