# Feature 03 - OLE Object & Embedded File Extractor

## 📌 Overview
Legacy Access 97 applications frequently stored images (BMP, JPEG) and documents (Word, Excel) inside `Long Binary` OLE fields. Microsoft Access wrapped these files with a 78-byte OLE Container Header (`OLE2STREAM`). This feature strips container headers and extracts raw embedded files into a folder.

---

## 📐 Technical Specification

### 1. OLE Container Header Stripping
- Scans `Long Binary` blob fields.
- Locates magic signatures:
  - `0x42, 0x4D` (BMP Image Header)
  - `0xFF, 0xD8, 0xFF` (JPEG Image Header)
  - `0x89, 0x50, 0x4E, 0x47` (PNG Image Header)
  - `0x25, 0x50, 0x44, 0x46` (PDF Document)
  - `0xD0, 0xCF, 0x11, 0xE0` (Compound Document File Header / MS Office)
- Bypasses the initial 78-byte Access OLE wrapper and extracts raw stream bytes.

### 2. Extractor Pipeline
Outputs extracted files into structured directories:
`./extracted_ole/{TableName}/{ColumnName}_Row_{RowID}.bmp`

---

## 🎯 User Interface Integration

### CLI Command
```bash
AccessUtility.exe extract-ole C:\Databases\Products97.mdb --output ./extracted_files
```
