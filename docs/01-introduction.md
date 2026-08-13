# 01 - Introduction to AccessUtility

## What is AccessUtility?

AccessUtility is a modernization toolkit for legacy Microsoft Access 97 (`.mdb`) databases, leveraging the extreme performance and memory safety of .NET 10 Native AOT. 

It completely bypasses COM drivers, ODBC, OLEDB, and the traditional Access Engine, replacing them with a custom-built, purely managed C# parser capable of reading Jet 3.5 binaries byte-by-byte.

## Why Native AOT?

Native AOT compilation allows AccessUtility to:
1. **Start instantly**: No JIT compilation time.
2. **Deploy anywhere**: It compiles down to a single standalone binary with zero runtime dependencies.
3. **Save memory**: Minimal footprint compared to traditional .NET runtimes.

## Core Capabilities

- **Jet Binary Parsing**: Directly read `2048-byte` pages, reconstruct tables, and extract records.
- **Diagnostics**: Analyze corrupted databases and clean orphan `.ldb` lock files.
- **Export**: Seamlessly extract schema definitions, data, and embedded OLE objects.
- **Automation**: Designed to be integrated into CI/CD pipelines via Cobra-style CLI commands.
