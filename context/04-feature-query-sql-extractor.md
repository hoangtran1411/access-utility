# Feature 04 - Access Query (MSysQueries) SQL Extractor

## 📌 Overview
Access 97 stores saved database queries, views, and parameter queries inside system catalog tables (`MSysQueries` and `MSysObjects`). This feature reconstructs SQL `SELECT`, `JOIN`, `UPDATE`, and `TRANSFORM` query definitions into readable `.sql` files.

---

## 📐 Technical Specification

### 1. `MSysQueries` Catalog Schema
- `MSysQueries` stores query definitions in normalized rows:
  - `Attribute`: 1 = SELECT, 2 = FROM/JOIN, 3 = WHERE, 4 = GROUP BY, 5 = HAVING, 6 = ORDER BY.
  - `Expression`: SQL clause fragment or field reference.
  - `Name1` / `Name2`: Table and column identifiers.

### 2. SQL Reconstructor
- Groups rows by `ObjectId`.
- Resolves table name from `MSysObjects`.
- Rebuilds formatted SQL text:
  ```sql
  -- Query: QuarterlySales
  SELECT Customers.CompanyName, SUM(Orders.Total) AS TotalSales
  FROM Customers INNER JOIN Orders ON Customers.CustomerID = Orders.CustomerID
  GROUP BY Customers.CompanyName;
  ```

---

## 🎯 User Interface Integration

### CLI Command
```bash
AccessUtility.exe extract-queries C:\Databases\Main97.mdb --output ./queries/
```
