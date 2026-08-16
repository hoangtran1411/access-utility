# 14 - Multi-Dialect SQL Schema & Migration Guide

This guide provides developer documentation, architectural diagrams, data type mapping specifications, and maintenance instructions for the **Multi-Dialect SQL Schema & Migration Exporter** (Feature 13).

---

## 🎯 Purpose & Migration Challenge

When modernizing legacy systems powered by Microsoft Access 97 / Jet 3.5 databases, migrating schemas and data to enterprise relational database management systems (RDBMS) is a critical requirement. 

Legacy `.mdb` files present several unique challenges during migration:
1. **Proprietary Data Types**: Jet 3.5 uses distinct representations for `AutoNumber`, `Currency`, `Memo`, and `Binary (OLE)` that do not map identically across SQL engines.
2. **Entity Constraints**: Primary keys and foreign key relationships must be declared in target-compatible DDL without creating cyclical dependency errors during table creation.
3. **Saved Query Translation**: Access queries stored in `MSysQueries` contain valuable business logic that must be translated into target-compatible `CREATE VIEW` statements.
4. **Data Ingestion Throughput**: Traditional single-row `INSERT` statements are slow for large datasets. High-speed multi-row batch inserts (`INSERT INTO ... VALUES (...), (...)`) drastically reduce migration runtimes.

---

## 🏗️ Architecture Overview

```
+─────────────────────────────────────────────────────────────────────────────+
|                             AccessDatabase (.mdb)                           |
+──────────────────────────────────────┬──────────────────────────────────────+
                                       │
                                       ▼
+─────────────────────────────────────────────────────────────────────────────+
|                    SqlMigrationExporter.ExportDatabase()                    |
+─────────────────────────────────────────────────────────────────────────────+
        │                              │                             │
        ▼                              ▼                             ▼
┌─────────────────────────┐  ┌─────────────────────────┐  ┌─────────────────────────┐
│ 1. DDL Schema Generator │  │ 2. FK & View Generator  │  │ 3. Batch DML Exporter   │
│ - Table definitions     │  │ - Primary Keys          │  │ - Multi-row batching    │
│ - Target type mappings  │  │ - ErdGenerator FKs      │  │ - Hex/Base64 binary     │
│ - Drop table handling   │  │ - QueryExtractor Views  │  │ - Transaction wrappers  │
└─────────────────────────┘  └─────────────────────────┘  └─────────────────────────┘
        │                              │                             │
        └──────────────────────────────┴─────────────────────────────┘
                                       │
                                       ▼
+─────────────────────────────────────────────────────────────────────────────+
|                         Target-Optimized SQL Script                         |
|   (PostgreSQL | MySQL/MariaDB | SQL Server | SQLite | Oracle | ANSI SQL)    |
+─────────────────────────────────────────────────────────────────────────────+
```

---

## 📊 Comprehensive Data Type Mapping Reference

