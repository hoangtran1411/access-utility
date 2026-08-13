using System;
using System.IO;
using AccessUtility.Engine;
using AccessUtility.Exporters;
using AccessUtility.Web;

namespace AccessUtility
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.Title = "Access 97 Utility - Compact, Repair & Lock Inspector (.NET 10 Native AOT)";

            if (args.Length == 0)
            {
                RunInteractiveMenu();
                return;
            }

            string command = args[0].ToLower();

            switch (command)
            {
                case "lockstat":
                    if (args.Length < 2) { PrintUsage(); return; }
                    RunLockStatCommand(args[1]);
                    break;

                case "diagnose":
                    if (args.Length < 2) { PrintUsage(); return; }
                    RunDiagnoseCommand(args[1]);
                    break;

                case "compact":
                    if (args.Length < 2) { PrintUsage(); return; }
                    string compactTarget = GetArgValue(args, "--output") ?? Path.ChangeExtension(args[1], ".compacted.mdb");
                    bool forceCompact = HasFlag(args, "--force-unlock");
                    RunCompactCommand(args[1], compactTarget, forceCompact);
                    break;

                case "repair":
                    if (args.Length < 2) { PrintUsage(); return; }
                    string repairTarget = GetArgValue(args, "--output") ?? Path.ChangeExtension(args[1], ".repaired.mdb");
                    bool forceRepair = HasFlag(args, "--force-unlock");
                    RunRepairCommand(args[1], repairTarget, forceRepair);
                    break;

                case "export":
                    if (args.Length < 2) { PrintUsage(); return; }
                    string fmt = GetArgValue(args, "--format") ?? "sqlite";
                    RunExportCommand(args[1], fmt);
                    break;

                case "test":
                    string testFile = args.Length >= 2 ? args[1] : "sample97.mdb";
                    Tests.TestRunner.CreateSampleDatabase(testFile);
                    Tests.TestRunner.CreateSampleLockFile(testFile);
                    Console.WriteLine($"[TEST SETUP] Created sample Access 97 database and .ldb file: {testFile}");
                    RunLockStatCommand(testFile, promptCleanup: false);
                    RunDiagnoseCommand(testFile);
                    RunCompactCommand(testFile, Path.ChangeExtension(testFile, ".compacted.mdb"), true);
                    RunRepairCommand(testFile, Path.ChangeExtension(testFile, ".repaired.mdb"), true);
                    RunExportCommand(testFile, "sqlite");
                    break;

                case "web":
                    int port = 5000;
                    string? portStr = GetArgValue(args, "--port");
                    if (int.TryParse(portStr, out int p)) port = p;
                    WebServer.StartServer(port);
                    break;

                default:
                    Console.WriteLine($"Unknown command '{command}'.");
                    PrintUsage();
                    break;
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("""

========================================================================
 MS Access 97 (.mdb) Utility - Native AOT Engine
 Focus: Compact, Repair & Lock File (.ldb) Inspector
========================================================================

Usage:
  AccessUtility.exe <command> <file.mdb> [options]

Commands:
  lockstat <file.mdb>                            Inspect .ldb lock file & active user connections
  diagnose <file.mdb>                            Run health & page fragmentation diagnostics
  compact  <file.mdb> [--output target] [--force-unlock]  Defragment & minimize .mdb file size
  repair   <file.mdb> [--output target] [--force-unlock]  Deep sector repair & data recovery
  export   <file.mdb> [--format sqlite|sql|csv]  Export database to SQLite, SQL, or CSV
  web      [--port 5000]                         Launch Web Dashboard UI in browser

Examples:
  AccessUtility.exe lockstat C:\DB\Data97.mdb
  AccessUtility.exe compact C:\DB\Data97.mdb --output C:\DB\Data97_Clean.mdb
  AccessUtility.exe repair C:\DB\Corrupt97.mdb --force-unlock
  AccessUtility.exe web --port 5000
""");
        }

        private static void RunLockStatCommand(string mdbPath, bool promptCleanup = true)
        {
            Console.WriteLine($"\n[+] Inspecting Lock File (.ldb) for: {mdbPath}");
            var lockInfo = LdbLockInspector.Inspect(mdbPath);

            Console.WriteLine($"  .ldb File Exists   : {lockInfo.Exists}");
            Console.WriteLine($"  File Actively Locked: {lockInfo.IsFileInUse}");
            Console.WriteLine($"  Orphan Lock Detected: {lockInfo.IsOrphanLock}");
            Console.WriteLine($"  Connected Users Count: {lockInfo.ConnectedUsers.Count}");

            if (lockInfo.ConnectedUsers.Count > 0)
            {
                Console.WriteLine("\n--- Connected Users & Computer Names ---");
                foreach (var user in lockInfo.ConnectedUsers)
                {
                    Console.WriteLine($"  #{user.EntryIndex} | Computer: {user.ComputerName} | User: {user.UserName}");
                }
            }

            if (lockInfo.IsOrphanLock && promptCleanup)
            {
                Console.WriteLine("\n[!] Orphan .ldb lock file detected from previous crash.");
                Console.Write("    Attempt automatic lock cleanup? (Y/N): ");
                string? input = Console.ReadLine();
                if (input?.Trim().ToUpper() == "Y")
                {
                    LdbLockInspector.TryCleanOrphanLock(mdbPath, out string msg);
                    Console.WriteLine($"    {msg}");
                }
            }
        }

        private static void RunDiagnoseCommand(string mdbPath)
        {
            Console.WriteLine($"\n[+] Running Database Health Diagnostics for: {mdbPath}");
            var db = Jet3BinaryReader.ReadDatabase(mdbPath, out var diag);

            Console.WriteLine($"  File Size       : {diag.FileSizeBytes:N0} bytes ({diag.FileSizeBytes / (1024.0 * 1024.0):F2} MB)");
            Console.WriteLine($"  Total 2KB Pages : {diag.TotalPages}");
            Console.WriteLine($"  TDEF Pages Count: {diag.TdefPagesCount}");
            Console.WriteLine($"  Data Pages Count: {diag.DataPagesCount}");
            Console.WriteLine($"  Slack Pages     : {diag.FreeSlackPagesCount}");
            Console.WriteLine($"  Fragmentation   : {diag.FragmentationPercentage}%");
            Console.WriteLine($"  Corrupt Pages   : {diag.CorruptPagesCount}");
            Console.WriteLine($"\nSummary: {diag.StatusSummary}");

            if (diag.TableSummaries.Count > 0)
            {
                Console.WriteLine("\n--- Table Summary ---");
                foreach (var table in diag.TableSummaries)
                {
                    Console.WriteLine($"  * {table}");
                }
            }
        }

        private static void RunCompactCommand(string srcPath, string targetPath, bool force)
        {
            Console.WriteLine($"\n[+] Compacting Access 97 Database: {srcPath}");
            Console.WriteLine($"    Target Output: {targetPath}");

            var res = Jet3Compactor.Compact(srcPath, targetPath, force);

            if (res.Success)
            {
                Console.WriteLine($"\n[SUCCESS] {res.Message}");
                Console.WriteLine($"  Original Size : {res.OriginalSizeBytes:N0} bytes");
                Console.WriteLine($"  Compacted Size: {res.CompactedSizeBytes:N0} bytes");
                Console.WriteLine($"  Space Saved   : {res.SpaceSavedBytes:N0} bytes ({res.ReductionPercentage}%)");
                Console.WriteLine($"  Tables        : {res.TotalTablesCompacted}, Total Rows: {res.TotalRowsPreserved}");
            }
            else
            {
                Console.WriteLine($"\n[FAILED] {res.Message}");
            }
        }

        private static void RunRepairCommand(string srcPath, string targetPath, bool force)
        {
            Console.WriteLine($"\n[+] Repairing Access 97 Database: {srcPath}");
            Console.WriteLine($"    Target Output: {targetPath}");

            var res = Jet3Repairer.Repair(srcPath, targetPath, force);

            if (res.Success)
            {
                Console.WriteLine($"\n[SUCCESS] {res.Message}");
                Console.WriteLine($"  Pages Scanned  : {res.TotalPagesScanned}");
                Console.WriteLine($"  Corrupt Pages  : {res.CorruptPagesIsolated}");
                Console.WriteLine($"  Recovered Tables: {res.TotalTablesRecovered}");
                Console.WriteLine($"  Salvaged Rows   : {res.TotalRowsSalvaged}");
            }
            else
            {
                Console.WriteLine($"\n[FAILED] {res.Message}");
            }
        }

        private static void RunExportCommand(string mdbPath, string format)
        {
            Console.WriteLine($"\n[+] Exporting Access 97 Database: {mdbPath} to {format.ToUpper()}");
            var db = Jet3BinaryReader.ReadDatabase(mdbPath, out _);

            string outPath = format.ToLower() switch
            {
                "csv" => CsvExporter.ExportTable(db.Tables.Count > 0 ? db.Tables[0] : new Models.AccessTable(), Path.ChangeExtension(mdbPath, ".csv")),
                "sql" => SqlScriptExporter.ExportDatabase(db, Path.ChangeExtension(mdbPath, ".sql")),
                _ => SqliteExporter.ExportDatabase(db, Path.ChangeExtension(mdbPath, ".sqlite"))
            };

            Console.WriteLine($"[SUCCESS] Exported successfully to: {outPath}");
        }

        private static void RunInteractiveMenu()
        {
            PrintUsage();
            Console.Write("\nSelect action (1=LockStat, 2=Diagnose, 3=Compact, 4=Repair, 5=Web Dashboard, 0=Exit): ");
            string? choice = Console.ReadLine();

            if (choice == "5")
            {
                WebServer.StartServer(5000);
            }
            else if (choice is "1" or "2" or "3" or "4")
            {
                Console.Write("Enter path to Access 97 .mdb file: ");
                string? file = Console.ReadLine()?.Trim('"');

                if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
                {
                    Console.WriteLine("File not found.");
                    return;
                }

                if (choice == "1") RunLockStatCommand(file);
                else if (choice == "2") RunDiagnoseCommand(file);
                else if (choice == "3") RunCompactCommand(file, Path.ChangeExtension(file, ".compacted.mdb"), false);
                else if (choice == "4") RunRepairCommand(file, Path.ChangeExtension(file, ".repaired.mdb"), false);
            }
        }

        private static string? GetArgValue(string[] args, string flag)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].Equals(flag, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            }
            return null;
        }

        private static bool HasFlag(string[] args, string flag)
        {
            foreach (var arg in args)
            {
                if (arg.Equals(flag, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }
}
