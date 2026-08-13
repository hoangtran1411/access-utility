# 02 - CLI Usage & Commands

AccessUtility features a Cobra-style command-line interface.

## General Usage

```bash
AccessUtility.exe [command] [arguments] [flags]
```

## Available Commands

### `export`
Export the database to various formats.
```bash
AccessUtility.exe export legacy.mdb --format csv
AccessUtility.exe export legacy.mdb --format sqlite
```

### `lockstat`
Inspect the lock file (`.ldb`) to identify active users.
```bash
AccessUtility.exe lockstat legacy.mdb --clean-orphan true
```

### `diagnose`
Check the health of the Jet 3.5 database and scan for corruption.
```bash
AccessUtility.exe diagnose legacy.mdb
```

### `compact` / `repair`
Reconstruct and compact the database, repairing orphaned pages.
```bash
AccessUtility.exe compact legacy.mdb new.mdb --force
```

### `password`
Decrypt the internal database password or inspect workgroup (`System.mdw`) files.
```bash
AccessUtility.exe password legacy.mdb
AccessUtility.exe password legacy.mdb --workgroup System.mdw
```

### `diff`
Compare two database schemas and generate SQL migration scripts.
```bash
AccessUtility.exe diff dev.mdb prod.mdb --dialect pgsql --output up.sql
```

### `extract-ole`
Extract embedded media and files (BMP, JPG, PNG, PDF, Word) from OLE blobs.
```bash
AccessUtility.exe extract-ole legacy.mdb --output ./extracted_files
```

### `ax` (AI Assistant)
Pass a natural language query to the AI to execute commands for you.
```bash
AccessUtility.exe ax "Export my users database to SQLite"
```