The table below summarizes how Jet 3.5 column types are mapped to each target dialect by [`Exporters/SqlMigrationExporter.cs`](file:///E:/Workspace/Dev/Tool/access-utility/Exporters/SqlMigrationExporter.cs):

| Jet 3.5 Type | PostgreSQL | MySQL / MariaDB | SQL Server (T-SQL) | SQLite | Oracle | ANSI SQL |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **`Autonumber` (PK)** | `SERIAL` | `INT AUTO_INCREMENT` | `INT IDENTITY(1,1)` | `INTEGER PRIMARY KEY AUTOINCREMENT` | `NUMBER(10) GENERATED ALWAYS AS IDENTITY` | `INT PRIMARY KEY` |
| **`Boolean`** | `BOOLEAN` | `TINYINT(1)` | `BIT` | `INTEGER` | `NUMBER(1)` | `BOOLEAN` |
| **`Byte`** | `SMALLINT` | `TINYINT UNSIGNED` | `TINYINT` | `INTEGER` | `NUMBER(3)` | `SMALLINT` |
| **`Integer` (16-bit)** | `SMALLINT` | `SMALLINT` | `SMALLINT` | `INTEGER` | `NUMBER(5)` | `SMALLINT` |
| **`LongInteger` (32-bit)** | `INTEGER` | `INT` | `INT` | `INTEGER` | `NUMBER(10)` | `INT` |
| **`Single` (32-bit Float)** | `REAL` | `FLOAT` | `REAL` | `REAL` | `FLOAT(24)` | `FLOAT` |
| **`Double` (64-bit Float)** | `DOUBLE PRECISION` | `DOUBLE` | `FLOAT(53)` | `REAL` | `FLOAT(53)` | `DOUBLE PRECISION` |
| **`Currency`** | `NUMERIC(19,4)` | `DECIMAL(19,4)` | `MONEY` | `NUMERIC` | `NUMBER(19,4)` | `DECIMAL(19,4)` |
| **`DateTime`** | `TIMESTAMP` | `DATETIME(6)` | `DATETIME2` | `TEXT` (ISO 8601) | `TIMESTAMP` | `DATETIME` |
| **`Text` (Length $N$)** | `VARCHAR(N)` | `VARCHAR(N)` | `NVARCHAR(N)` | `TEXT` | `VARCHAR2(N)` | `VARCHAR(N)` |
| **`Memo` (Long Text)** | `TEXT` | `LONGTEXT` | `NVARCHAR(MAX)` | `TEXT` | `CLOB` | `TEXT` |
| **`Binary` / `OLE`** | `BYTEA` | `LONGBLOB` | `VARBINARY(MAX)` | `BLOB` | `BLOB` | `BLOB` |

---

## 💻 Developer Guide & Usage

### 1. Programmatic API Usage (`C#`)

```csharp
using AccessUtility.Engine;
using AccessUtility.Exporters;

// 1. Read the database
var db = Jet3BinaryReader.ReadDatabase("Northwind97.mdb", out _);

// 2. Configure migration options
var options = new SqlMigrationOptions
{
    Dialect = SqlDialect.PostgreSql,
    SchemaOnly = false,
    IncludeForeignKeys = true,
    IncludeViews = true,
    UseTransactions = true,
    BatchSize = 500
};

// 3. Export migration script
string sqlPath = SqlMigrationExporter.ExportDatabase(db, "./northwind_pg.sql", options);
Console.WriteLine($"SQL Migration generated at: {sqlPath}");
```

### 2. Command-Line Interface (CLI)

```bash
# Export full database (schema + data) to PostgreSQL
AccessUtility.exe export Northwind97.mdb --format sql --dialect postgres --output ./pg_northwind.sql

# Export DDL schema only (no data) for MySQL
AccessUtility.exe schema Northwind97.mdb --dialect mysql --output ./mysql_schema.sql

# Export data only with custom batch size for Microsoft SQL Server
AccessUtility.exe export Northwind97.mdb --format sql --dialect mssql --data-only --batch-size 1000 --output ./mssql_data.sql

# Ask AX AI Assistant to generate migration script
AccessUtility.exe ax "Export Northwind97.mdb schema and data to PostgreSQL SQL script"
```

### 3. REST API Endpoint

```http
GET /api/export?path=Northwind97.mdb&format=sql&dialect=postgres&schemaOnly=true HTTP/1.1
Host: localhost:5000
```

---

## 🛡️ Migration Best Practices

1. **Order of Execution**:
   - Table DDL is emitted first without foreign keys.
   - Foreign key constraints are added using `ALTER TABLE ADD CONSTRAINT` after all tables exist to prevent circular dependency errors.
   - Views (`CREATE VIEW`) are created after all base tables and constraints exist.
2. **Identity & AutoNumber Handling**:
   - For Microsoft SQL Server, `SET IDENTITY_INSERT [table] ON` is emitted before inserting explicit primary key values, and `SET IDENTITY_INSERT [table] OFF` is emitted immediately after.
3. **Character Encoding**:
   - All output scripts are written in `UTF-8` encoding.
   - PostgreSQL scripts include `SET client_encoding = 'UTF8';`.
   - MySQL scripts include `SET NAMES utf8mb4;` and use `ENGINE=InnoDB DEFAULT CHARSET=utf8mb4`.

---

## ⏩ Navigation
- ⬅️ **Previous:** [13 - Phase 4: Forensic Record Carving](13-phase4-forensic-carving.md)
- 🔄 **Return to Start:** [00 - Beginner's Guide to AccessUtility](00-beginner-guide.md)
