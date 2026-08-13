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

## 📚 Documentation & Tutorial Series

Check out our step-by-step documentation series in the [`docs/`](docs/) folder:

1. [**01 - Introduction & Architecture**](docs/01-introduction-and-architecture.md): Overview of Jet 3.5 (Access 97) database engine layout & Native AOT design.
2. [**02 - Lock File (.ldb) Inspector Guide**](docs/02-lock-file-inspector-guide.md): Understanding `.ldb` 64-byte blocks, active locks, and stale lock removal.
3. [**03 - Compact & Repair Engine Guide**](docs/03-compact-and-repair-engine.md): In-depth breakdown of defragmentation and deep sector page recovery.
4. [**04 - CLI & Web UI Guide**](docs/04-cli-and-web-ui-guide.md): Using terminal commands and launching the embedded Web Dashboard UI.
5. [**05 - Building, Testing & CI/CD Guide**](docs/05-building-testing-and-cicd.md): Compiling Native AOT binaries, running xUnit tests, and GitHub Actions workflow setup.

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
