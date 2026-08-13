# 03 - Compact & Repair Engine Guide

This guide explains the inner mechanics of database compaction, space defragmentation, and deep sector repair for Microsoft Access 97 `.mdb` databases.

---

## 🧹 Database Compactor Engine (`Jet3Compactor.cs`)

Over time, modifying or deleting records in Access 97 creates **slack space** and fragmented deleted record slots inside 2048-byte pages.

### Compacting Steps:
1. **Safety Check**: Inspects `.ldb` lock status. Prevents compaction if active user connections exist (unless `--force-unlock` is supplied).
2. **Schema & Record Extraction**: Reads active Table Definitions (TDEF) and active row records, ignoring deleted record slots (`slotPointer & 0x8000`).
3. **Sequential Page Allocation**:
   - Re-writes Page 0 (Clean Jet 3.5 Header).
   - Re-writes Page 1 (Clean Page Allocation Map - PAM).
   - Writes contiguous Table Definition (TDEF) pages.
   - Writes contiguous Data pages containing active records only.
4. **Result Calculation**: Returns original size, compacted size, and exact space saved (bytes and %).

```bash
AccessUtility.exe compact C:\Databases\Bloated97.mdb --output Clean97.mdb
```

---

## 🚑 Database Repairer Engine (`Jet3Repairer.cs`)

When an Access 97 database experiences sudden power loss, header corruption, or index B-tree corruption, standard Jet drivers fail to open the file.

### Repair & Recovery Steps:
1. **Deep Sector Scan**: Iterates through every 2048-byte page in the file.
2. **TDEF Recovery**: Identifies Table Definition pages by scanning for magic bytes (`0x02, 0x01`). Reconstructs table schemas even if catalog system tables (`MSysObjects`) are wiped out.
3. **Data Page Salvaging**: Matches Data pages (`0x01`) to recovered table TDEF pointers and extracts valid record rows, bypassing corrupted byte ranges.
4. **Reconstruction**: Invokes the Compactor engine to write out a fully reconstructed, healthy Access 97 `.mdb` file.

```bash
AccessUtility.exe repair C:\Databases\Corrupted97.mdb --output Repaired97.mdb --force-unlock
```

---

## Next Step
Continue to [04 - CLI & Web UI Guide](04-cli-and-web-ui-guide.md) to explore the interactive terminal menu and embedded Web Dashboard.
