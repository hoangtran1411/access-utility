# Feature 11 - Web Dashboard Sector Map, Hex Inspector & ERD

## 📌 Overview
The embedded ASP.NET Core Native AOT Web UI (`WebServer.cs`, `DashboardHtml.cs`) provides high-level metrics. However, database administrators and forensic engineers require visual insight into physical 2048-byte page distributions and schema relationships.

This feature adds an interactive database sector map, a low-level hex/ASCII page inspector, and automatic Mermaid Entity-Relationship Diagram (ERD) generation.

---

## 📐 Technical Specification

### 1. Sector Map REST API (`/api/pages`)
- Classifies each 2048-byte page: Header (`0x00`), PAM (`0x01`), TDEF (`0x02`), Data (`0x01`), Slack/Free (`0x00`), Corrupt.
- Returns page layout array for interactive grid rendering in the Web Dashboard.

### 2. Hex Inspector REST API (`/api/pages/{pageIndex}/hex`)
- Returns formatted 16-byte hex rows and ASCII character representation for any chosen 2048-byte page.

### 3. Automated Mermaid ERD Visualizer
- Generates interactive Mermaid ERD diagrams from TDEF relationships and primary keys.
- Available in Web UI and via CLI: `AccessUtility.exe diff db.mdb --format erd`.

---

## 🎯 User Interface Integration

### Web Dashboard
- Accessible at `http://localhost:5000` via `AccessUtility.exe web`.
- New tabs: **[Sector Map]**, **[Hex Inspector]**, and **[ERD Graph]**.
