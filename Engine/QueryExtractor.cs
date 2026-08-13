using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AccessUtility.Models;
using Serilog;

namespace AccessUtility.Engine
{
    /// <summary>
    /// Feature 04: Access Query SQL Extractor
    /// Parses MSysQueries and MSysObjects to reconstruct SQL SELECT, JOIN, WHERE, GROUP BY clauses.
    /// </summary>
    public static class QueryExtractor
    {
        public static QueryExtractionReport ExtractQueries(AccessDatabase db, string outputDir)
        {
            var report = new QueryExtractionReport { OutputDirectory = outputDir };

            var msysObjects = db.Tables.FirstOrDefault(t => t.Name.Equals("MSysObjects", StringComparison.OrdinalIgnoreCase));
            var msysQueries = db.Tables.FirstOrDefault(t => t.Name.Equals("MSysQueries", StringComparison.OrdinalIgnoreCase));

            if (msysObjects == null || msysQueries == null)
            {
                Log.Warning("System catalog tables MSysObjects or MSysQueries not found. Cannot extract queries.");
                return report;
            }

            // Map ObjectId -> Query Name (Type = 5 is Query in Access)
            var queryNames = new Dictionary<int, string>();
            foreach (var row in msysObjects.Rows)
            {
                if (row.TryGetValue("Type", out var typeVal) && Convert.ToInt32(typeVal) == 5)
                {
                    if (row.TryGetValue("Id", out var idVal) && row.TryGetValue("Name", out var nameVal))
                    {
                        queryNames[Convert.ToInt32(idVal)] = nameVal?.ToString() ?? "UnknownQuery";
                    }
                }
            }

            // Group MSysQueries rows by ObjectId
            var queryParts = new Dictionary<int, List<Dictionary<string, object?>>>();
            foreach (var row in msysQueries.Rows)
            {
                if (row.TryGetValue("ObjectId", out var objIdVal) && objIdVal != null)
                {
                    int objId = Convert.ToInt32(objIdVal);
                    if (!queryParts.ContainsKey(objId))
                    {
                        queryParts[objId] = new List<Dictionary<string, object?>>();
                    }
                    queryParts[objId].Add(row);
                }
            }

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            foreach (var kvp in queryParts)
            {
                int objId = kvp.Key;
                string queryName = queryNames.ContainsKey(objId) ? queryNames[objId] : $"Query_{objId}";
                
                // Exclude system queries that start with ~
                if (queryName.StartsWith("~")) continue;

                string sql = ReconstructSql(kvp.Value);

                if (!string.IsNullOrWhiteSpace(sql))
                {
                    string safeName = string.Join("_", queryName.Split(Path.GetInvalidFileNameChars()));
                    string filePath = Path.Combine(outputDir, $"{safeName}.sql");

                    var sb = new StringBuilder();
                    sb.AppendLine($"-- Query: {queryName}");
                    sb.AppendLine($"-- ObjectId: {objId}");
                    sb.AppendLine();
                    sb.AppendLine(sql);

                    File.WriteAllText(filePath, sb.ToString());

                    report.Queries.Add(new ExtractedQuery
                    {
                        Name = queryName,
                        ObjectId = objId,
                        SqlText = sql
                    });

                    Log.Debug("Extracted query {QueryName} to {FilePath}", queryName, filePath);
                }
            }

            return report;
        }

        private static string ReconstructSql(List<Dictionary<string, object?>> rows)
        {
            // Attributes:
            // 1 = SELECT (Expression or Name1)
            // 2 = FROM (Name1 is table, Expression might be JOIN)
            // 3 = WHERE (Expression)
            // 4 = GROUP BY (Expression)
            // 5 = HAVING (Expression)
            // 6 = ORDER BY (Expression, Name1 = DESC?)

            var selects = new List<string>();
            var froms = new List<string>();
            var wheres = new List<string>();
            var groupBys = new List<string>();
            var havings = new List<string>();
            var orderBys = new List<string>();

            // Access sorts by Order field
            var sortedRows = rows
                .OrderBy(r => r.TryGetValue("Order", out var ord) ? Convert.ToInt32(ord ?? 0) : 0)
                .ToList();

            foreach (var row in sortedRows)
            {
                int attribute = row.TryGetValue("Attribute", out var attr) ? Convert.ToInt32(attr ?? 0) : 0;
                string expr = row.TryGetValue("Expression", out var e) ? e?.ToString() ?? "" : "";
                string name1 = row.TryGetValue("Name1", out var n1) ? n1?.ToString() ?? "" : "";
                string name2 = row.TryGetValue("Name2", out var n2) ? n2?.ToString() ?? "" : "";

                switch (attribute)
                {
                    case 1: // SELECT
                        string sel = !string.IsNullOrEmpty(expr) ? expr : name1;
                        if (!string.IsNullOrEmpty(name2) && !sel.Contains(name2)) sel += $" AS {name2}";
                        if (!string.IsNullOrEmpty(sel)) selects.Add(sel);
                        break;
                    case 2: // FROM
                        string from = !string.IsNullOrEmpty(expr) ? expr : name1;
                        if (!string.IsNullOrEmpty(name2)) from += $" AS {name2}";
                        if (!string.IsNullOrEmpty(from)) froms.Add(from);
                        break;
                    case 3: // WHERE
                        if (!string.IsNullOrEmpty(expr)) wheres.Add(expr);
                        break;
                    case 4: // GROUP BY
                        string gb = !string.IsNullOrEmpty(expr) ? expr : name1;
                        if (!string.IsNullOrEmpty(gb)) groupBys.Add(gb);
                        break;
                    case 5: // HAVING
                        if (!string.IsNullOrEmpty(expr)) havings.Add(expr);
                        break;
                    case 6: // ORDER BY
                        string ob = !string.IsNullOrEmpty(expr) ? expr : name1;
                        if (name2.Equals("D", StringComparison.OrdinalIgnoreCase)) ob += " DESC";
                        if (!string.IsNullOrEmpty(ob)) orderBys.Add(ob);
                        break;
                }
            }

            var sb = new StringBuilder();
            
            if (selects.Count > 0)
                sb.AppendLine("SELECT " + string.Join(", ", selects));
            else
                sb.AppendLine("SELECT *");

            if (froms.Count > 0)
                sb.AppendLine("FROM " + string.Join(", ", froms));

            if (wheres.Count > 0)
                sb.AppendLine("WHERE " + string.Join(" AND ", wheres));

            if (groupBys.Count > 0)
                sb.AppendLine("GROUP BY " + string.Join(", ", groupBys));

            if (havings.Count > 0)
                sb.AppendLine("HAVING " + string.Join(" AND ", havings));

            if (orderBys.Count > 0)
                sb.AppendLine("ORDER BY " + string.Join(", ", orderBys));

            if (sb.Length > 0) sb.Append(";");

            return sb.ToString().Trim();
        }
    }
}
