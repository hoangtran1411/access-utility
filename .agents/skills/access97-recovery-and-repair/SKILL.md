---
name: access97-recovery-and-repair
description: >-
  Expert runbook and diagnostic workflow for inspecting, repairing, defragmenting,
  and carving deleted records from corrupted Microsoft Access 97 (Jet 3.5) databases.
---

# Access 97 Database Recovery & Repair Runbook

This skill provides step-by-step diagnostic and disaster recovery procedures for damaged or legacy `.mdb` databases using the **AccessUtility** engine.

---

## 🔍 Step 1: Lock File & Initial Health Diagnostics

Before modifying or repairing a database, determine its lock status and page structure:

1. **Check for Active or Orphan Locks**:
   ```bash
   AccessUtility.exe lockstat C:\Data\Damaged.mdb
   ```
   - If an active user connection is shown, ensure other applications have closed the file.
   - If an **orphan lock** is detected (left from an unexpected power outage or crash), clean it safely:
     ```bash
     AccessUtility.exe lockstat C:\Data\Damaged.mdb --clean
     ```

2. **Run Page Allocation Map (PAM) Diagnostics**:
   ```bash
   AccessUtility.exe diagnose C:\Data\Damaged.mdb
   ```
   - Inspect the **Fragmentation Percentage**, **Corrupt Pages Count**, and discovered table summaries.

---

## 🛠️ Step 2: Compaction & Defragmentation

If the database is uncorrupted but bloated or fragmented:

```bash
AccessUtility.exe compact C:\Data\Main.mdb --output C:\Data\Main_Compacted.mdb
```
- Rebuilds continuous 2048-byte page clusters.
- Purges unreferenced deleted slot gaps.
- Reduces disk space by up to 80%.

---

## 🚑 Step 3: Deep Sector Page Recovery

When physical pages or headers are damaged (`Corrupt Pages > 0`):

```bash
# Deep sector repair isolating corrupted byte blocks
AccessUtility.exe repair C:\Data\Damaged.mdb --output C:\Data\Damaged_Repaired.mdb --force-unlock
```
- Scans all 2KB pages independently.
- Extracts valid `TDEF` tables and reads recoverable record rows.
- Rebuilds a healthy `.mdb` file.

---

## 🔬 Step 4: Forensic Deleted Record Carving

If records or tables were accidentally deleted, dropped, or corrupted:

```bash
# 1. Forensic scan to report salvageable records
AccessUtility.exe diagnose C:\Data\Damaged.mdb --forensic-scan

# 2. Carve deleted records into a dedicated recovery SQLite database
AccessUtility.exe carve C:\Data\Damaged.mdb --output ./recovered/salvaged_records.sqlite

# 3. Or carve directly during deep repair
AccessUtility.exe repair C:\Data\Damaged.mdb --carve-deleted --output C:\Data\Repaired.mdb
```

---

## 📊 Step 5: Visual Sector Inspection & Hex Dump

To visually verify page layout or inspect raw bytes:

- **Launch Interactive Web Dashboard**:
  ```bash
  AccessUtility.exe web --port 5000
  ```
  Open `http://localhost:5000` to view the 2KB Sector Map, live Hex Inspector, and Mermaid ERD diagram.

- **Inspect Raw Hex Dump via CLI**:
  ```bash
  AccessUtility.exe hex C:\Data\Main.mdb --page 0
  ```
