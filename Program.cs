using System;
using System.IO;
using AccessUtility.Engine;
using AccessUtility.Exporters;
using AccessUtility.Web;
using Serilog;

namespace AccessUtility
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            AppLogger.Initialize("access_utility_logs.sqlite");
            
            try
            {
                Console.Title = "Access 97 Utility - AX AI Engine & Cobra CLI (.NET 10 Native AOT)";

                if (args.Length == 0)
                {
                    TuiEngine.RunInteractiveTui();
                    return;
                }

            string command = args[0].ToLower();

            if (command is "--help" or "-h" or "help")
            {
                CommandRegistry.PrintCobraHelp();
                return;
            }

            switch (command)
            {
                case "lockstat" or "ls" or "locks":
                    if (args.Length < 2) { CommandRegistry.PrintCobraHelp(); return; }
                    RunLockStatCommand(args[1]);
                    break;

                case "diagnose" or "diag" or "health":
                    if (args.Length < 2) { CommandRegistry.PrintCobraHelp(); return; }
                    bool forensicScan = HasFlag(args, "--forensic-scan");
                    RunDiagnoseCommand(args[1], forensicScan);
                    break;

                case "compact" or "cmp" or "defrag":
                    if (args.Length < 2) { CommandRegistry.PrintCobraHelp(); return; }
                    string compactTarget = GetArgValue(args, "--output") ?? Path.ChangeExtension(args[1], ".compacted.mdb");
                    bool forceCompact = HasFlag(args, "--force-unlock");
                    RunCompactCommand(args[1], compactTarget, forceCompact);
                    break;

                case "repair" or "rep" or "recover":
                    if (args.Length < 2) { CommandRegistry.PrintCobraHelp(); return; }
                    string repairTarget = GetArgValue(args, "--output") ?? Path.ChangeExtension(args[1], ".repaired.mdb");
                    bool forceRepair = HasFlag(args, "--force-unlock");
                    bool carveDeleted = HasFlag(args, "--carve-deleted");
                    RunRepairCommand(args[1], repairTarget, forceRepair, carveDeleted);
                    break;

                case "carve" or "salvage" or "forensic":
                    if (args.Length < 2) { CommandRegistry.PrintCobraHelp(); return; }
                    string? carveTable = GetArgValue(args, "--table");
                    string? carveOut = GetArgValue(args, "--output");
                    RunCarveCommand(args[1], carveTable, carveOut);
                    break;

                case "export" or "exp" or "convert":
                    if (args.Length < 2) { CommandRegistry.PrintCobraHelp(); return; }
                    string fmt = GetArgValue(args, "--format") ?? "sqlite";
                    string? outTarget = GetArgValue(args, "--output");
                    string? dialect = GetArgValue(args, "--dialect");
                    bool schemaOnly = HasFlag(args, "--schema-only");
                    bool dataOnly = HasFlag(args, "--data-only");
                    int batchSize = int.TryParse(GetArgValue(args, "--batch-size"), out int bs) ? bs : 250;
                    RunExportCommand(args[1], fmt, outTarget, dialect, schemaOnly, dataOnly, batchSize);
                    break;

                case "schema" or "ddl":
                    if (args.Length < 2) { CommandRegistry.PrintCobraHelp(); return; }
                    string? schemaDialect = GetArgValue(args, "--dialect");
                    string schemaTarget = GetArgValue(args, "--output") ?? Path.ChangeExtension(args[1], ".schema.sql");
                    RunSchemaCommand(args[1], schemaDialect, schemaTarget);
                    break;

                case "password" or "pw" or "security":
                    if (args.Length < 2) { CommandRegistry.PrintCobraHelp(); return; }
                    string? mdwPath = GetArgValue(args, "--workgroup");
                    RunPasswordCommand(args[1], mdwPath);
                    break;

                case "diff" or "compare":
                    if (args.Length < 2) { CommandRegistry.PrintCobraHelp(); return; }
                    string diffFmt = GetArgValue(args, "--format") ?? "sql";
                    if (diffFmt.Equals("erd", StringComparison.OrdinalIgnoreCase))
                    {
                        string erdOut = GetArgValue(args, "--output") ?? Path.ChangeExtension(args[1], "_erd.md");
                        RunErdCommand(args[1], erdOut);
                        break;
                    }
                    if (args.Length < 3) { CommandRegistry.PrintCobraHelp(); return; }
                    string diffOut = GetArgValue(args, "--output") ?? Path.ChangeExtension(args[1], ".diff.sql");
                    string diffDialect = GetArgValue(args, "--dialect") ?? "ansi";
                    RunDiffCommand(args[1], args[2], diffOut, diffDialect);
                    break;

                case "erd" or "diagram":
                    if (args.Length < 2) { CommandRegistry.PrintCobraHelp(); return; }
                    string erdTarget = GetArgValue(args, "--output") ?? Path.ChangeExtension(args[1], "_erd.md");
                    RunErdCommand(args[1], erdTarget);
                    break;

                case "hex":
                    if (args.Length < 2) { CommandRegistry.PrintCobraHelp(); return; }
                    string pageStr = GetArgValue(args, "--page") ?? "0";
                    int page = int.TryParse(pageStr, out int hexPage) ? hexPage : 0;
                    RunHexCommand(args[1], page);
                    break;

                case "extract-ole" or "extract":
                    if (args.Length < 2) { CommandRegistry.PrintCobraHelp(); return; }
                    string oleOut = GetArgValue(args, "--output") ?? "./extracted_ole";
                    RunExtractOleCommand(args[1], oleOut);
                    break;

                case "extract-queries" or "queries":
                    if (args.Length < 2) { CommandRegistry.PrintCobraHelp(); return; }
                    string queriesOut = GetArgValue(args, "--output") ?? "./extracted_queries";
                    RunExtractQueriesCommand(args[1], queriesOut);
                    break;

                case "daemon":
                    if (args.Length < 2) { CommandRegistry.PrintCobraHelp(); return; }
                    string backupDir = GetArgValue(args, "--backup-dir") ?? "./backups";
                    string intervalStr = GetArgValue(args, "--interval") ?? "24h";
                    TimeSpan interval = TimeSpan.FromHours(24);
                    if (intervalStr.EndsWith("h") && double.TryParse(intervalStr.TrimEnd('h'), out double h)) interval = TimeSpan.FromHours(h);
                    if (intervalStr.EndsWith("m") && double.TryParse(intervalStr.TrimEnd('m'), out double m)) interval = TimeSpan.FromMinutes(m);
                    RunDaemonCommand(args[1], interval, backupDir);
                    break;

                case "update":
                    AutoUpdater.CheckAndUpdateAsync().GetAwaiter().GetResult();
                    break;

                case "logs":
                    string logDbPath = GetArgValue(args, "--db") ?? "app_logs.sqlite";
                    string tailStr = GetArgValue(args, "--tail") ?? "50";
                    string levelFilter = GetArgValue(args, "--level") ?? string.Empty;
                    int tail = 50;
                    if (int.TryParse(tailStr, out int t)) tail = t;
                    LogViewer.ShowLogs(logDbPath, tail, levelFilter);
                    break;

                case "ax" or "ai" or "ask":
                    string query = args.Length >= 2 ? string.Join(" ", args[1..]) : "";
                    if (string.IsNullOrWhiteSpace(query))
                    {
                        Console.Write("AX Query > ");
                        query = Console.ReadLine() ?? "";
                    }
                    var plan = AxAssistant.InterpretQuery(query);
                    AxAssistant.ExecutePlan(plan);
                    break;

                case "tui":
                    TuiEngine.RunInteractiveTui();
                    break;

                case "web" or "ui" or "dashboard":
                    int port = 5000;
                    string? portStr = GetArgValue(args, "--port");
                    if (int.TryParse(portStr, out int p)) port = p;
                    WebServer.StartServer(port);
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

                default:
                    Console.WriteLine($"Unknown command '{command}'.");
                    CommandRegistry.PrintCobraHelp();
                    break;
            }
            }
            finally
            {
                AppLogger.CloseAndFlush();
            }
        }

        private static void RunLockStatCommand(string mdbPath, bool promptCleanup = true)
        {
            Log.Information("Executing LockStatCommand on {Path}", mdbPath);
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

        private static void RunDiagnoseCommand(string mdbPath, bool forensicScan = false)
        {
            Log.Information("Executing DiagnoseCommand on {Path} (ForensicScan={ForensicScan})", mdbPath, forensicScan);
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

            if (forensicScan)
            {
                Console.WriteLine("\n[+] Executing Deep Forensic Slack Space Analysis...");
                var carveReport = ForensicCarver.CarveDatabase(mdbPath);
                Console.WriteLine($"\n--- Forensic Carve Scan Results ---");
                Console.WriteLine($"  Pages Scanned         : {carveReport.TotalPagesScanned}");
                Console.WriteLine($"  Active Rows Detected  : {carveReport.ActiveRowsCount}");
                Console.WriteLine($"  Deleted Rows Salvaged : {carveReport.SalvagedDeletedRowsCount}");
                Console.WriteLine($"  High Confidence (>80%): {carveReport.HighConfidenceCount}");
                Console.WriteLine($"  Medium Confidence     : {carveReport.MediumConfidenceCount}");
                Console.WriteLine($"  Low Confidence        : {carveReport.LowConfidenceCount}");

                if (carveReport.TableSummaries.Count > 0)
                {
                    Console.WriteLine("\n--- Salvaged Table Breakdown ---");
                    foreach (var ts in carveReport.TableSummaries)
                    {
                        Console.WriteLine($"  * Table '{ts.TableName}': {ts.DeletedRowsSalvaged} deleted records salvaged (Avg Confidence: {ts.AverageConfidence}%)");
                    }
                }
            }
        }

        private static void RunCompactCommand(string srcPath, string targetPath, bool force)
        {
            Log.Information("Executing CompactCommand on {SourcePath} to {TargetPath}", srcPath, targetPath);
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

        private static void RunRepairCommand(string srcPath, string targetPath, bool force, bool carveDeleted = false)
        {
            Log.Information("Executing RepairCommand on {SourcePath} to {TargetPath} (CarveDeleted={CarveDeleted})", srcPath, targetPath, carveDeleted);
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

                if (carveDeleted)
                {
                    Console.WriteLine("\n[+] Deep Forensic Carving Enabled: Scanning unallocated slack space for deleted records...");
                    var carveReport = ForensicCarver.CarveDatabase(srcPath);
                    string carvedSqlite = Path.ChangeExtension(targetPath, ".carved.sqlite");
                    ForensicCarver.ExportCarvedRecordsToSqlite(carveReport, carvedSqlite);

                    Console.WriteLine($"[SUCCESS] Salvaged {carveReport.SalvagedDeletedRowsCount} deleted records ({carveReport.HighConfidenceCount} high confidence).");
                    Console.WriteLine($"          Carved records database created at: {carvedSqlite}");
                }
            }
            else
            {
                Console.WriteLine($"\n[FAILED] {res.Message}");
            }
        }

        private static void RunCarveCommand(string mdbPath, string? tableName, string? outputPath)
        {
            Console.WriteLine($"\n[+] Running Forensic Record Carver on: {mdbPath}");
            if (!string.IsNullOrEmpty(tableName)) Console.WriteLine($"    Target Table: {tableName}");

            var carveReport = ForensicCarver.CarveDatabase(mdbPath, tableName);

            Console.WriteLine($"\n--- Forensic Carving Report ---");
            Console.WriteLine($"  Total Pages Scanned   : {carveReport.TotalPagesScanned}");
            Console.WriteLine($"  Active Rows Found     : {carveReport.ActiveRowsCount}");
            Console.WriteLine($"  Deleted Rows Salvaged : {carveReport.SalvagedDeletedRowsCount}");
            Console.WriteLine($"  High Confidence (>80%): {carveReport.HighConfidenceCount}");
            Console.WriteLine($"  Medium Confidence     : {carveReport.MediumConfidenceCount}");
            Console.WriteLine($"  Low Confidence        : {carveReport.LowConfidenceCount}");

            string target = outputPath ?? Path.ChangeExtension(mdbPath, ".carved.sqlite");
            if (target.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                ForensicCarver.ExportCarvedRecordsToJson(carveReport, target);
            }
            else
            {
                ForensicCarver.ExportCarvedRecordsToSqlite(carveReport, target);
            }

            Console.WriteLine($"\n[SUCCESS] Salvaged records exported to: {target}");
        }

        private static void RunPasswordCommand(string mdbPath, string? mdwPath)
        {
            Console.WriteLine($"\n[+] Inspecting Access 97 Database Security: {Path.GetFileName(mdbPath)}");
            Console.WriteLine($"    File: {mdbPath}\n");

            var sec = Engine.SecurityReader.InspectDatabase(mdbPath);

            if (!string.IsNullOrEmpty(sec.ErrorMessage))
            {
                Console.WriteLine($"[ERROR] {sec.ErrorMessage}");
                return;
            }

            Console.WriteLine($"  Jet Version        : {sec.JetVersion}");
            Console.WriteLine($"  Valid Jet Database : {sec.IsValidJetDatabase}");
            Console.WriteLine($"  Password Protected : {sec.IsPasswordProtected}");
            Console.WriteLine($"  Database Password  : {(sec.IsPasswordProtected ? $"\"{sec.DatabasePassword}\"" : "(none)" )}");
            Console.WriteLine($"  User-Level Security: {sec.HasUserLevelSecurity}");
            Console.WriteLine($"  Encrypted At Rest  : {sec.IsEncryptedAtRest}");
            Console.WriteLine($"  Owner SID          : {(string.IsNullOrEmpty(sec.DatabaseOwnerSid) ? "(not found)" : sec.DatabaseOwnerSid)}");

            // Optional workgroup inspection
            if (!string.IsNullOrEmpty(mdwPath))
            {
                Console.WriteLine($"\n[+] Parsing Workgroup File: {Path.GetFileName(mdwPath)}");
                var wg = Engine.SecurityReader.InspectWorkgroup(mdwPath);

                if (!string.IsNullOrEmpty(wg.ErrorMessage))
                {
                    Console.WriteLine($"[ERROR] {wg.ErrorMessage}");
                }
                else
                {
                    Console.WriteLine($"  Valid Workgroup    : {wg.IsValidWorkgroupFile}");
                    Console.WriteLine($"  Workgroup ID (WID) : {(string.IsNullOrEmpty(wg.WorkgroupId) ? "(not found)" : wg.WorkgroupId)}");
                    Console.WriteLine($"  Users Found        : {wg.Users.Count}");
                    Console.WriteLine($"  Groups Found       : {wg.Groups.Count}");

                    if (wg.Users.Count > 0)
                    {
                        Console.WriteLine("\n--- Workgroup Users ---");
                        foreach (var user in wg.Users)
                        {
                            string sid = string.IsNullOrEmpty(user.Sid) ? "(no SID)" : user.Sid;
                            Console.WriteLine($"  [{user.AccountType}] {user.AccountName,-20} SID: {sid}");
                        }
                    }

                    if (wg.Groups.Count > 0)
                    {
                        Console.WriteLine("\n--- Workgroup Groups ---");
                        foreach (var grp in wg.Groups)
                        {
                            Console.WriteLine($"  [Group] {grp.GroupName}");
                        }
                    }
                }
            }
        }

        private static void RunDiffCommand(string sourceMdb, string targetMdb, string outputPath, string dialect)
        {
            Log.Information("Executing DiffCommand on {SourcePath} and {TargetPath}, dialect {Dialect}", sourceMdb, targetMdb, dialect);
            Console.WriteLine($"\n[+] Comparing Schemas: {Path.GetFileName(sourceMdb)} vs {Path.GetFileName(targetMdb)}");

            var sourceDb = Jet3BinaryReader.ReadDatabase(sourceMdb, out var srcReport);
            if (srcReport.CorruptPagesCount > 0)
            {
                Console.WriteLine($"[WARNING] Source DB has {srcReport.CorruptPagesCount} corrupted pages.");
            }

            var targetDb = Jet3BinaryReader.ReadDatabase(targetMdb, out var tgtReport);
            if (tgtReport.CorruptPagesCount > 0)
            {
                Console.WriteLine($"[WARNING] Target DB has {tgtReport.CorruptPagesCount} corrupted pages.");
            }

            var diff = SchemaComparer.Compare(sourceDb, targetDb);

            // Print summary report to console
            string report = MigrationScriptExporter.GenerateDiffReport(diff);
            Console.WriteLine(report);

            if (diff.HasDifferences)
            {
                Console.WriteLine($"\n[+] Generating {dialect.ToUpper()} Migration Script: {outputPath}");
                MigrationScriptExporter.GenerateMigrationScript(diff, outputPath, dialect);
                Console.WriteLine($"[SUCCESS] Script generated successfully.");
            }
        }

        private static void RunExtractOleCommand(string mdbPath, string outputDir)
        {
            Log.Information("Executing ExtractOleCommand on {Path} to {OutputDir}", mdbPath, outputDir);
            Console.WriteLine($"\n[+] Extracting OLE Objects from: {mdbPath}");
            Console.WriteLine($"    Output Directory: {outputDir}");

            var db = Jet3BinaryReader.ReadDatabase(mdbPath, out var diag);
            if (diag.CorruptPagesCount > 0)
            {
                Console.WriteLine($"[WARNING] Database has {diag.CorruptPagesCount} corrupted pages. Extraction may be incomplete.");
            }

            var report = OleExtractor.ExtractDatabase(db, outputDir);

            if (report.ExtractedFiles.Count == 0)
            {
                Console.WriteLine("\n[INFO] No embedded OLE objects found (or none matched known signatures).");
            }
            else
            {
                Console.WriteLine($"\n[SUCCESS] Extracted {report.ExtractedFiles.Count} files.");
                foreach (var file in report.ExtractedFiles)
                {
                    Console.WriteLine($"  [{file.FileType.ToUpper()}] {file.TableName}.{file.ColumnName} -> {Path.GetFileName(file.FilePath)} ({file.SizeBytes} bytes)");
                }
            }
        }

        private static void RunExtractQueriesCommand(string mdbPath, string outputDir)
        {
            Log.Information("Executing ExtractQueriesCommand on {Path} to {OutputDir}", mdbPath, outputDir);
            Console.WriteLine($"\n[+] Extracting SQL Queries from: {mdbPath}");
            Console.WriteLine($"    Output Directory: {outputDir}");

            var db = Jet3BinaryReader.ReadDatabase(mdbPath, out var diag);
            if (diag.CorruptPagesCount > 0)
            {
                Console.WriteLine($"[WARNING] Database has {diag.CorruptPagesCount} corrupted pages. Extraction may be incomplete.");
            }

            var report = QueryExtractor.ExtractQueries(db, outputDir);

            if (report.Queries.Count == 0)
            {
                Console.WriteLine("\n[INFO] No saved queries found (or MSysQueries is missing).");
            }
            else
            {
                Console.WriteLine($"\n[SUCCESS] Extracted {report.Queries.Count} queries.");
                foreach (var q in report.Queries)
                {
                    Console.WriteLine($"  [SQL] {q.Name} (ObjectId: {q.ObjectId})");
                }
            }
        }

        private static void RunDaemonCommand(string mdbPath, TimeSpan interval, string backupDir)
        {
            using var cts = new System.Threading.CancellationTokenSource();
            
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            MaintenanceDaemon.RunDaemon(mdbPath, interval, backupDir, cts.Token);
        }

        private static void RunExportCommand(string mdbPath, string format, string? outputPath = null, string? dialect = null, bool schemaOnly = false, bool dataOnly = false, int batchSize = 250)
        {
            Console.WriteLine($"\n[+] Exporting Access 97 Database: {mdbPath} to {format.ToUpper()}");
            var db = Jet3BinaryReader.ReadDatabase(mdbPath, out var diag);
            if (diag.CorruptPagesCount > 0)
            {
                Console.WriteLine($"[WARNING] Database contains {diag.CorruptPagesCount} corrupted pages. Exported data may be partial.");
            }

            string target = outputPath ?? string.Empty;
            string outPath = format.ToLower() switch
            {
                "parquet" or "pq" => ParquetExporter.ExportDatabase(db, string.IsNullOrEmpty(target) ? Path.ChangeExtension(mdbPath, ".parquet") : target),
                "duckdb" or "duck" => DuckDbExporter.ExportDatabase(db, string.IsNullOrEmpty(target) ? Path.ChangeExtension(mdbPath, ".duckdb") : target),
                "jsonl" or "jsonlines" or "ndjson" => JsonLinesExporter.ExportDatabase(db, string.IsNullOrEmpty(target) ? Path.ChangeExtension(mdbPath, ".jsonl") : target),
                "csv" => CsvExporter.ExportTable(db.Tables.Count > 0 ? db.Tables[0] : new Models.AccessTable(), string.IsNullOrEmpty(target) ? Path.ChangeExtension(mdbPath, ".csv") : target),
                "sql" or "migration" => SqlMigrationExporter.ExportDatabase(db, string.IsNullOrEmpty(target) ? Path.ChangeExtension(mdbPath, ".sql") : target, new SqlMigrationOptions
                {
                    Dialect = SqlMigrationExporter.ParseDialect(dialect),
                    SchemaOnly = schemaOnly,
                    DataOnly = dataOnly,
                    BatchSize = batchSize
                }),
                _ => SqliteExporter.ExportDatabase(db, string.IsNullOrEmpty(target) ? Path.ChangeExtension(mdbPath, ".sqlite") : target)
            };

            Console.WriteLine($"[SUCCESS] Exported successfully to: {outPath}");
        }

        private static void RunSchemaCommand(string mdbPath, string? dialect, string outputPath)
        {
            var parsedDialect = SqlMigrationExporter.ParseDialect(dialect);
            Console.WriteLine($"\n[+] Generating DDL Schema for {mdbPath} [Dialect: {parsedDialect}]");
            var db = Jet3BinaryReader.ReadDatabase(mdbPath, out _);
            
            var options = new SqlMigrationOptions
            {
                Dialect = parsedDialect,
                SchemaOnly = true,
                IncludeForeignKeys = true,
                IncludeViews = true,
                IncludeDropTable = true
            };

            string outPath = SqlMigrationExporter.ExportDatabase(db, outputPath, options);
            Console.WriteLine($"[SUCCESS] Schema DDL exported to: {outPath}");
        }

        private static void RunErdCommand(string mdbPath, string? outputPath)
        {
            Console.WriteLine($"\n[+] Generating Mermaid ERD Schema Diagram for: {mdbPath}");
            var db = Jet3BinaryReader.ReadDatabase(mdbPath, out _);
            var erd = ErdGenerator.GenerateErd(db);

            string outPath = outputPath ?? Path.ChangeExtension(mdbPath, "_erd.md");
            ErdGenerator.ExportErdToMarkdown(db, outPath);

            Console.WriteLine($"[SUCCESS] Discovered {erd.TableCount} tables and {erd.RelationshipCount} foreign key relationships.");
            Console.WriteLine($"          Diagram markdown exported to: {outPath}\n");
            Console.WriteLine("--- Mermaid Diagram ---");
            Console.WriteLine(erd.MermaidCode);
        }

        private static void RunHexCommand(string mdbPath, int pageIndex)
        {
            Console.WriteLine($"\n[+] Inspecting 2KB Sector Page #{pageIndex} of: {mdbPath}");
            var hexView = SectorMapAnalyzer.GetPageHexView(mdbPath, pageIndex);
            Console.WriteLine($"    Page Type: {hexView.PageType} | Status: {hexView.Status} | {hexView.Description}\n");

            foreach (var line in hexView.HexLines)
            {
                Console.WriteLine(line.FormattedLine);
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
