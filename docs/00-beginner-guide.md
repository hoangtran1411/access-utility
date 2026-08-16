# 00 - Beginner's Guide to AccessUtility

Welcome to **AccessUtility**! If you are new to the team or just getting started with programming, this guide is written specifically for you. We will break down exactly what this project does without the confusing jargon.

## 🕰️ The History Lesson
In the late 1990s and early 2000s, businesses loved **Microsoft Access 97** (`.mdb` files). It was an easy way to store data, like users, products, and sales. 

Fast forward to today: Most computers run 64-bit Windows, and Microsoft Office has completely dropped support for these old Access 97 databases. This means companies with old data are completely locked out of their own files! 

## 🦸‍♂️ What Does AccessUtility Do?
**AccessUtility** is a rescue tool. 

Normally, to read an Access database, you need special "drivers" installed by Microsoft Office. **AccessUtility does not need them.** Instead, we built a custom engine in C# that reads the pure, raw 1s and 0s (binary data) of the `.mdb` file directly from the hard drive. 

Here is what the tool can do:
1. **Read Data**: It reads the tables and rows from the old database.
2. **Export Data**: It can convert the old data into modern formats like SQLite, PostgreSQL, or CSV so modern apps can use it.
3. **Fix Broken Files**: Old databases corrupt easily. This tool can repair broken pages and compact the database to make it smaller.
4. **Extract Files**: Sometimes people saved images or PDFs inside the database. We can extract them!
5. **Break Passwords**: If a company forgot their password from 25 years ago, we can decrypt it.

## 🧩 How is the Code Organized?
If you want to read the code, here is where to look:

- **`Models/`**: Contains core data models and blueprints (e.g., `AccessDatabase`, `AccessTable`, `AccessColumn`, `LogEntry`, `SectorMapModels`, `ForensicModels`).
- **`Engine/`**: The core execution engine:
  - `Jet3BinaryReader.cs` & `Jet3MemoryReader.cs`: Zero-allocation memory-mapped binary parser for Jet 3.5.
  - `Jet3Compactor.cs` & `Jet3Repairer.cs`: Compacts and repairs database files.
  - `SectorMapAnalyzer.cs` & `ErdGenerator.cs`: 2KB sector page classifier and Mermaid ERD generator.
  - `ForensicCarver.cs`: Slack space deleted record salvaging and reconstruction.
  - `LdbLockInspector.cs`: Inspects `.ldb` lock files and cleans up stale orphan locks.
  - `SecurityReader.cs`: Decrypts database passwords and parses `System.mdw` workgroup files.
  - `SchemaComparer.cs`: Compares two database schemas and generates delta DDL.
  - `OleExtractor.cs` & `QueryExtractor.cs`: Extracts embedded media (OLE) and saved SQL queries.
  - `MaintenanceDaemon.cs`: Background worker for automated cleanup and backups.
  - `AxAssistant.cs`: Natural language AI assistant for automated command execution.
  - `TuiEngine.cs` & `LogViewer.cs`: Terminal UI engine and native SQLite log viewer.
- **`Exporters/`**: High-performance analytical and database export engines:
  - `SqlMigrationExporter.cs`: Multi-dialect SQL migration scripts (PostgreSQL, MySQL, SQL Server, SQLite, Oracle, ANSI).
  - `ParquetExporter.cs`: Strongly typed columnar Apache Parquet generator with Snappy compression.
  - `DuckDbExporter.cs`: Analytical DuckDB database generator with native vector appenders.
  - `JsonLinesExporter.cs`: Zero-allocation streaming line-delimited JSON (`.jsonl`).
  - `SqliteExporter.cs`, `CsvExporter.cs`, `SqlScriptExporter.cs`.
- **`Web/`**: Embedded ASP.NET Core Native AOT web server and interactive web dashboard (`WebServer.cs`, `DashboardHtml.cs`).
- **`AccessUtility.Tests/`**: Automated xUnit test suite (80+ unit tests) validating all engine components, exporters, and forensic tools.
- **`Program.cs`**: Entry point handling CLI argument parsing, interactive mode, and command dispatching.

## 🛠️ How Do I Run It?
This tool is a **Command-Line Interface (CLI)**. There are no complex setups required. You can run interactive mode by executing without arguments, or type direct commands into your terminal.

1. Open a terminal (like PowerShell or Command Prompt).
2. Type a command to run the tool. For example, to find out if a database has a password, type:
   ```bash
   AccessUtility.exe password C:\MyOldData.mdb
   ```
3. The tool will print the answer directly in your terminal!

## 🎓 Next Steps for Beginners
Don't worry if it seems overwhelming! Here is what you should do next to learn:
1. **Play with it**: Build the code in Visual Studio or Rider, and try running the `AccessUtility.exe diagnose` command on a test `.mdb` file.
2. **Read the Next Guide**: Proceed to [01 - Introduction & Architecture](01-introduction-and-architecture.md) for a deeper dive into how binary data is read.
3. **Ask Questions**: We use an AI Assistant (AX) in the CLI! You can ask the tool how to use it by running: `AccessUtility.exe ax "How do I export data?"`

---

## ⏩ Next Step
Continue to [01 - Introduction & Architecture](01-introduction-and-architecture.md) to learn how Access 97 binary files are structured and how the engine processes them.
