# 07 - Serilog Configuration & Telemetry

AccessUtility utilizes **Serilog** for robust, structured logging. Due to the strict nature of .NET 10 Native AOT (which restricts dynamic reflection), traditional Serilog SQLite sinks are unstable.

To counter this, AccessUtility uses a custom, reflection-free **SQLite Log Sink** (`Engine/SqliteLogSink.cs`) built purely on top of `Microsoft.Data.Sqlite`.

## Log Output

Logs are actively streamed to two places:
1. **Console Sink**: Printed cleanly to stdout.
2. **SQLite Sink**: Inserted into `access_utility_logs.sqlite` (or `app_logs.sqlite`) in the working directory.

## Viewing Logs

You can inspect logs using the CLI command `AccessUtility.exe logs` or open `access_utility_logs.sqlite` with any SQLite viewer (e.g., DB Browser for SQLite). 

The schema created by `SqliteLogSink.cs`:
```sql
CREATE TABLE IF NOT EXISTS Logs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Timestamp DATETIME NOT NULL,
    Level VARCHAR(15) NOT NULL,
    Message TEXT NOT NULL,
    Exception TEXT NULL
);
```

## Adding New Logs

In the codebase, use standard Serilog syntax:
```csharp
using Serilog;

// Standard info
Log.Information("Processing table {TableName}...", tableName);

// Warnings & Errors
Log.Warning("Missing lock file at {Path}", ldbPath);
Log.Error(ex, "Failed to extract OLE data from row {RowId}", rowId);
```

---

## ⏩ Navigation
- ⬅️ **Previous:** [06 - Complete CLI Usage Commands](06-cli-usage.md)
- ➡️ **Next:** [08 - SQLite Log Viewer Guide](08-log-viewer-guide.md)
