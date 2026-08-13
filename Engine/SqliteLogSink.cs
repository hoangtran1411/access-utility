using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Configuration;

namespace AccessUtility.Engine
{
    /// <summary>
    /// Native AOT compatible Serilog sink that logs to a SQLite database.
    /// Avoids third-party ORMs or reflection to ensure trimming compatibility.
    /// </summary>
    public class SqliteLogSink : ILogEventSink
    {
        private readonly string _connectionString;

        public SqliteLogSink(string databasePath)
        {
            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string createTableSql = @"
                CREATE TABLE IF NOT EXISTS Logs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Timestamp DATETIME NOT NULL,
                    Level VARCHAR(15) NOT NULL,
                    Message TEXT NOT NULL,
                    Exception TEXT NULL
                );
            ";

            using var cmd = new SqliteCommand(createTableSql, connection);
            cmd.ExecuteNonQuery();
        }

        public void Emit(LogEvent logEvent)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                string insertSql = @"
                    INSERT INTO Logs (Timestamp, Level, Message, Exception) 
                    VALUES (@ts, @level, @msg, @ex);
                ";

                using var cmd = new SqliteCommand(insertSql, connection);
                cmd.Parameters.AddWithValue("@ts", logEvent.Timestamp.UtcDateTime);
                cmd.Parameters.AddWithValue("@level", logEvent.Level.ToString());
                cmd.Parameters.AddWithValue("@msg", logEvent.RenderMessage());
                
                object exObj = logEvent.Exception != null ? logEvent.Exception.ToString() : DBNull.Value;
                cmd.Parameters.AddWithValue("@ex", exObj);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // Silent catch: sinks should not crash the application
                Console.WriteLine($"[SqliteLogSink Error] Failed to write log: {ex.Message}");
            }
        }
    }

    public static class SqliteLogSinkExtensions
    {
        public static LoggerConfiguration Sqlite(
            this LoggerSinkConfiguration loggerConfiguration,
            string databasePath)
        {
            return loggerConfiguration.Sink(new SqliteLogSink(databasePath));
        }
    }

    public static class AppLogger
    {
        public static void Initialize(string logDbPath = "app_logs.sqlite")
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.Sqlite(logDbPath)
                .CreateLogger();

            Log.Information("Serilog initialized with SQLite Sink. Log DB: {Path}", logDbPath);
        }

        public static void CloseAndFlush()
        {
            Log.CloseAndFlush();
        }
    }
}
