# 11 - Phase 2: Modern Analytical Exporters (Parquet & DuckDB)

This guide outlines the technical implementation details for **Phase 2: Modern Analytical Exporters** in AccessUtility.

---

## 🎯 Goal & Problem Statement

Legacy `.mdb` databases are frequently migrated into modern cloud data warehouses (Snowflake, BigQuery, Databricks) or local analytical query engines (DuckDB, Polars). 

Exporting to raw `.csv` or standard `.sql` script files has significant drawbacks:
- Loss of strict column data types.
- Massive file sizes due to uncompressed text.
- Slow ingestion times in data processing engines.

Phase 2 adds direct native exports to **Apache Parquet (`.parquet`)**, **DuckDB (`.duckdb`)**, and **Streaming JSON Lines (`.jsonl`)**.

---

## 📦 Exporter Matrix

```
+───────────────────+       +───────────────────────────────────────+
|                   |  ──►  | Apache Parquet (.parquet)             |
|                   |       | • Native column chunk compression     |
|                   |       | • Snappy / Zstandard codecs           |
|                   |       +───────────────────────────────────────+
|   Access 97 .mdb  |  ──►  | DuckDB Database (.duckdb)             |
|   (Jet 3.5 Tables)|       | • Direct standalone analytical DB     |
|                   |       | • Fast vectorised SQL queries         |
|                   |       +───────────────────────────────────────+
|                   |  ──►  | JSON Lines (.jsonl)                   |
|                   |       | • Line-delimited streaming records    |
+───────────────────+       +───────────────────────────────────────+
```

---

## 💻 Implementation Blueprint

### 1. Apache Parquet Exporter (`ParquetExporter.cs`)
Using Native AOT-compatible Parquet serializers:

```csharp
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using AccessUtility.Models;

namespace AccessUtility.Exporters
{
    public static class ParquetExporter
    {
        public static async Task ExportTableAsync(AccessTable table, string outputFilePath)
        {
            var fields = new List<Field>();
            foreach (var col in table.Columns)
            {
                fields.Add(new DataField(col.Name, MapToParquetType(col.Type)));
            }

            var schema = new ParquetSchema(fields);

            using var fileStream = File.Create(outputFilePath);
            using var parquetWriter = await ParquetWriter.CreateAsync(schema, fileStream);
            using var groupWriter = parquetWriter.CreateRowGroup();

            foreach (var col in table.Columns)
            {
                var columnData = table.Rows.Select(r => r.Values.GetValueOrDefault(col.Name)).ToArray();
                var dataColumn = new DataColumn((DataField)schema.DataFields.First(f => f.Name == col.Name), columnData);
                await groupWriter.WriteColumnAsync(dataColumn);
            }
        }

        private static Type MapToParquetType(string jetType) => jetType switch
        {
            "Long" or "AutoNumber" => typeof(int),
            "Integer" => typeof(short),
            "Byte" => typeof(byte),
            "Double" => typeof(double),
            "Single" => typeof(float),
            "Currency" => typeof(decimal),
            "DateTime" => typeof(DateTime),
            "Boolean" => typeof(bool),
            _ => typeof(string)
        };
    }
}
```

### 2. DuckDB Exporter (`DuckDbExporter.cs`)
Directly generates a queryable DuckDB database file using SQLite-to-DuckDB native bridge or direct batch insertion:

```bash
# Example CLI usage after implementation
AccessUtility.exe export C:\Legacy\inventory.mdb --format duckdb --output ./analytics.duckdb
```

### 3. CLI Command Extensions
Extend `Program.cs` export command options:
```bash
AccessUtility.exe export sample97.mdb --format parquet --output ./exports/
AccessUtility.exe export sample97.mdb --format duckdb --output ./exports/data.duckdb
AccessUtility.exe export sample97.mdb --format jsonl --output ./exports/
```

---

## 📈 Performance & Compression Comparison

| Format | Output Size (100k Rows) | Read Speed in Python / BI | Type Safety |
| :--- | :--- | :--- | :--- |
| **Raw CSV** | ~48.5 MB | Slow (text parsing) | ❌ None (Strings) |
| **SQL Insert Script** | ~72.0 MB | Very Slow (row by row) | ⚠️ Depends on DBMS |
| **SQLite (`.sqlite`)** | ~22.0 MB | Fast | ⚠️ Weak typing |
| **Apache Parquet (`.parquet`)** | **~4.2 MB** | **Ultra Fast (Columnar)** |  Strong schema |
| **DuckDB (`.duckdb`)** | **~6.1 MB** | **Instant Vectorized SQL** |  Strong schema |

---

## ⏩ Navigation
- ⬅️ **Previous:** [10 - Phase 1: Memory & Streaming Optimizations](10-phase1-memory-and-streaming.md)
- ➡️ **Next:** [12 - Phase 3: Web Dashboard Visualizer & ERD](12-phase3-web-visualizer-and-erd.md)
- 🔄 **Return to Start:** [00 - Beginner's Guide to AccessUtility](00-beginner-guide.md)
