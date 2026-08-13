# AccessUtility 97 (.NET 10 Native AOT)

AccessUtility is a high-performance command-line utility built in **.NET 10 (Native AOT)** designed specifically for managing, repairing, migrating, and diagnosing legacy **Microsoft Access 97 (`.mdb` / Jet 3.5)** databases.

Built without reliance on old COM drivers or `System.Data.OleDb`, AccessUtility parses Jet 3.5 binaries natively at blinding speed, making it the perfect CI/CD tool for modernizing legacy infrastructures.

## ✨ Key Features

- **Blazing Fast**: Native AOT compilation means instantaneous startup and zero JIT overhead. No runtime dependencies needed.
- **Cross-Platform Potential**: Parses Jet 3.5 binaries purely in C# memory. 
- **Database Repair & Diagnostics**: Scans for corrupted 2048-byte pages, orphan `.ldb` lock files, and provides robust reconstruction tools.
- **Data Export & Migration**: Diff schemas and export cleanly to CSV, SQLite, PostgreSQL, SQL Server, or ANSI SQL.
- **Password & Security Inspection**: Decrypt Jet 3.5 database passwords and inspect `System.mdw` Workgroup files.
- **OLE Blob Extraction**: Rip embedded media (BMP, JPG, PDF, Word) out of `Long Binary` OLE database fields.
- **AX AI Assistant**: Chat with the tool using the built-in AX Assistant to auto-execute commands.
- **Serilog Native Logging**: Built-in, reflection-free logging to an `access_utility_logs.sqlite` file.

## 🚀 Quick Start

Ensure you have the .NET 10 SDK installed.

```bash
# Clone the repository
git clone https://github.com/hoangtran1411/access-utility.git
cd access-utility

# Build Native AOT
dotnet publish -c Release

# Run the CLI
./bin/Release/net10.0/win-x64/publish/AccessUtility.exe --help
```

## 📖 Documentation

Check out the `docs/` folder for comprehensive guides:
1. [Introduction to AccessUtility](docs/01-introduction.md)
2. [CLI Usage & Commands](docs/02-cli-usage.md)
3. [Serilog Configuration](docs/03-serilog-configuration.md)

## 🤝 Contributing
See [CONTRIBUTING.md](CONTRIBUTING.md) for details on how you can help.

## 📝 License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
