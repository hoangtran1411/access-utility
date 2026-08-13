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
                    Description = "Analyzes Access 97 2048-byte page allocation maps (PAM), table definition counts (TDEF), data pages, fragmentation percentage, and corrupt page counts.",
                    Usage = "AccessUtility.exe diagnose <file.mdb>",
                    Examples = new List<string> { "AccessUtility.exe diagnose C:\\DB\\Northwind97.mdb" }
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
                    Description = "Scans all 2KB page sectors for valid table definitions and record rows, bypasses corrupted byte ranges, and reconstructs a healthy .mdb database.",
                    Usage = "AccessUtility.exe repair <file.mdb> [--output target.mdb] [--force-unlock]",
                    Flags = new List<string> { "--output <path>", "--force-unlock" },
                    Examples = new List<string> { "AccessUtility.exe repair C:\\DB\\Corrupted.mdb --force-unlock" }
                },
                new CommandDescriptor
                {
                    Name = "export",
                    Aliases = new[] { "exp", "convert" },
                    Summary = "Export database to SQLite, SQL scripts, or CSV",
                    Description = "Converts Access 97 database schema and tables to modern storage formats.",
                    Usage = "AccessUtility.exe export <file.mdb> [--format sqlite|sql|csv]",
                    Flags = new List<string> { "--format <sqlite|sql|csv>" },
                    Examples = new List<string> { "AccessUtility.exe export C:\\DB\\Main.mdb --format sqlite" }
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
