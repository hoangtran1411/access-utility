using System;
using System.IO;
using System.IO.Compression;
using AccessUtility.Engine;
using Xunit;

namespace AccessUtility.Tests
{
    public class MaintenanceDaemonTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _backupDir;
        private readonly string _mockDbPath;

        public MaintenanceDaemonTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"AccessUtility_DaemonTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
            _backupDir = Path.Combine(_tempDir, "backups");
            Directory.CreateDirectory(_backupDir);
            
            _mockDbPath = Path.Combine(_tempDir, "TestDb.mdb");
            File.WriteAllBytes(_mockDbPath, new byte[100]); // Dummy file
        }

        [Fact]
        public void CreateZipBackup_ValidFile_CreatesZipArchive()
        {
            string zipPath = MaintenanceDaemon.CreateZipBackup(_mockDbPath, _backupDir);
            
            Assert.True(File.Exists(zipPath));
            Assert.EndsWith(".zip", zipPath, StringComparison.OrdinalIgnoreCase);

            using var archive = ZipFile.OpenRead(zipPath);
            Assert.Single(archive.Entries);
            Assert.Equal("TestDb.mdb", archive.Entries[0].Name);
            Assert.Equal(100, archive.Entries[0].Length);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }
}
