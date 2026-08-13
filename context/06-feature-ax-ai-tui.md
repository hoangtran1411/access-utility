# Feature 06 - AX (AI Experiment) & Cobra-Style Rich TUI Engine

## 📌 Overview
Enhances **AccessUtility** with an **AX (AI Experiment)** natural language command assistant and a **Cobra-style rich Terminal User Interface (TUI)**. Users can interact with the utility via natural language commands (e.g. `"Compact my database and remove stale locks"`) or navigate a self-documenting interactive TUI with colorized status badges, table previews, and progress bars.

---

## 📐 Technical Specification

### 1. AX AI Experiment Assistant (`Engine/AxAssistant.cs`)
- Translates natural language requests into structured execution plans:
  - `"Analyze health of data97.mdb"` -> Runs `diagnose`
  - `"Clean stale lock and compact data97.mdb"` -> Runs `clean-lock` then `compact`
  - `"Convert data97.mdb to sqlite"` -> Runs `export --format sqlite`
- Provides an interactive AI REPL prompt:
  ```bash
  AccessUtility.exe ax
  ```

### 2. Cobra-Style Self-Documenting Command Hierarchy (`Engine/CommandRegistry.cs`)
- Command Descriptors (Name, Aliases, Usage, Short Description, Long Description, Flags, Examples).
- Auto-generated rich help screens and sub-command tree routing.

### 3. Rich TUI Renderer (`Engine/TuiEngine.cs`)
- ANSI colorized health indicators (`[HEALTHY]`, `[FRAGMENTED]`, `[CORRUPTED]`).
- Formatted console table rendering for `.ldb` connected users and table schemas.
- Interactive progress spinners for compacting and repairing operations.

---

## 🎯 User Interface Integration

### Natural Language AX Command
```bash
AccessUtility.exe ax "Check lock status of sample97.mdb and clean stale locks"
```

### Interactive TUI Mode
```bash
AccessUtility.exe tui
```
