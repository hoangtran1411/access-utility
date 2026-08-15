# Feature 10 - Modern Analytical Exporters (Parquet & DuckDB)

## 📌 Overview
Modern data modernization workflows require direct ingestion into analytical data lakes, cloud warehouses, or local OLAP engines. Exporting to raw CSV or SQL dumps loses data types and results in excessive file sizes.

This feature adds high-performance, strongly-typed exporters for **Apache Parquet (`.parquet`)**, **DuckDB (`.duckdb`)**, and **Streaming JSON Lines (`.jsonl`)**.

---

## 📐 Technical Specification

### 1. Parquet Exporter (`Exporters/ParquetExporter.cs`)
- Native AOT-compatible schema generation mapping Jet 3.5 types (AutoNumber, Text, Long, Currency, DateTime, Boolean) to strict Parquet physical types.
- Efficient columnar chunk writing with Snappy or Zstandard compression.

### 2. DuckDB Exporter (`Exporters/DuckDbExporter.cs`)
- Direct export into a queryable standalone DuckDB database file using native table structures.

### 3. JSON Lines Exporter (`Exporters/JsonLinesExporter.cs`)
- Line-delimited streaming JSON output using `System.Text.Json` Native AOT source-generated serializer.

---

## 🎯 User Interface Integration

### CLI Commands
```bash
AccessUtility.exe export data.mdb --format parquet --output ./exports/
AccessUtility.exe export data.mdb --format duckdb --output ./analytics.duckdb
AccessUtility.exe export data.mdb --format jsonl --output ./exports/
```
