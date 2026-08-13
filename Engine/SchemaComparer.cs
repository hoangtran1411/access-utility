using System;
using System.Collections.Generic;
using System.Linq;
using AccessUtility.Models;

namespace AccessUtility.Engine
{
    /// <summary>
    /// Feature 02: Schema Diff &amp; Migration Generator
    /// Compares two Access 97 database schemas (tables, columns, row counts)
    /// and produces a structured diff result used by MigrationScriptExporter.
    /// </summary>
    public static class SchemaComparer
    {
        /// <summary>
        /// Compares the schema of a source database against a target database.
        /// Source = "new" or "development" version. Target = "old" or "production" version.
        /// The diff describes what must change in Target to match Source.
        /// </summary>
        public static SchemaDiffResult Compare(AccessDatabase source, AccessDatabase target)
        {
            var result = new SchemaDiffResult
            {
                SourcePath = source.FilePath,
                TargetPath = target.FilePath
            };

            var sourceTableMap = BuildTableMap(source.Tables);
            var targetTableMap = BuildTableMap(target.Tables);

            // ── Tables only in Source → Added ──
            foreach (var kvp in sourceTableMap)
            {
                if (!targetTableMap.ContainsKey(kvp.Key))
                {
                    result.AddedTables.Add(new TableDiff
                    {
                        TableName = kvp.Value.Name,
                        DiffType = TableDiffType.Added,
                        Columns = kvp.Value.Columns.Select(c => new ColumnSnapshot
                        {
                            Name = c.Name,
                            DataType = c.DataType,
                            Length = c.Length,
                            IsNullable = c.IsNullable,
                            IsAutoNumber = c.IsAutoNumber,
                            IsVariableLength = c.IsVariableLength
                        }).ToList(),
                        SourceRowCount = kvp.Value.Rows.Count
                    });
                }
            }

            // ── Tables only in Target → Removed ──
            foreach (var kvp in targetTableMap)
            {
                if (!sourceTableMap.ContainsKey(kvp.Key))
                {
                    result.RemovedTables.Add(new TableDiff
                    {
                        TableName = kvp.Value.Name,
                        DiffType = TableDiffType.Removed,
                        TargetRowCount = kvp.Value.Rows.Count
                    });
                }
            }

            // ── Tables in both → compare columns ──
            foreach (var kvp in sourceTableMap)
            {
                if (targetTableMap.TryGetValue(kvp.Key, out var targetTable))
                {
                    var sourceTable = kvp.Value;
                    var tableDiff = CompareTable(sourceTable, targetTable);
                    if (tableDiff != null)
                    {
                        result.ModifiedTables.Add(tableDiff);
                    }
                }
            }

            result.HasDifferences = result.AddedTables.Count > 0
                || result.RemovedTables.Count > 0
                || result.ModifiedTables.Count > 0;

            return result;
        }

        /// <summary>
        /// Compares two tables with the same name from source and target databases.
        /// Returns null if schemas are identical.
        /// </summary>
        private static TableDiff? CompareTable(AccessTable source, AccessTable target)
        {
            var diff = new TableDiff
            {
                TableName = source.Name,
                DiffType = TableDiffType.Modified,
                SourceRowCount = source.Rows.Count,
                TargetRowCount = target.Rows.Count
            };

            var sourceColMap = BuildColumnMap(source.Columns);
            var targetColMap = BuildColumnMap(target.Columns);

            // Columns only in source → Added
            foreach (var kvp in sourceColMap)
            {
                if (!targetColMap.ContainsKey(kvp.Key))
                {
                    diff.AddedColumns.Add(new ColumnDiff
                    {
                        ColumnName = kvp.Value.Name,
                        DiffType = ColumnDiffType.Added,
                        NewDataType = kvp.Value.DataType,
                        NewLength = kvp.Value.Length,
                        IsNullable = kvp.Value.IsNullable
                    });
                }
            }

            // Columns only in target → Removed
            foreach (var kvp in targetColMap)
            {
                if (!sourceColMap.ContainsKey(kvp.Key))
                {
                    diff.RemovedColumns.Add(new ColumnDiff
                    {
                        ColumnName = kvp.Value.Name,
                        DiffType = ColumnDiffType.Removed,
                        OldDataType = kvp.Value.DataType,
                        OldLength = kvp.Value.Length
                    });
                }
            }

            // Columns in both → check type/length changes
            foreach (var kvp in sourceColMap)
            {
                if (targetColMap.TryGetValue(kvp.Key, out var targetCol))
                {
                    var sourceCol = kvp.Value;
                    bool typeChanged = sourceCol.DataType != targetCol.DataType;
                    bool lengthChanged = sourceCol.Length != targetCol.Length;
                    bool nullableChanged = sourceCol.IsNullable != targetCol.IsNullable;

                    if (typeChanged || lengthChanged || nullableChanged)
                    {
                        diff.ModifiedColumns.Add(new ColumnDiff
                        {
                            ColumnName = sourceCol.Name,
                            DiffType = ColumnDiffType.Modified,
                            OldDataType = targetCol.DataType,
                            NewDataType = sourceCol.DataType,
                            OldLength = targetCol.Length,
                            NewLength = sourceCol.Length,
                            IsNullable = sourceCol.IsNullable,
                            TypeChanged = typeChanged,
                            LengthChanged = lengthChanged,
                            NullableChanged = nullableChanged
                        });
                    }
                }
            }

            // Row count difference
            diff.RowCountDifference = source.Rows.Count - target.Rows.Count;

            bool hasDiff = diff.AddedColumns.Count > 0
                || diff.RemovedColumns.Count > 0
                || diff.ModifiedColumns.Count > 0
                || diff.RowCountDifference != 0;

            return hasDiff ? diff : null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        private static Dictionary<string, AccessTable> BuildTableMap(List<AccessTable> tables)
        {
            var map = new Dictionary<string, AccessTable>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in tables)
            {
                if (!string.IsNullOrWhiteSpace(t.Name))
                    map[t.Name] = t;
            }
            return map;
        }

        private static Dictionary<string, AccessColumn> BuildColumnMap(List<AccessColumn> columns)
        {
            var map = new Dictionary<string, AccessColumn>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in columns)
            {
                if (!string.IsNullOrWhiteSpace(c.Name))
                    map[c.Name] = c;
            }
            return map;
        }
    }
}
