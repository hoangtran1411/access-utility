# Feature 01 - Database Password & Security Inspector

## 📌 Overview
Access 97 (`.mdb` / Jet 3.5) databases frequently have lost database passwords or legacy User-Level Security (ULS) `System.mdw` workgroups. This feature adds automatic password extraction and security group inspection to **AccessUtility**.

---

## 📐 Technical Specification

### 1. Jet 3.5 Database Password Recovery
- In Access 97, database passwords are stored at offset `0x42` (66 decimal) in **Page 0** (Header Page).
- The password block is 14 bytes long and XOR-masked using a static Jet 3.5 key:
  ```csharp
  byte[] jet3Mask = new byte[] { 0x86, 0xFB, 0xEC, 0x37, 0x5D, 0x44, 0x9C, 0xFA, 0xC6, 0x5E, 0x28, 0xE6, 0x13, 0xB6 };
  ```
- **Algorithm**:
  1. Read bytes `0x42..0x4F` from Page 0.
  2. Perform byte-by-byte XOR with `jet3Mask`.
  3. Trim null terminators (`\0`) to reveal the plaintext database password.

### 2. Workgroup (`System.mdw`) Inspection
- Parses `System.mdw` workgroup files to list users, groups, and permission SIDs.

---

## 🎯 User Interface Integration

### CLI Command
```bash
AccessUtility.exe password C:\Databases\Protected97.mdb
```

### Output Format
```text
[+] Inspecting Access 97 Database Security: Protected97.mdb
  Database Password: "MySecretPassword123"
  Password Protected: True
```
