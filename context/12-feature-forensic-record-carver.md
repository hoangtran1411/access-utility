# Feature 12 - Forensic Record Carver & Deleted Data Recovery

## 📌 Overview
In Microsoft Access 97 / Jet 3.5 databases, deleted records and dropped tables leave raw binary payloads intact in page slack space until overwritten. Standard repair tools ignore unreferenced bytes.

This feature adds a deep **Forensic Record Carver** capable of scanning unallocated page bytes, matching column data signatures, and salvaging deleted rows without requiring active TDEF pointer references.

---

## 📐 Technical Specification

### 1. Slot Table & Slack Space Scanner (`Engine/ForensicCarver.cs`)
- Parse row slot offset arrays from the end of data pages (`0x01`).
- Compute unreferenced byte boundaries (slack regions) within each active page.
- Scan freed pages (type `0x00`) for orphaned record structures.

### 2. Pattern Matching & Column Reconstruction
- Reconstruct column data based on expected schema types or general Jet heuristics.
- Compute confidence scores based on column length validation and null terminator checks.

### 3. Reporting & Recovery Exporter
- Output salvaged records to a dedicated recovery SQLite database or JSON file.

---

## 🎯 User Interface Integration

### CLI Commands
```bash
AccessUtility.exe repair damaged.mdb --carve-deleted --output ./recovered/
AccessUtility.exe diagnose database.mdb --forensic-scan
```
