using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace AccessUtility.Engine
{
    public static class LogViewer
    {
        public static void ShowLogs(string databasePath, int tailCount = 50, string? levelFilter = null)
        {
            if (string.IsNullOrWhiteSpace(databasePath) || !System.IO.File.Exists(databasePath))
            {
                Console.WriteLine($"[-] Log database not found at '{databasePath}'. The tool may not have run any logged operations yet.");
                return;
            }

            try
            {
                string connectionString = new SqliteConnectionStringBuilder
                {
                    DataSource = databasePath,
                    Mode = SqliteOpenMode.ReadOnly
                }.ToString();

                using var connection = new SqliteConnection(connectionString);
                
                // We clear pools before operating on SQLite db
                SqliteConnection.ClearAllPools();
                connection.Open();

                string query = "SELECT Id, Timestamp, Level, Message, Exception FROM Logs";
                
                if (!string.IsNullOrWhiteSpace(levelFilter))
                {
                    query += " WHERE Level LIKE @level";
                }
                
                query += " ORDER BY Timestamp DESC LIMIT @limit";

                using var cmd = new SqliteCommand(query, connection);
                cmd.Parameters.AddWithValue("@limit", tailCount);
                if (!string.IsNullOrWhiteSpace(levelFilter))
                {
                    cmd.Parameters.AddWithValue("@level", $"%{levelFilter}%");
                }

                using var reader = cmd.ExecuteReader();
                var logs = new List<string>();

                while (reader.Read())
                {
                    long id = reader.GetInt64(0);
                    DateTime ts = reader.GetDateTime(1).ToLocalTime();
                    string level = reader.GetString(2);
                    string message = reader.GetString(3);
                    string? exception = reader.IsDBNull(4) ? null : reader.GetString(4);

                    string color = level.ToLower() switch
                    {
                        "error" or "fatal" => "\x1b[31m", // Red
                        "warning" => "\x1b[33m", // Yellow
                        "info" or "information" => "\x1b[32m", // Green
                        _ => "\x1b[37m" // White
                    };
                    string reset = "\x1b[0m";

                    string logLine = $"{color}[{ts:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{reset}";
                    if (!string.IsNullOrWhiteSpace(exception))
                    {
                        logLine += $"\n{color}Exception: {exception}{reset}";
                    }
                    
                    logs.Add(logLine);
                }

                if (logs.Count == 0)
                {
                    Console.WriteLine("[*] No logs found matching the criteria.");
                    return;
                }

                Console.WriteLine($"\n--- Last {logs.Count} Logs ---");
                // Print in ascending order (oldest to newest)
                for (int i = logs.Count - 1; i >= 0; i--)
                {
                    Console.WriteLine(logs[i]);
                }
                Console.WriteLine("------------------------\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[-] Failed to read logs: {ex.Message}");
            }
        }
    }
}
