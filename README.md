# AccessUtility 🛠️ (.NET 10 Native AOT)

> **Microsoft Access 97 (`.mdb` / Jet 3.5) Database Repair, Compactor & Lock File (`.ldb`) Utility**

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Native AOT](https://img.shields.io/badge/Native-AOT-brightgreen)](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![CI/CD Pipeline](https://img.shields.io/badge/CI%2FCD-GitHub--Actions-blue?logo=github-actions)](.github/workflows/ci-cd.yml)

---

## 💡 Overview

**AccessUtility** is a high-performance, zero-dependency standalone native tool written in C# targeting **.NET 10 Native AOT**. It is specifically engineered to read, **Compact**, **Repair**, and export legacy **Microsoft Access 97 (`.mdb`)** database files directly on 64-bit operating systems without requiring Microsoft Access or legacy 32-bit Jet/ACE OLEDB drivers.

It includes full **Lock File (`.ldb`) Inspection** to identify connected computer and user names, detect active vs orphan lock states, and clean up stale locks safely.

---

## ✨ Key Features

- ⚡ **Pure C# Jet 3.5 Engine**: Direct binary byte parsing of Access 97 2048-byte page structures (Header, Table Definitions TDEF, Data pages, Record slot pointers, Null masks, ANSI strings, OLE dates).
- 🧹 **Database Compactor**: Defragments `.mdb` files, purges deleted record slack space, rebuilds B-tree indexes, and minimizes file size.
- 🚑 **Deep Repair & Recovery Engine**: Page sector scanner that isolates corrupted pages, salvages orphan tables and rows, rebuilds catalog structures, and restores database integrity.
- 🔒 **Lock File (`.ldb`) Inspector**: Decodes 64-byte user locking blocks to display connected **Computer Names** and **User Names**. Safely removes orphan locks.
- 🔄 **Multi-Format Exporters**: Converts Access 97 tables directly to **SQLite (`.sqlite`)**, **SQL DDL/DML scripts (`.sql`)**, **CSV (`.csv`)**, and **JSON (`.json`)**.
- 🖥️ **Dual Interface**:
  - **Terminal CLI**: Interactive menu + automated command flags (`lockstat`, `diagnose`, `compact`, `repair`, `export`, `web`).
  - **Embedded Web Dashboard**: Modern single-page web UI served via embedded ASP.NET Core Native AOT Web API.

---

## 📚 Documentation & Feature Roadmap

- 📖 [**Step-by-Step Learning Series (`docs/`)**](docs/01-introduction-and-architecture.md): 5-part tutorial covering Jet 3.5 2KB page engine, `.ldb` lock parsing, compacting, repair algorithms, and CI/CD.
- 🎯 [**Feature Specifications & Progress Tracker (`context/`)**](context/progress.md):
  - [`01-feature-password-decryptor.md`](context/01-feature-password-decryptor.md): Database password extraction & security inspector.
  - [`02-feature-schema-diff-migration.md`](context/02-feature-schema-diff-migration.md): Schema version comparison & SQL migration generator.
  - [`03-feature-ole-object-extractor.md`](context/03-feature-ole-object-extractor.md): Embedded image & document OLE blob extractor.
  - [`04-feature-query-sql-extractor.md`](context/04-feature-query-sql-extractor.md): Query SQL reconstructor from `MSysQueries`.
  - [`05-feature-maintenance-daemon.md`](context/05-feature-maintenance-daemon.md): Scheduled health monitoring, orphan lock cleanup & zip backups.
  - [`progress.md`](context/progress.md): Live task checklist and implementation tracker.

---

## 🚀 Quick Start

### 1. Build & Run
Ensure [.NET 10 SDK](https://dotnet.microsoft.com/download) is installed:

```bash
# Clone the repository
git clone https://github.com/hoangtran1411/access-utility.git
cd access-utility

# Build using modern XML solution file (.slnx)
dotnet build AccessUtility.slnx -c Release

# Run xUnit unit tests
dotnet test AccessUtility.slnx
```

### 2. Publish Standalone Native AOT Executable
```bash
dotnet publish AccessUtility.csproj -c Release -r win-x64 -p:PublishAot=true
```
The compiled self-contained binary `AccessUtility.exe` will be located in `bin/Release/net10.0/win-x64/publish/`.

---

## 💻 Command Line Usage

```bash
# Inspect lock file (.ldb) & list connected users
AccessUtility.exe lockstat C:\Databases\Northwind97.mdb

# Run database health diagnostics
AccessUtility.exe diagnose C:\Databases\Northwind97.mdb

# Compact database (defragment & reduce size)
AccessUtility.exe compact C:\Databases\Northwind97.mdb --output Northwind_Clean.mdb

# Repair corrupted database
AccessUtility.exe repair C:\Databases\Corrupted97.mdb --output Northwind_Repaired.mdb --force-unlock

# Export database to SQLite
AccessUtility.exe export C:\Databases\Northwind97.mdb --format sqlite

# Launch embedded Web UI Dashboard in browser
AccessUtility.exe web --port 5000
```

---

## 🤝 Contributing

We welcome community contributions! Please read our [**CONTRIBUTING.md**](CONTRIBUTING.md) for development setup, coding standards, and pull request guidelines.

---

## 📜 License

This project is licensed under the **MIT License**. See the [**LICENSE**](LICENSE) file for details.
