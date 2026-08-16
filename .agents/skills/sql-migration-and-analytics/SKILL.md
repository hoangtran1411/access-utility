---
name: sql-migration-and-analytics
description: >-
  Procedure and reference guide for migrating Access 97 databases to modern SQL systems
  (PostgreSQL, MySQL, SQL Server, Oracle, SQLite) and modern analytical columnar formats (Parquet, DuckDB, JSONL).
---

# SQL Migration & Modern Analytics Runbook

This skill outlines best practices and command workflows for exporting legacy Access 97 (`.mdb`) schemas and data to modern cloud, database, and analytical platforms.

---

## 🎯 Target Format Selection

| Target Platform / Use Case | Recommended Format / Dialect | Command Example |
| :--- | :--- | :--- |
| **PostgreSQL** (Production RDBMS) | `--format sql --dialect postgres` | `AccessUtility.exe export Main.mdb --format sql --dialect postgres --output ./pg.sql` |
| **MySQL / MariaDB** (Web App / Cloud) | `--format sql --dialect mysql` | `AccessUtility.exe export Main.mdb --format sql --dialect mysql --output ./mysql.sql` |
| **Microsoft SQL Server** (Enterprise T-SQL) | `--format sql --dialect mssql` | `AccessUtility.exe export Main.mdb --format sql --dialect mssql --output ./mssql.sql` |
| **Oracle Database** | `--format sql --dialect oracle` | `AccessUtility.exe export Main.mdb --format sql --dialect oracle --output ./ora.sql` |
| **Big Data / Data Lake / DuckDB** | `--format parquet` | `AccessUtility.exe export Main.mdb --format parquet --output ./data.parquet` |
| **Fast Local Analytics (OLAP)** | `--format duckdb` | `AccessUtility.exe export Main.mdb --format duckdb --output ./analytics.duckdb` |
| **JSON Pipeline / Kafka / Elastic** | `--format jsonl` | `AccessUtility.exe export Main.mdb --format jsonl --output ./stream.jsonl` |
| **Local SQLite Database** | `--format sqlite` | `AccessUtility.exe export Main.mdb --format sqlite --output ./main.sqlite` |

---

## 🏗️ 1. DDL Schema-Only Generation

To extract clean database schemas, primary keys, foreign key constraints, and views without data rows:

```bash
# PostgreSQL Schema DDL
AccessUtility.exe schema Main.mdb --dialect postgres --output ./pg_schema.sql

# MySQL Schema DDL
AccessUtility.exe schema Main.mdb --dialect mysql --output ./mysql_schema.sql

# SQL Server Schema DDL
AccessUtility.exe schema Main.mdb --dialect mssql --output ./mssql_schema.sql
```

---

## ⚡ 2. High-Throughput Batched Data Ingestion

For databases with large tables (> 50,000 records), tune batch sizes to accelerate SQL import:

```bash
AccessUtility.exe export Main.mdb --format sql --dialect postgres --batch-size 1000 --output ./pg_data.sql
```

---

## 📊 3. Analytical Columnar Exports (Parquet & DuckDB)

- **Export Columnar Parquet**:
  ```bash
  AccessUtility.exe export Main.mdb --format parquet --output ./exports/
  ```
  - Compresses fields with physical Snappy compression.
  - Retains native date/time, decimal, integer, and boolean column types.

- **Export Standalone DuckDB Analytics Database**:
  ```bash
  AccessUtility.exe export Main.mdb --format duckdb --output ./analytics.duckdb
  ```
  - Appends rows directly via high-speed native vector appenders.
  - Queries instantly with DuckDB CLI or Python/R.

- **Streaming Line-Delimited JSON (JSON Lines)**:
  ```bash
  AccessUtility.exe export Main.mdb --format jsonl --output ./stream.jsonl
  ```

---

## 🤖 4. Natural Language Assistant Workflows

You can also use natural language with the AX Assistant:

```bash
AccessUtility.exe ax "Export schema and data from Northwind97.mdb to PostgreSQL SQL script"
AccessUtility.exe ax "Convert inventory.mdb to parquet columnar format"
AccessUtility.exe ax "Generate MySQL migration script for sales.mdb"
```
