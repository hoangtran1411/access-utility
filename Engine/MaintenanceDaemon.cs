using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using AccessUtility.Models;
using Serilog;

namespace AccessUtility.Engine
{
    /// <summary>
    /// Feature 05: Maintenance Daemon &amp; Backup Scheduler
    /// </summary>
    public static class MaintenanceDaemon
    {
        public static void RunDaemon(string mdbPath, TimeSpan interval, string backupDir, CancellationToken cancellationToken)
        {
            Log.Information("Starting Maintenance Daemon for {Path}. Interval: {Interval}", mdbPath, interval);
            Console.WriteLine($"\n[+] Maintenance Daemon Started.");
            Console.WriteLine($"    Target: {mdbPath}");
            Console.WriteLine($"    Interval: {interval.TotalHours} hours");
            Console.WriteLine($"    Backup Dir: {backupDir}");
            Console.WriteLine($"    Press Ctrl+C to exit.\n");

            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    RunMaintenanceCycle(mdbPath, backupDir);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error during maintenance cycle on {Path}", mdbPath);
                }

                // Wait for the next interval or cancellation
                try
                {
                    Task.Delay(interval, cancellationToken).Wait();
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            Log.Information("Maintenance Daemon stopped.");
        }

        public static void RunMaintenanceCycle(string mdbPath, string backupDir)
        {
            if (!File.Exists(mdbPath))
            {
                Log.Warning("Target database {Path} does not exist.", mdbPath);
                return;
            }

            Log.Information("--- Starting Maintenance Cycle for {Path} ---", mdbPath);

            // Step 1: Clean Orphan Lock
            bool lockCleaned = LdbLockInspector.TryCleanOrphanLock(mdbPath, out string lockMsg);
            if (lockCleaned)
            {
                Log.Information("Cleaned orphaned .ldb lock file: {Message}", lockMsg);
            }

            // Step 2: Diagnostics & Fragmentation Check
            var db = Jet3BinaryReader.ReadDatabase(mdbPath, out var diag);
            double fragmentationPct = 0;
            if (diag.TotalPages > 0)
            {
                fragmentationPct = ((double)diag.FreeSlackPagesCount / diag.TotalPages) * 100.0;
            }

            Log.Information("Database Health: {Corrupt} corrupt pages. Fragmentation: {Frag:F2}%", diag.CorruptPagesCount, fragmentationPct);

            // Step 3: Compact if needed (Threshold: > 15%)
            string currentDbPath = mdbPath;
            if (fragmentationPct > 15.0 || diag.CorruptPagesCount > 0)
            {
                Log.Information("Threshold exceeded (Frag > 15% or Corrupt). Initiating compaction...");
                string tempCompactPath = mdbPath + ".compacted.tmp";
                
                var compactResult = Jet3Compactor.Compact(mdbPath, tempCompactPath, true);
                
                if (compactResult.Success && File.Exists(tempCompactPath))
                {
                    string backupBeforeReplace = mdbPath + ".bak";
                    File.Copy(mdbPath, backupBeforeReplace, true);
                    File.Copy(tempCompactPath, mdbPath, true);
                    File.Delete(tempCompactPath);
                    File.Delete(backupBeforeReplace); // Clean temp backup after successful overwrite
                    Log.Information("Compaction successful. Database replaced atomically.");
                }
                else
                {
                    Log.Error("Compaction failed: {Message}", compactResult.Message);
                }
            }

            // Step 4: Timestamped ZIP Backup
            CreateZipBackup(currentDbPath, backupDir);

            Log.Information("--- Maintenance Cycle Completed ---");
        }

        public static string CreateZipBackup(string mdbPath, string backupDir)
        {
            string fileName = Path.GetFileNameWithoutExtension(mdbPath);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string zipFileName = $"{fileName}_Backup_{timestamp}.zip";
            string zipPath = Path.Combine(backupDir, zipFileName);

            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                archive.CreateEntryFromFile(mdbPath, Path.GetFileName(mdbPath));
            }

            Log.Information("Created backup archive: {ZipPath}", zipPath);
            return zipPath;
        }
    }
}
