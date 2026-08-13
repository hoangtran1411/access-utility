using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AccessUtility.Models;

namespace AccessUtility.Engine
{
    public class AxExecutionPlan
    {
        public string RawQuery { get; set; } = string.Empty;
        public string TargetFile { get; set; } = string.Empty;
        public List<string> ActionSteps { get; set; } = new();
        public string ExportFormat { get; set; } = "sqlite";
        public bool ForceUnlock { get; set; } = true;
    }

    public static class AxAssistant
    {
        public static AxExecutionPlan InterpretQuery(string query)
        {
            var plan = new AxExecutionPlan { RawQuery = query };
            string lower = query.ToLower();

            // Extract file path if present (e.g. ends with .mdb or matches path)
            var match = Regex.Match(query, @"([a-zA-Z]:\\[^\s]+\.mdb|[^\s]+\.mdb)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                plan.TargetFile = match.Value.Trim('"');
            }

            // Identify requested maintenance actions
            if (lower.Contains("lock") || lower.Contains("user") || lower.Contains("connected"))
            {
                plan.ActionSteps.Add("lockstat");
            }
            if (lower.Contains("clean") || lower.Contains("unlock") || lower.Contains("orphan"))
            {
                plan.ActionSteps.Add("clean-lock");
                plan.ForceUnlock = true;
            }
            if (lower.Contains("diag") || lower.Contains("health") || lower.Contains("frag") || lower.Contains("check"))
            {
                plan.ActionSteps.Add("diagnose");
            }
            if (lower.Contains("compact") || lower.Contains("defrag") || lower.Contains("shrink") || lower.Contains("reduce"))
            {
                plan.ActionSteps.Add("compact");
            }
            if (lower.Contains("repair") || lower.Contains("fix") || lower.Contains("recover") || lower.Contains("corrupt"))
            {
                plan.ActionSteps.Add("repair");
            }
            if (lower.Contains("export") || lower.Contains("convert") || lower.Contains("sqlite") || lower.Contains("csv") || lower.Contains("sql"))
            {
                plan.ActionSteps.Add("export");
                if (lower.Contains("csv")) plan.ExportFormat = "csv";
                else if (lower.Contains("sql")) plan.ExportFormat = "sql";
                else plan.ExportFormat = "sqlite";
            }

            // Default fallback if no action detected
            if (plan.ActionSteps.Count == 0)
            {
                plan.ActionSteps.Add("diagnose");
                plan.ActionSteps.Add("compact");
            }

            return plan;
        }

        public static void ExecutePlan(AxExecutionPlan plan)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n🤖 [AX AI Assistant] Interpreted Execution Plan for Query:");
            Console.WriteLine($"   \"{plan.RawQuery}\"");
            Console.ResetColor();

            if (string.IsNullOrWhiteSpace(plan.TargetFile) || !File.Exists(plan.TargetFile))
            {
                Console.Write("\nTarget .mdb file not specified or found. Enter file path: ");
                string? input = Console.ReadLine()?.Trim('"');
                if (string.IsNullOrWhiteSpace(input) || !File.Exists(input))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Valid Access 97 .mdb file required to run AX plan.");
                    Console.ResetColor();
                    return;
                }
                plan.TargetFile = input;
            }

            Console.WriteLine($"\n📋 Planned Maintenance Chain ({plan.ActionSteps.Count} steps):");
            for (int i = 0; i < plan.ActionSteps.Count; i++)
            {
                Console.WriteLine($"   Step {i + 1}: {plan.ActionSteps[i].ToUpper()}");
            }

            Console.WriteLine("\nExecuting planned steps...");

            foreach (var step in plan.ActionSteps)
            {
                switch (step)
                {
                    case "lockstat":
                        var lockInfo = LdbLockInspector.Inspect(plan.TargetFile);
                        Console.WriteLine($"   [AX] Lock Inspector: Found {lockInfo.ConnectedUsers.Count} connected users. Active lock: {lockInfo.IsFileInUse}");
                        break;

                    case "clean-lock":
                        LdbLockInspector.TryCleanOrphanLock(plan.TargetFile, out string cleanMsg);
                        Console.WriteLine($"   [AX] Lock Cleaner: {cleanMsg}");
                        break;

                    case "diagnose":
                        Jet3BinaryReader.ReadDatabase(plan.TargetFile, out var diag);
                        Console.WriteLine($"   [AX] Diagnostics: {diag.StatusSummary}");
                        break;

                    case "compact":
                        string compactTarget = Path.ChangeExtension(plan.TargetFile, ".compacted.mdb");
                        var compRes = Jet3Compactor.Compact(plan.TargetFile, compactTarget, plan.ForceUnlock);
                        Console.WriteLine($"   [AX] Compactor: {compRes.Message}");
                        break;

                    case "repair":
                        string repairTarget = Path.ChangeExtension(plan.TargetFile, ".repaired.mdb");
                        var repRes = Jet3Repairer.Repair(plan.TargetFile, repairTarget, plan.ForceUnlock);
                        Console.WriteLine($"   [AX] Repairer: {repRes.Message}");
                        break;

                    case "export":
                        var db = Jet3BinaryReader.ReadDatabase(plan.TargetFile, out _);
                        string outPath = Exporters.SqliteExporter.ExportDatabase(db, Path.ChangeExtension(plan.TargetFile, "." + plan.ExportFormat));
                        Console.WriteLine($"   [AX] Exporter: Exported database to {outPath}");
                        break;
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n[AX AI Assistant] Maintenance Execution Completed Successfully! ✨");
            Console.ResetColor();
        }
    }
}
