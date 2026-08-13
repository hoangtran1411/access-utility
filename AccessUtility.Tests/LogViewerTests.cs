using System;
using System.IO;
using AccessUtility.Engine;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace AccessUtility.Tests
{
    public class LogViewerTests
    {
        [Fact]
        public void ShowLogs_ValidDatabase_PrintsLogsWithoutExceptions()
        {
            // Arrange
            string dbPath = Path.Combine(Path.GetTempPath(), $"test_logs_{Guid.NewGuid()}.sqlite");
            
            // This will auto-migrate and create the Logs table
            var sink = new SqliteLogSink(dbPath);
            
            // Seed a log
            var logEvent = new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Error,
                null,
                new MessageTemplateParser().Parse("Test error {Message}"),
                new[] { new LogEventProperty("Message", new ScalarValue("Disk space low")) }
            );
            sink.Emit(logEvent);

            // Redirect console output
            using var sw = new StringWriter();
            var originalOut = Console.Out;
            Console.SetOut(sw);

            try
            {
                // Act
                LogViewer.ShowLogs(dbPath, tailCount: 10, levelFilter: "error");
                
                string output = sw.ToString();

                // Assert
                Assert.Contains("--- Last 1 Logs ---", output);
                Assert.Contains("Test error", output);
                Assert.Contains("Disk space low", output);
                Assert.Contains("[Error]", output);
            }
            finally
            {
                // Restore console
                Console.SetOut(originalOut);
                
                // Clear SQLite connection pools to release file lock on Windows
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                
                // Cleanup
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }
            }
        }
        
        [Fact]
        public void ShowLogs_FileNotFound_PrintsWarning()
        {
            // Arrange
            using var sw = new StringWriter();
            var originalOut = Console.Out;
            Console.SetOut(sw);

            try
            {
                // Act
                LogViewer.ShowLogs("non_existent_logs_db.sqlite");
                
                string output = sw.ToString();

                // Assert
                Assert.Contains("Log database not found", output);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public void GetLogs_ValidDatabase_ReturnsLogEntries()
        {
            // Arrange
            string dbPath = Path.Combine(Path.GetTempPath(), $"test_logs_{Guid.NewGuid()}.sqlite");
            
            var sink = new SqliteLogSink(dbPath);
            
            var logEvent = new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Warning,
                null,
                new MessageTemplateParser().Parse("Web Dashboard started on port {Port}"),
                new[] { new LogEventProperty("Port", new ScalarValue(5000)) }
            );
            sink.Emit(logEvent);

            try
            {
                // Act
                var logs = LogViewer.GetLogs(dbPath, tailCount: 10);
                
                // Assert
                Assert.NotNull(logs);
                Assert.Single(logs);
                
                var entry = logs[0];
                Assert.Equal("Warning", entry.Level);
                Assert.Contains("Web Dashboard started on port 5000", entry.Message);
                Assert.Null(entry.Exception);
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }
            }
        }
    }
}
