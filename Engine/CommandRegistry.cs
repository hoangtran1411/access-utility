using System;
using System.Collections.Generic;

namespace AccessUtility.Engine
{
    public class CommandDescriptor
    {
        public string Name { get; set; } = string.Empty;
        public string[] Aliases { get; set; } = Array.Empty<string>();
        public string Summary { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Usage { get; set; } = string.Empty;
        public List<string> Examples { get; set; } = new();
        public List<string> Flags { get; set; } = new();
    }

    public static class CommandRegistry
    {
        public static List<CommandDescriptor> GetCommands()
        {
            return new List<CommandDescriptor>
            {
                new CommandDescriptor
                {
                    Name = "lockstat",
                    Aliases = new[] { "ls", "locks" },
                    Summary = "Inspect .ldb lock file & list connected users",
                    Description = "Parses 64-byte user locking blocks in MS Access .ldb lock files. Lists connected computer names, usernames, and detects stale orphan locks.",
                    Usage = "AccessUtility.exe lockstat <file.mdb>",
                    Examples = new List<string> { "AccessUtility.exe lockstat C:\\DB\\Northwind97.mdb" }
                },
                new CommandDescriptor
                {
                    Name = "diagnose",
                    Aliases = new[] { "diag", "health" },
                    Summary = "Run database health & page fragmentation diagnostics",
                    Description = "Analyzes Access 97 2048-byte page allocation maps (PAM), table definition counts (TDEF), data pages, fragmentation percentage, corrupt page counts, and optional forensic slack space scanning.",
                    Usage = "AccessUtility.exe diagnose <file.mdb> [--forensic-scan]",
                    Flags = new List<string> { "--forensic-scan" },
                    Examples = new List<string> 
                    { 
                        "AccessUtility.exe diagnose C:\\DB\\Northwind97.mdb",
                        "AccessUtility.exe diagnose C:\\DB\\Northwind97.mdb --forensic-scan"
                    }
                },
                new CommandDescriptor
                {
                    Name = "compact",
                    Aliases = new[] { "cmp", "defrag" },
                    Summary = "Defragment & minimize .mdb file size",
                    Description = "Purges deleted record slack space, rebuilds B-tree indexes, re-allocates continuous page structures, and writes a defragmented .mdb file.",
                    Usage = "AccessUtility.exe compact <file.mdb> [--output target.mdb] [--force-unlock]",
                    Flags = new List<string> { "--output <path>", "--force-unlock" },
                    Examples = new List<string> { "AccessUtility.exe compact C:\\DB\\Main.mdb --output Clean.mdb" }
                },
                new CommandDescriptor
                {
                    Name = "repair",
                    Aliases = new[] { "rep", "recover" },
                    Summary = "Deep sector repair & data recovery",
                    Description = "Scans all 2KB page sectors for valid table definitions and record rows, bypasses corrupted byte ranges, salvages deleted rows from slack space, and reconstructs a healthy database.",
                    Usage = "AccessUtility.exe repair <file.mdb> [--output target.mdb] [--force-unlock] [--carve-deleted]",
                    Flags = new List<string> { "--output <path>", "--force-unlock", "--carve-deleted" },
                    Examples = new List<string> 
                    { 
                        "AccessUtility.exe repair C:\\DB\\Corrupted.mdb --force-unlock",
                        "AccessUtility.exe repair C:\\DB\\Damaged.mdb --carve-deleted"
                    }
                },
                new CommandDescriptor
                {
                    Name = "carve",
                    Aliases = new[] { "salvage", "forensic" },
                    Summary = "Forensic record carver for deleted data & slack space",
                    Description = "Scans unallocated page slack space and deleted slot directories, matches column schema heuristics, scores confidence, and exports salvaged deleted records to SQLite or JSON.",
                    Usage = "AccessUtility.exe carve <file.mdb> [--table <tableName>] [--output <target.sqlite|target.json>]",
                    Flags = new List<string> { "--table <name>", "--output <path>" },
                    Examples = new List<string>
                    {
                        "AccessUtility.exe carve Damaged97.mdb",
                        "AccessUtility.exe carve Damaged97.mdb --table Customers --output ./recovered/customers_deleted.sqlite"
                    }
                },
                new CommandDescriptor
                {
                    Name = "export",
                    Aliases = new[] { "exp", "convert" },
                    Summary = "Export database to Parquet, DuckDB, JSON Lines, SQLite, SQL, or CSV",
                    Description = "Converts Access 97 database schema and tables to modern storage and analytical formats including Apache Parquet, DuckDB, streaming JSON Lines, SQLite, SQL scripts, and CSV.",
                    Usage = "AccessUtility.exe export <file.mdb> [--format parquet|duckdb|jsonl|sqlite|sql|csv] [--output <path>]",
                    Flags = new List<string> { "--format <parquet|duckdb|jsonl|sqlite|sql|csv>", "--output <path>" },
                    Examples = new List<string> 
                    { 
                        "AccessUtility.exe export C:\\DB\\Main.mdb --format parquet --output ./exports/",
                        "AccessUtility.exe export C:\\DB\\Main.mdb --format duckdb --output ./analytics.duckdb",
                        "AccessUtility.exe export C:\\DB\\Main.mdb --format jsonl --output ./exports/" 
                    }
                },
                new CommandDescriptor
                {
                    Name = "password",
                    Aliases = new[] { "pw", "security" },
                    Summary = "Decrypt database password & inspect security settings",
                    Description = "Reads Jet 3.5 Page 0 at offset 0x42 and XOR-decrypts the 14-byte password block using the static Jet3 mask. Also reports User-Level Security (ULS) flags, encryption-at-rest, and owner SID. Optionally parses System.mdw workgroup files.",
                    Usage = "AccessUtility.exe password <file.mdb> [--workgroup System.mdw]",
                    Flags = new List<string> { "--workgroup <path to System.mdw>" },
                    Examples = new List<string>
                    {
                        "AccessUtility.exe password C:\\DB\\Protected97.mdb",
                        "AccessUtility.exe password C:\\DB\\Protected97.mdb --workgroup C:\\Windows\\System32\\System.mdw"
                    }
                },
                new CommandDescriptor
                {
                    Name = "diff",
                    Aliases = new[] { "compare" },
                    Summary = "Compare two databases & generate SQL migration script",
                    Description = "Compares Source (Dev) and Target (Prod) schemas (tables, columns, data types). Generates ANSI SQL, SQLite, PostgreSQL, or SQL Server DDL migration scripts.",
                    Usage = "AccessUtility.exe diff <source.mdb> <target.mdb> [--output migration.sql] [--dialect ansi|sqlite|pgsql|mssql]",
                    Flags = new List<string> { "--output <path>", "--dialect <ansi|sqlite|pgsql|mssql>" },
                    Examples = new List<string>
                    {
                        "AccessUtility.exe diff Dev.mdb Prod.mdb",
                        "AccessUtility.exe diff Dev.mdb Prod.mdb --dialect pgsql --output up.sql"
                    }
                },
                new CommandDescriptor
                {
                    Name = "extract-ole",
                    Aliases = new[] { "extract" },
                    Summary = "Extract OLE embedded objects (BMP, PDF, Word)",
                    Description = "Scans Long Binary (OLE) fields, strips Access OLE Container Headers (78-bytes), and dumps embedded files to disk.",
                    Usage = "AccessUtility.exe extract-ole <file.mdb> [--output ./extracted]",
                    Flags = new List<string> { "--output <dir>" },
                    Examples = new List<string>
                    {
                        "AccessUtility.exe extract-ole Products97.mdb",
                        "AccessUtility.exe extract-ole Products97.mdb --output C:\\ExportedFiles"
                    }
                },
                new CommandDescriptor
                {
                    Name = "extract-queries",
                    Aliases = new[] { "queries" },
                    Summary = "Extract saved queries to SQL files",
                    Description = "Parses MSysQueries and MSysObjects to reconstruct SQL SELECT, JOIN, WHERE, GROUP BY clauses.",
                    Usage = "AccessUtility.exe extract-queries <file.mdb> [--output ./queries]",
                    Flags = new List<string> { "--output <dir>" },
                    Examples = new List<string>
                    {
                        "AccessUtility.exe extract-queries Products97.mdb",
                        "AccessUtility.exe extract-queries Products97.mdb --output ./out"
                    }
                },
                new CommandDescriptor
                {
                    Name = "erd",
                    Aliases = new[] { "diagram" },
                    Summary = "Generate Mermaid Entity-Relationship Diagram (ERD)",
                    Description = "Analyzes database schema, primary keys, and foreign keys to generate clean Mermaid ERD diagrams and markdown reports.",
                    Usage = "AccessUtility.exe erd <file.mdb> [--output schema_erd.md]",
                    Flags = new List<string> { "--output <path>" },
                    Examples = new List<string>
                    {
                        "AccessUtility.exe erd Northwind97.mdb",
                        "AccessUtility.exe erd Northwind97.mdb --output ./docs/schema.md"
                    }
                },
                new CommandDescriptor
                {
                    Name = "hex",
                    Aliases = new[] { "page", "inspect-page" },
                    Summary = "Inspect raw 2048-byte sector page hex & ASCII bytes",
                    Description = "Renders formatted 16-byte hexadecimal dump and ASCII characters for any specified page index.",
                    Usage = "AccessUtility.exe hex <file.mdb> [--page 0]",
                    Flags = new List<string> { "--page <number>" },
                    Examples = new List<string>
                    {
                        "AccessUtility.exe hex sample97.mdb --page 0",
                        "AccessUtility.exe hex sample97.mdb --page 2"
                    }
                },
                new CommandDescriptor
                {
                    Name = "daemon",
                    Aliases = new[] { "maintain" },
                    Summary = "Run automated background maintenance",
                    Description = "Periodically cleans orphan locks, compacts database if fragmented > 15%, and generates ZIP backups.",
                    Usage = "AccessUtility.exe daemon <file.mdb> [--interval 24h] [--backup-dir ./backups]",
                    Flags = new List<string> { "--interval <hours>", "--backup-dir <dir>" },
                    Examples = new List<string>
                    {
                        "AccessUtility.exe daemon Products97.mdb",
                        "AccessUtility.exe daemon Products97.mdb --interval 12h --backup-dir D:\\Backups"
                    }
                },
                new CommandDescriptor
                {
                    Name = "logs",
                    Aliases = new[] { "log", "tail" },
                    Summary = "View and filter the local SQLite application logs",
                    Description = "Reads the Serilog SQLite database (app_logs.sqlite) and prints color-coded logs to the terminal. Supports filtering by level and tailing recent entries.",
                    Usage = "AccessUtility.exe logs [--tail <count>] [--level <info|warning|error>]",
                    Flags = new List<string> { "--tail", "--level", "--db" },
                    Examples = new List<string> { "AccessUtility.exe logs --tail 20", "AccessUtility.exe logs --level error" }
                },
                new CommandDescriptor
                {
                    Name = "update",
                    Aliases = new string[0],
                    Summary = "Update AccessUtility to the latest release",
                    Description = "Queries GitHub for the latest release version, downloads the proper binaries for your OS architecture, and replaces the current executable.",
                    Usage = "AccessUtility.exe update",
                    Flags = new List<string>(),
                    Examples = new List<string> { "AccessUtility.exe update" }
                },
                new CommandDescriptor
                {
                    Name = "ax",
                    Aliases = new[] { "ai", "ask" },
                    Summary = "AX (AI Experiment) Natural Language Command Assistant",
                    Description = "Interprets natural language queries (e.g. 'compact my database and clean locks') and executes maintenance plans automatically.",
                    Usage = "AccessUtility.exe ax [\"query string\"]",
                    Examples = new List<string> { "AccessUtility.exe ax \"Clean locks and compact C:\\DB\\Main.mdb\"" }
                },
                new CommandDescriptor
                {
                    Name = "web",
                    Aliases = new[] { "ui", "dashboard" },
                    Summary = "Launch Web Dashboard UI in browser",
                    Description = "Hosts embedded ASP.NET Core Native AOT Web Server serving interactive single-page dashboard.",
                    Usage = "AccessUtility.exe web [--port 5000]",
                    Flags = new List<string> { "--port <number>" },
                    Examples = new List<string> { "AccessUtility.exe web --port 5000" }
                }
            };
        }

        public static void PrintCobraHelp()
        {
            Console.WriteLine("""
========================================================================
 AccessUtility (.NET 10 Native AOT) - Cobra CLI System
 Focus: Access 97 Compact, Repair, Lock Inspector & AX Assistant
========================================================================

Usage:
  AccessUtility.exe [command] [flags]

Available Commands:
""");

            foreach (var cmd in GetCommands())
            {
                string aliases = cmd.Aliases.Length > 0 ? $" (aliases: {string.Join(", ", cmd.Aliases)})" : "";
                Console.WriteLine($"  {cmd.Name,-12} {cmd.Summary}{aliases}");
            }

            Console.WriteLine("""

Flags:
  -h, --help    Show help for AccessUtility

Use "AccessUtility.exe [command] --help" for more information about a command.
""");
        }
    }
}
