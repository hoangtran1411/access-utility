using System;
using System.IO;
using AccessUtility.Models;

namespace AccessUtility.Engine
{
    public static class TuiEngine
    {
        public static void RunInteractiveTui()
        {
            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("""
========================================================================
 🛠️ ACCESSUTILITY - RICH TERMINAL USER INTERFACE (TUI)
 Modern .NET 10 Native AOT Engine for Access 97 (.mdb) Maintenance
========================================================================
""");
                Console.ResetColor();

                Console.WriteLine("""
Select a Maintenance Task:

  [1] 🔍 Lock File (.ldb) Inspector & Active User Tracker
  [2] 🩺 Database Health & Fragmentation Diagnostics
  [3] 🧹 Compact Database (Defragment & Shrink Size)
  [4] 🚑 Repair Corrupted Database (Deep Page Recovery)
  [5] 📦 Export Database (SQLite / SQL Scripts / CSV)
  [6] 🤖 AX AI Natural Language Maintenance Assistant
  [7] 🌐 Launch Web Dashboard UI (Browser)
  [0] 🚪 Exit Application
""");

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Enter Option [0-7]: ");
                Console.ResetColor();

                string? choice = Console.ReadLine()?.Trim();
                if (choice == "0") break;

                switch (choice)
                {
                    case "1":
                        RunTaskWithPrompt(file =>
                        {
                            var info = LdbLockInspector.Inspect(file);
                            RenderLockInfoTui(info);
                        });
                        break;

                    case "2":
                        RunTaskWithPrompt(file =>
                        {
                            var db = Jet3BinaryReader.ReadDatabase(file, out var report);
                            RenderDiagnosticsTui(report);
                        });
                        break;

                    case "3":
                        RunTaskWithPrompt(file =>
                        {
                            string target = Path.ChangeExtension(file, ".compacted.mdb");
                            RenderHeader("COMPACT DATABASE");
                            var res = Jet3Compactor.Compact(file, target, forceUnlock: true);
                            RenderStatus(res.Success, res.Message);
                        });
                        break;

                    case "4":
                        RunTaskWithPrompt(file =>
                        {
                            string target = Path.ChangeExtension(file, ".repaired.mdb");
                            RenderHeader("REPAIR DATABASE");
                            var res = Jet3Repairer.Repair(file, target, forceUnlock: true);
                            RenderStatus(res.Success, res.Message);
                        });
                        break;

                    case "5":
                        RunTaskWithPrompt(file =>
                        {
                            RenderHeader("EXPORT DATABASE");
                            var db = Jet3BinaryReader.ReadDatabase(file, out _);
                            string outPath = Exporters.SqliteExporter.ExportDatabase(db, Path.ChangeExtension(file, ".sqlite"));
                            RenderStatus(true, $"Database successfully exported to SQLite: {outPath}");
                        });
                        break;

                    case "6":
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("\n🤖 AX (AI Experiment) REPL Mode. Enter natural language instructions (e.g. 'compact sample97.mdb and clean locks'):");
                        Console.ResetColor();
                        Console.Write("\nAX Query > ");
                        string? q = Console.ReadLine();
                        if (!string.IsNullOrWhiteSpace(q))
                        {
                            var plan = AxAssistant.InterpretQuery(q);
                            AxAssistant.ExecutePlan(plan);
                        }
                        Pause();
                        break;

                    case "7":
                        Web.WebServer.StartServer(5000);
                        break;
                }
            }
        }

        private static void RunTaskWithPrompt(Action<string> action)
        {
            Console.Write("\nEnter path to Access 97 .mdb file: ");
            string? path = Console.ReadLine()?.Trim('"');

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                RenderStatus(false, "File path is invalid or file does not exist.");
                Pause();
                return;
            }

            action(path);
            Pause();
        }

        private static void RenderHeader(string title)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"\n--- {title} ---");
            Console.ResetColor();
        }

        private static void RenderStatus(bool success, string message)
        {
            if (success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n[SUCCESS] {message}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[FAILED] {message}");
            }
            Console.ResetColor();
        }

        private static void RenderLockInfoTui(LockFileInfo info)
        {
            RenderHeader("LOCK FILE (.LDB) INSPECTOR");
            Console.WriteLine($"LDB Path        : {info.LdbPath}");
            Console.WriteLine($"Lock File Exists: {info.Exists}");
            Console.WriteLine($"Active Lock     : {info.IsFileInUse}");
            Console.WriteLine($"Orphan Lock     : {info.IsOrphanLock}");

            if (info.ConnectedUsers.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n+----+--------------------------------+--------------------------------+");
                Console.WriteLine("| #  | Computer Name                  | Connected User                 |");
                Console.WriteLine("+----+--------------------------------+--------------------------------+");
                foreach (var u in info.ConnectedUsers)
                {
                    Console.WriteLine($"| {u.EntryIndex,-2} | {u.ComputerName,-30} | {u.UserName,-30} |");
                }
                Console.WriteLine("+----+--------------------------------+--------------------------------+");
                Console.ResetColor();
            }
        }

        private static void RenderDiagnosticsTui(DiagnosticReport report)
        {
            RenderHeader("DATABASE HEALTH DIAGNOSTICS");
            Console.WriteLine($"File Size       : {report.FileSizeBytes:N0} bytes ({report.FileSizeBytes / (1024.0 * 1024.0):F2} MB)");
            Console.WriteLine($"Total 2KB Pages : {report.TotalPages}");
            Console.WriteLine($"TDEF Pages Count: {report.TdefPagesCount}");
            Console.WriteLine($"Data Pages Count: {report.DataPagesCount}");
            Console.WriteLine($"Slack Pages     : {report.FreeSlackPagesCount}");

            Console.Write("Fragmentation   : ");
            if (report.FragmentationPercentage > 15) Console.ForegroundColor = ConsoleColor.Red;
            else Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{report.FragmentationPercentage}%");
            Console.ResetColor();

            Console.WriteLine($"Corrupt Pages   : {report.CorruptPagesCount}");
            RenderStatus(report.CorruptPagesCount == 0, report.StatusSummary);
        }

        private static void Pause()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("\nPress Enter to return to menu...");
            Console.ResetColor();
            Console.ReadLine();
        }
    }
}
