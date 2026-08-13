# Feature 05 - Maintenance Daemon & Backup Scheduler

## 📌 Overview
Access 97 databases accumulate fragmentation daily and suffer from orphan `.ldb` lock files when client PCs crash. This feature adds a background service mode (`AccessUtility.exe daemon`) that automatically inspects database health, cleans orphan locks, compacts database files, and generates timestamped `.zip` backups on a scheduled interval.

---

## 📐 Technical Specification

### 1. Maintenance Pipeline
1. **Orphan Lock Cleanup**: Scans `.ldb` file and removes stale locks if no process holds an active handle.
2. **Health Diagnostics**: Measures fragmentation percentage.
3. **Auto-Compact**: If fragmentation exceeds threshold (default > 15%), executes zero-downtime compaction to a temp file and replaces target atomically.
4. **ZIP Backup**: Creates timestamped backup archives (`Backup_YYYYMMDD_HHMMSS.zip`).

---

## 🎯 User Interface Integration

### CLI Command
```bash
AccessUtility.exe daemon --path C:\Databases\Main97.mdb --interval 24h --backup-dir C:\Backups\
```
