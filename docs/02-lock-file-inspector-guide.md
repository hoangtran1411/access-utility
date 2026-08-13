# 02 - Lock File (.ldb) Inspector Guide

When Microsoft Access or any database application opens a `.mdb` database, it creates a corresponding **Lock File (`.ldb`)** in the same directory to track active user connections and page locks.

---

## 🔍 Binary Structure of `.ldb` Files

An `.ldb` file contains one or more **64-byte record blocks**:

```text
+------------------------------------+------------------------------------+
| Bytes 0..31 (32 Bytes ANSI)        | Bytes 32..63 (32 Bytes ANSI)       |
| Computer Name / Workstation ID     | User Name / Security ID            |
+------------------------------------+------------------------------------+
```

### Parsing Logic (`LdbLockInspector.cs`)
```csharp
byte[] bytes = File.ReadAllBytes(ldbPath);
int recordSize = 64;
int count = bytes.Length / recordSize;

for (int i = 0; i < count; i++)
{
    int offset = i * recordSize;
    string compName = Encoding.ASCII.GetString(bytes, offset, 32).Replace("\0", "").Trim();
    string userName = Encoding.ASCII.GetString(bytes, offset + 32, 32).Replace("\0", "").Trim();
}
```

---

## 🔒 Active Locks vs. Orphan Stale Locks

1. **Active Lock**:
   - An active process holds an open handle (`FileShare.None` or `FileShare.ReadWrite`) on the `.mdb` file.
   - Compacting or repairing the database while actively locked can cause severe data corruption.
2. **Orphan / Stale Lock**:
   - Left behind when Microsoft Access or the host system crashes unexpectedly.
   - The `.ldb` file exists on disk, but **no active process holds a lock on the `.mdb` file**.
   - Safe to clean up using `LdbLockInspector.TryCleanOrphanLock(path, out string msg)`.

---

## 🖥 CLI Lock Command

Inspect lock status and connected user names:
```bash
AccessUtility.exe lockstat C:\Databases\MainData97.mdb
```

Sample output:
```text
[+] Inspecting Lock File (.ldb) for: MainData97.mdb
  .ldb File Exists    : True
  File Actively Locked: False
  Orphan Lock Detected: True
  Connected Users Count: 2

--- Connected Users & Computer Names ---
  #1 | Computer: WORKSTATION1 | User: Admin
  #2 | Computer: LAPTOP-DEV | User: Hoang
```

---

## Next Step
Continue to [03 - Compact & Repair Engine](03-compact-and-repair-engine.md) to understand how database defragmentation and page recovery work.
