# 04 - CLI & Web UI Guide

**AccessUtility** provides two intuitive interfaces: a high-efficiency Command-Line Interface (CLI) for terminal automation and an embedded Web UI Dashboard for browser interaction.

---

## 💻 Command-Line Interface (CLI) & Interactive TUI

Running `AccessUtility.exe` without parameters (or with `tui`) launches the interactive terminal menu (`TuiEngine.cs`).

Running `AccessUtility.exe --help` displays all registered Cobra commands:

```text
========================================================================
 AccessUtility (.NET 10 Native AOT) - Cobra CLI System
 Focus: Access 97 Compact, Repair, Lock Inspector & AX Assistant
========================================================================

Usage:
  AccessUtility.exe [command] [flags]

Available Commands:
  lockstat     Inspect .ldb lock file & list connected users (aliases: ls, locks)
  diagnose     Run database health & page fragmentation diagnostics (aliases: diag, health)
  compact      Defragment & minimize .mdb file size (aliases: cmp, defrag)
  repair       Deep sector repair & data recovery (aliases: rep, recover)
  export       Export database to SQLite, SQL scripts, or CSV (aliases: exp, convert)
  password     Decrypt database password & inspect security settings (aliases: pw, security)
  diff         Compare two databases & generate SQL migration script (aliases: compare)
  extract-ole  Extract OLE embedded objects (BMP, PDF, Word) (aliases: extract)
  extract-queries Extract saved queries to SQL files (aliases: queries)
  daemon       Run automated background maintenance (aliases: maintain)
  logs         View and filter the local SQLite application logs (aliases: log, tail)
  update       Update AccessUtility to the latest release
  ax           AX (AI Experiment) Natural Language Command Assistant (aliases: ai, ask)
  web          Launch Web Dashboard UI in browser (aliases: ui, dashboard)
```

---

## 🌐 Embedded Web Dashboard (`Web/WebServer.cs`)

Launch the web dashboard on port 5000 (or custom port via `--port`):

```bash
AccessUtility.exe web --port 5000
# Or using aliases:
AccessUtility.exe dashboard --port 5000
```

Open `http://localhost:5000` in your web browser:

### Dashboard Features:
- **Lock File Warning Badge**: Real-time indication of `.ldb` presence, active users, and orphan lock cleanup button.
- **Health Metrics**: Page count, file size, fragmentation %, and corrupt page counter.
- **One-Click Operations**: Instant buttons for Compact, Repair, Export to SQLite, and Export SQL Script.
- **Execution Log**: Real-time console-style activity feed displaying step-by-step progress.

---

## ⏩ Navigation
- ⬅️ **Previous:** [03 - Compact & Repair Engine Guide](03-compact-and-repair-engine.md)
- ➡️ **Next:** [05 - Building, Testing & CI/CD](05-building-testing-and-cicd.md)
