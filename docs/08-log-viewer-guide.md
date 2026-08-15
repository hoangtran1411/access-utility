# 08 - SQLite Log Viewer Guide

To make troubleshooting easier and avoid relying on external GUI database browsers, AccessUtility includes a fully integrated native Log Viewer.

## 📁 Where are the logs?
Whenever the AccessUtility `daemon` runs or background jobs execute, they log telemetric data to a local, self-contained SQLite database named **`app_logs.sqlite`**. 

This file is automatically created and migrated (initialized with the proper table schemas) the very first time the application runs.

## 🔍 How to view logs
You can instantly retrieve and tail the logs directly in your command line:

```bash
AccessUtility.exe logs
```
*(By default, this shows the most recent 50 logs).*

### 1. Tail specific line count
If you only want to see the last 10 log entries:
```bash
AccessUtility.exe logs --tail 10
```

### 2. Filter by Level
If you are looking for specific issues, you can filter the logs down to only `error`, `warning`, or `info` events:
```bash
AccessUtility.exe logs --level error
```

### 3. Change Log Database Path
If your logs are stored in a different file, you can pass a custom `--db` path:
```bash
AccessUtility.exe logs --db ./my_custom_logs.sqlite
```

## 🎨 Color-coded Output
The integrated log viewer will format the output with ANSI color codes to make reading logs much easier:
* **Errors & Fatal Issues**: 🔴 Red
* **Warnings**: 🟡 Yellow
* **Information**: 🟢 Green

---

## ⏩ Navigation
- ⬅️ **Previous:** [07 - Serilog Configuration & Telemetry](07-serilog-configuration.md)
- ➡️ **Next:** [09 - Recommendations & Future Roadmap](09-recommendations-and-future-roadmap.md)
- 🔄 **Return to Start:** [00 - Beginner's Guide to AccessUtility](00-beginner-guide.md)
