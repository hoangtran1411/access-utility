# Feature 13 - Multi-Dialect SQL Schema & Migration Exporter

## 📌 Overview
Access 97 / Jet 3.5 databases often need to be migrated to modern relational database management systems (RDBMS) such as PostgreSQL, MySQL/MariaDB, Microsoft SQL Server, SQLite, Oracle, or standard ANSI SQL.

This feature adds a rich **Multi-Dialect SQL Migration Exporter** that translates Jet 3.5 schemas (tables, columns, primary keys, foreign keys, and saved query views) and record payloads into target-optimized SQL scripts.

---

## 📐 Technical Specification

### 1. Multi-Dialect SQL Generator (`Exporters/SqlMigrationExporter.cs`)
Supports syntax, data types, auto-increment sequences, and identifier quoting for:
- **PostgreSQL** (`SERIAL`, `TIMESTAMP WITH TIME ZONE`, `DOUBLE PRECISION`, `BYTEA`, `public."table"`)
- **MySQL / MariaDB** (`AUTO_INCREMENT`, `DATETIME`, `LONGTEXT`, `` `table` ``, `ENGINE=InnoDB`)
- **Microsoft SQL Server (T-SQL)** (`IDENTITY(1,1)`, `DATETIME2`, `NVARCHAR(MAX)`, `[table]`)
- **SQLite** (`INTEGER PRIMARY KEY AUTOINCREMENT`, `TEXT`, `BLOB`)
- **Oracle** (`GENERATED ALWAYS AS IDENTITY`, `NUMBER`, `VARCHAR2(4000)`, `TIMESTAMP`)
- **ANSI SQL** (Standard portable DDL)

### 2. Advanced Migration Capabilities
- **Foreign Keys & Primary Keys**: Automatically infers and emits `ALTER TABLE ADD CONSTRAINT ... FOREIGN KEY` or inline constraints.
- **Views & Saved Queries**: Integrates saved queries from `MSysQueries` as `CREATE VIEW` statements.
- **Batch Inserts**: Generates multi-row `INSERT INTO ... VALUES (...), (...)` with configurable `--batch-size` for 50x faster import execution.
- **Modes**: `--schema-only` (DDL only), `--data-only` (DML only), or full schema + data.
- **Transactional Wrappers**: Wraps scripts in `BEGIN TRANSACTION` / `COMMIT`.

---

## 🎯 User Interface Integration

### CLI Commands
```bash
# Export schema and data for PostgreSQL
AccessUtility.exe export Northwind97.mdb --format sql --dialect postgres --output ./pg_northwind.sql

# Export DDL schema only for MySQL
AccessUtility.exe schema Northwind97.mdb --dialect mysql --output ./mysql_schema.sql

# Export data only with batch inserts for SQL Server
AccessUtility.exe export Northwind97.mdb --format sql --dialect mssql --data-only --batch-size 1000 --output ./mssql_data.sql
```
