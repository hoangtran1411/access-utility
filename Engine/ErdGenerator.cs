using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AccessUtility.Models;

namespace AccessUtility.Engine
{
    public static class ErdGenerator
    {
        public static ErdDiagramResult GenerateErd(AccessDatabase db)
        {
            var result = new ErdDiagramResult
            {
                DatabasePath = db.FilePath
            };

            var erdTables = new List<ErdTable>();
            var tableNameMap = new Dictionary<string, AccessTable>(StringComparer.OrdinalIgnoreCase);

            foreach (var table in db.Tables)
            {
                if (table.Columns.Count == 0) continue;
                tableNameMap[table.Name] = table;
            }

            // Step 1: Analyze columns, detect Primary Keys and Foreign Keys
            foreach (var table in db.Tables)
            {
                if (table.Columns.Count == 0) continue;

                var erdTable = new ErdTable
                {
                    Name = SanitizeIdentifier(table.Name)
                };

                // Detect Primary Key
                string? detectedPkName = null;
                var autoNumCol = table.Columns.FirstOrDefault(c => c.IsAutoNumber);
                if (autoNumCol != null)
                {
                    detectedPkName = autoNumCol.Name;
                }
                else
                {
                    var idCol = table.Columns.FirstOrDefault(c =>
                        c.Name.Equals("ID", StringComparison.OrdinalIgnoreCase) ||
                        c.Name.Equals($"{table.Name}ID", StringComparison.OrdinalIgnoreCase) ||
                        c.Name.Equals($"{table.Name}_ID", StringComparison.OrdinalIgnoreCase) ||
                        c.Name.Equals($"{table.Name}Id", StringComparison.OrdinalIgnoreCase));
                    if (idCol != null)
                    {
                        detectedPkName = idCol.Name;
                    }
                    else
                    {
                        var anyIdCol = table.Columns.FirstOrDefault(c => c.Name.EndsWith("ID", StringComparison.OrdinalIgnoreCase));
                        if (anyIdCol != null) detectedPkName = anyIdCol.Name;
                    }
                }

                foreach (var col in table.Columns)
                {
                    bool isPk = detectedPkName != null && col.Name.Equals(detectedPkName, StringComparison.OrdinalIgnoreCase);
                    bool isFk = false;
                    string? foreignTable = null;
                    string? foreignCol = null;

                    if (!isPk)
                    {
                        // Check if column refers to another table (e.g. CustomerID -> Customers / Customer)
                        foreach (var otherTable in db.Tables)
                        {
                            if (otherTable == table) continue;

                            string singular = otherTable.Name.TrimEnd('s', 'S');
                            if (col.Name.Equals($"{otherTable.Name}ID", StringComparison.OrdinalIgnoreCase) ||
                                col.Name.Equals($"{otherTable.Name}_ID", StringComparison.OrdinalIgnoreCase) ||
                                col.Name.Equals($"{singular}ID", StringComparison.OrdinalIgnoreCase) ||
                                col.Name.Equals($"{singular}_ID", StringComparison.OrdinalIgnoreCase) ||
                                col.Name.Equals($"{otherTable.Name}Id", StringComparison.OrdinalIgnoreCase) ||
                                col.Name.Equals($"{singular}Id", StringComparison.OrdinalIgnoreCase))
                            {
                                isFk = true;
                                foreignTable = otherTable.Name;
                                foreignCol = col.Name;
                                break;
                            }
                        }
                    }

                    erdTable.Columns.Add(new ErdTableColumn
                    {
                        Name = SanitizeIdentifier(col.Name),
                        DataType = MapToMermaidType(col.DataType),
                        IsPrimaryKey = isPk,
                        IsForeignKey = isFk,
                        ForeignTable = foreignTable,
                        ForeignColumn = foreignCol
                    });
                }

                erdTables.Add(erdTable);
            }

            result.Tables = erdTables;
            result.TableCount = erdTables.Count;

            // Step 2: Build Relationships
            var relationships = new List<ErdRelationship>();
            foreach (var t in erdTables)
            {
                foreach (var col in t.Columns)
                {
                    if (col.IsForeignKey && !string.IsNullOrEmpty(col.ForeignTable))
                    {
                        string fromTable = SanitizeIdentifier(col.ForeignTable);
                        string toTable = t.Name;

                        bool exists = relationships.Any(r => r.FromTable == fromTable && r.ToTable == toTable && r.FromColumn == (col.ForeignColumn ?? col.Name));
                        if (!exists)
                        {
                            relationships.Add(new ErdRelationship
                            {
                                FromTable = fromTable,
                                ToTable = toTable,
                                FromColumn = col.ForeignColumn ?? col.Name,
                                ToColumn = col.Name,
                                RelationshipType = "||--o{",
                                Label = "references"
                            });
                        }
                    }
                }
            }

            result.Relationships = relationships;
            result.RelationshipCount = relationships.Count;

            // Step 3: Generate Mermaid ERD code
            var sb = new StringBuilder();
            sb.AppendLine("erDiagram");

            if (relationships.Count > 0)
            {
                foreach (var rel in relationships)
                {
                    sb.AppendLine($"    {rel.FromTable} {rel.RelationshipType} {rel.ToTable} : \"{rel.Label}\"");
                }
                sb.AppendLine();
            }

            foreach (var table in erdTables)
            {
                sb.AppendLine($"    {table.Name} {{");
                foreach (var col in table.Columns)
                {
                    string modifier = col.IsPrimaryKey ? " PK" : (col.IsForeignKey ? " FK" : "");
                    sb.AppendLine($"        {col.DataType} {col.Name}{modifier}");
                }
                sb.AppendLine("    }");
            }

            result.MermaidCode = sb.ToString();

            // Step 4: Markdown representation
            var mdSb = new StringBuilder();
            mdSb.AppendLine($"# Entity Relationship Diagram (ERD)");
            mdSb.AppendLine();
            mdSb.AppendLine($"> Database: `{Path.GetFileName(db.FilePath)}`");
            mdSb.AppendLine($"> Total Tables: {result.TableCount} | Detected Relationships: {result.RelationshipCount}");
            mdSb.AppendLine();
            mdSb.AppendLine("```mermaid");
            mdSb.Append(result.MermaidCode);
            mdSb.AppendLine("```");
            mdSb.AppendLine();
            mdSb.AppendLine("## Table Summaries");
            mdSb.AppendLine();

            foreach (var t in erdTables)
            {
                mdSb.AppendLine($"### Table: `{t.Name}`");
                mdSb.AppendLine("| Column | Type | Key | References |");
                mdSb.AppendLine("| :--- | :--- | :--- | :--- |");
                foreach (var c in t.Columns)
                {
                    string keyStr = c.IsPrimaryKey ? "🔑 **PK**" : (c.IsForeignKey ? "🔗 **FK**" : "-");
                    string refStr = c.IsForeignKey && !string.IsNullOrEmpty(c.ForeignTable) ? $"`{c.ForeignTable}`" : "-";
                    mdSb.AppendLine($"| `{c.Name}` | `{c.DataType}` | {keyStr} | {refStr} |");
                }
                mdSb.AppendLine();
            }

            result.MarkdownContent = mdSb.ToString();
            return result;
        }

        public static string ExportErdToMarkdown(AccessDatabase db, string outputFilePath)
        {
            string dir = Path.GetDirectoryName(Path.GetFullPath(outputFilePath)) ?? ".";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var erd = GenerateErd(db);
            File.WriteAllText(outputFilePath, erd.MarkdownContent, Encoding.UTF8);
            return outputFilePath;
        }

        private static string MapToMermaidType(JetDataType type) => type switch
        {
            JetDataType.Boolean => "boolean",
            JetDataType.Byte => "byte",
            JetDataType.Integer => "short",
            JetDataType.LongInteger or JetDataType.Autonumber => "int",
            JetDataType.Single => "float",
            JetDataType.Double => "double",
            JetDataType.Currency => "decimal",
            JetDataType.DateTime => "datetime",
            JetDataType.Binary => "blob",
            JetDataType.Guid => "uuid",
            JetDataType.Text or JetDataType.Memo => "string",
            _ => "string"
        };

        private static string SanitizeIdentifier(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Anonymous";
            var sb = new StringBuilder();
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_') sb.Append(c);
                else sb.Append('_');
            }
            string sanitized = sb.ToString().Trim('_');
            return string.IsNullOrEmpty(sanitized) ? "Table" : sanitized;
        }
    }
}
