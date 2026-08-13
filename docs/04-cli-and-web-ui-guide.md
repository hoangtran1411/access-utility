# 04 - CLI & Web UI Guide

**AccessUtility** provides two intuitive interfaces: a high-efficiency Command-Line Interface (CLI) for terminal automation and an embedded Web UI Dashboard for browser interaction.

---

## 💻 Command-Line Interface (CLI)

Running `AccessUtility.exe` without parameters launches an interactive terminal menu:

```text
========================================================================
 MS Access 97 (.mdb) Utility - Native AOT Engine
 Focus: Compact, Repair & Lock File (.ldb) Inspector
========================================================================

Usage:
  AccessUtility.exe <command> <file.mdb> [options]

Commands:
  lockstat <file.mdb>                            Inspect .ldb lock file & active user connections
  diagnose <file.mdb>                            Run health & page fragmentation diagnostics
  compact  <file.mdb> [--output target] [--force-unlock]  Defragment & minimize .mdb file size
  repair   <file.mdb> [--output target] [--force-unlock]  Deep sector repair & data recovery
  export   <file.mdb> [--format sqlite|sql|csv]  Export database to SQLite, SQL, or CSV
  web      [--port 5000]                         Launch Web Dashboard UI in browser
```

---

## 🌐 Embedded Web Dashboard (`Web/WebServer.cs`)

Launch the web dashboard on port 5000 (or custom port):

```bash
AccessUtility.exe web --port 5000
```

Open `http://localhost:5000` in your web browser:

### Dashboard Features:
- **Lock File Warning Badge**: Real-time indication of `.ldb` presence, active users, and orphan lock cleanup button.
- **Health Metrics**: Page count, file size, fragmentation %, and corrupt page counter.
- **One-Click Operations**: Instant buttons for Compact, Repair, Export to SQLite, and Export SQL Script.
- **Execution Log**: Real-time console-style activity feed displaying step-by-step progress.

---

## Next Step
Continue to [05 - Building, Testing & CI/CD](05-building-testing-and-cicd.md) to learn how to build, test, and automate releases using GitHub Actions.
