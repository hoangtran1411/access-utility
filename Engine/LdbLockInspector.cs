using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AccessUtility.Models;

namespace AccessUtility.Engine
{
    public static class LdbLockInspector
    {
        public static LockFileInfo Inspect(string mdbPath)
        {
            var info = new LockFileInfo();
            if (string.IsNullOrWhiteSpace(mdbPath)) return info;

            string ldbPath = Path.ChangeExtension(mdbPath, ".ldb");
            info.LdbPath = ldbPath;
            info.Exists = File.Exists(ldbPath);

            // Check if MDB file is locked by OS/Process
            info.IsFileInUse = IsFileLocked(mdbPath);

            if (info.Exists)
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(ldbPath);
                    int recordSize = 64;
                    int count = bytes.Length / recordSize;

                    for (int i = 0; i < count; i++)
                    {
                        int offset = i * recordSize;
                        string compName = Encoding.ASCII.GetString(bytes, offset, 32).Replace("\0", "").Trim();
                        string userName = Encoding.ASCII.GetString(bytes, offset + 32, 32).Replace("\0", "").Trim();

                        if (!string.IsNullOrWhiteSpace(compName) || !string.IsNullOrWhiteSpace(userName))
                        {
                            info.ConnectedUsers.Add(new LdbLockEntry
                            {
                                EntryIndex = i + 1,
                                ComputerName = string.IsNullOrWhiteSpace(compName) ? "Unknown" : compName,
                                UserName = string.IsNullOrWhiteSpace(userName) ? "Admin" : userName,
                                IsActive = info.IsFileInUse
                            });
                        }
                    }
                }
                catch
                {
                    // LDB file might be locked exclusively by MS Access
                }

                // An orphan lock occurs when LDB exists but no active process holds a lock on the MDB
                info.IsOrphanLock = !info.IsFileInUse;
            }

            return info;
        }

        public static bool TryCleanOrphanLock(string mdbPath, out string message)
        {
            string ldbPath = Path.ChangeExtension(mdbPath, ".ldb");
            if (!File.Exists(ldbPath))
            {
                message = "No .ldb file exists.";
                return true;
            }

            if (IsFileLocked(mdbPath))
            {
                message = "Cannot clean lock file: Database file is actively in use by another process.";
                return false;
            }

            try
            {
                File.Delete(ldbPath);
                message = "Successfully removed orphan .ldb lock file.";
                return true;
            }
            catch (Exception ex)
            {
                message = $"Failed to delete .ldb lock file: {ex.Message}";
                return false;
            }
        }

        public static bool IsFileLocked(string filePath)
        {
            if (!File.Exists(filePath)) return false;

            try
            {
                using FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                stream.Close();
                return false;
            }
            catch (IOException)
            {
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
        }
    }
}
