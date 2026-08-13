# Feature 08 - Log Viewer & Database Initializer

## 📌 Overview
AccessUtility uses a custom native AOT-compatible SQLite Serilog sink. To make it easy for administrators to review daemon activity, errors, and access patterns without needing a third-party SQLite browser, we need a built-in CLI log viewer. 

Additionally, we ensure the local settings/log database is migrated and initialized automatically upon the first run of the tool.

---

## 📐 Technical Specification

### 1. Database Initialization (Migration)
- The SQLite database (`app_logs.sqlite`) is initialized automatically when the `SqliteLogSink` is instantiated.
- `CREATE TABLE IF NOT EXISTS Logs` acts as the primary schema migration for the first run.

### 2. Log Viewer Engine
- Read logs from `app_logs.sqlite` using `Microsoft.Data.Sqlite`.
- Filter logs by Level (`--level error`, `--level info`) or Limit (`--tail 50`).
- Print the output to the console with ANSI color-coding based on the log level (Error = Red, Warning = Yellow, Info = Green).

---

## 🎯 User Interface Integration

### CLI Command
```bash
AccessUtility.exe logs --tail 20
AccessUtility.exe logs --level error
```
