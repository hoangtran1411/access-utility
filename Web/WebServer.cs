using System;
using System.IO;
using System.Text.Json;
using AccessUtility.Engine;
using AccessUtility.Exporters;
using AccessUtility.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AccessUtility.Web
{
    public static class WebServer
    {
        public static void StartServer(int port = 5000)
        {
            var builder = WebApplication.CreateSlimBuilder();

            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.TypeInfoResolver = AppJsonContext.Default;
            });

            var app = builder.Build();

            // Serve Web UI
            app.MapGet("/", () => Results.Content(DashboardHtml.HtmlContent, "text/html"));

            // API: Diagnose
            app.MapGet("/api/diagnose", (string path) =>
            {
                Jet3BinaryReader.ReadDatabase(path, out var report);
                return Results.Json(report, AppJsonContext.Default.DiagnosticReport);
            });

            // API: Lock Status
            app.MapGet("/api/lockstat", (string path) =>
            {
                var lockInfo = LdbLockInspector.Inspect(path);
                return Results.Json(lockInfo, AppJsonContext.Default.LockFileInfo);
            });

            // API: Clean Lock
            app.MapGet("/api/clean-lock", (string path) =>
            {
                bool success = LdbLockInspector.TryCleanOrphanLock(path, out string msg);
                return Results.Json(new CleanLockResponse { Success = success, Message = msg }, AppJsonContext.Default.CleanLockResponse);
            });

            // API: Compact
            app.MapGet("/api/compact", (string path, string? target, bool force = false) =>
            {
                string targetPath = string.IsNullOrWhiteSpace(target) ? Path.ChangeExtension(path, ".compacted.mdb") : target;
                var res = Jet3Compactor.Compact(path, targetPath, force);
                return Results.Json(res, AppJsonContext.Default.CompactResult);
            });

            // API: Repair
            app.MapGet("/api/repair", (string path, string? target, bool force = false) =>
            {
                string targetPath = string.IsNullOrWhiteSpace(target) ? Path.ChangeExtension(path, ".repaired.mdb") : target;
                var res = Jet3Repairer.Repair(path, targetPath, force);
                return Results.Json(res, AppJsonContext.Default.RepairResult);
            });

            // API: Export
            app.MapGet("/api/export", (string path, string format) =>
            {
                var db = Jet3BinaryReader.ReadDatabase(path, out _);
                string fmt = format?.ToLower() ?? "sqlite";
                string outPath = fmt switch
                {
                    "csv" => CsvExporter.ExportTable(db.Tables.Count > 0 ? db.Tables[0] : new AccessTable(), Path.ChangeExtension(path, ".csv")),
                    "sql" => SqlScriptExporter.ExportDatabase(db, Path.ChangeExtension(path, ".sql")),
                    _ => SqliteExporter.ExportDatabase(db, Path.ChangeExtension(path, ".sqlite"))
                };
                return Results.Json(new ExportResponse { Success = true, OutputPath = outPath, Format = fmt }, AppJsonContext.Default.ExportResponse);
            });

            Console.WriteLine($"==================================================");
            Console.WriteLine($" Access 97 Utility Web Dashboard Running!");
            Console.WriteLine($" URL: http://localhost:{port}");
            Console.WriteLine($" Press Ctrl+C to stop.");
            Console.WriteLine($"==================================================");

            app.Run($"http://localhost:{port}");
        }
    }
}
