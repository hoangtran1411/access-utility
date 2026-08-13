using System;
using System.IO;
using AccessUtility.Engine;
using AccessUtility.Models;
using Xunit;

namespace AccessUtility.Tests
{
    /// <summary>
    /// Feature 01: Unit tests for SecurityReader — Jet 3.5 password decryptor and System.mdw workgroup parser.
    /// </summary>
    public class SecurityReaderTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _tempMdb;
        private readonly string _tempMdw;

        // ── Jet 3.5 XOR mask (must match SecurityReader.Jet3PasswordMask) ──
        private static readonly byte[] Jet3Mask = new byte[]
        {
            0x86, 0xFB, 0xEC, 0x37, 0x5D, 0x44, 0x9C, 0xFA,
            0xC6, 0x5E, 0x28, 0xE6, 0x13, 0xB6
        };

        public SecurityReaderTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"AccessUtility_SecurityTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
            _tempMdb = Path.Combine(_tempDir, "test_security.mdb");
            _tempMdw = Path.Combine(_tempDir, "System.mdw");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helper: Build a minimal valid Jet 3.5 Page 0 (2048 bytes)
        // ─────────────────────────────────────────────────────────────────────

        private static byte[] BuildJet35Page0(string? password = null)
        {
            byte[] page = new byte[2048];

            // Jet DB signature at offset 4 (15 chars)
            byte[] sig = System.Text.Encoding.ASCII.GetBytes("Standard Jet DB");
            Array.Copy(sig, 0, page, 4, sig.Length);

            // Jet 3.5 version byte at offset 0x14
            page[0x14] = 0x01;

            // Embed XOR-encrypted password at offset 0x42 (if provided)
            if (!string.IsNullOrEmpty(password))
            {
                byte[] pwBytes = System.Text.Encoding.ASCII.GetBytes(password);
                for (int i = 0; i < 14; i++)
                {
                    byte plain = i < pwBytes.Length ? pwBytes[i] : (byte)0x00;
                    page[0x42 + i] = (byte)(plain ^ Jet3Mask[i]);
                }
            }
            // If no password: leave bytes at offset 0x42 as XOR of 0x00 ^ mask = mask itself,
            // but for "no password" we need XOR result to be 0x00, so bytes must equal mask.
            // Actually "no password" means encrypted bytes = mask XOR 0x00 = mask.
            // But the reader returns null when all decrypted bytes are 0x00,
            // which happens when encrypted bytes = Jet3Mask (XOR cancels out to 0x00).
            // We store nothing (zeros) → decrypted = 0x00 ^ mask = mask ≠ 0, so "no password"
            // is better represented by storing mask bytes themselves → decrypted = mask XOR mask = 0.
            else
            {
                // Store mask bytes so XOR decrypts to all zeros → no password
                Array.Copy(Jet3Mask, 0, page, 0x42, Jet3Mask.Length);
            }

            return page;
        }

        private void CreateMdbWithPassword(string? password)
        {
            byte[] page0 = BuildJet35Page0(password);
            // Write file with full page0 (2048 bytes)
            File.WriteAllBytes(_tempMdb, page0);
        }

        private void CreateMinimalMdwFile()
        {
            byte[] page = new byte[2048];

            // Jet DB signature
            byte[] sig = System.Text.Encoding.ASCII.GetBytes("Standard Jet DB");
            Array.Copy(sig, 0, page, 4, sig.Length);
            page[0x14] = 0x01;

            // Embed "Admins" group name as a length-prefixed ANSI string somewhere in file
            string[] names = { "Admin", "Admins", "Users" };
            int pos = 512;
            foreach (var name in names)
            {
                byte[] nameBytes = System.Text.Encoding.ASCII.GetBytes(name);
                page[pos] = (byte)nameBytes.Length;
                Array.Copy(nameBytes, 0, page, pos + 1, nameBytes.Length);
                pos += 1 + nameBytes.Length + 2;
            }

            File.WriteAllBytes(_tempMdw, page);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 1: DecryptDatabasePassword – known password round-trip
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void DecryptDatabasePassword_KnownPassword_ReturnsCorrectPlaintext()
        {
            // Arrange: XOR-encrypt "Secret123" with Jet3 mask → build page0 bytes
            const string expected = "Secret123";
            byte[] page0 = BuildJet35Page0(expected);

            // Act
            string? actual = SecurityReader.DecryptDatabasePassword(page0);

            // Assert
            Assert.Equal(expected, actual);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 2: DecryptDatabasePassword – no password (all zero decrypted)
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void DecryptDatabasePassword_NoPassword_ReturnsNull()
        {
            // Arrange: store mask bytes → decrypted = mask XOR mask = all zeros → null
            byte[] page0 = BuildJet35Page0(null); // null = "no password" path

            // Act
            string? actual = SecurityReader.DecryptDatabasePassword(page0);

            // Assert
            Assert.Null(actual);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 3: EncryptDatabasePassword / DecryptDatabasePassword round-trip
        // ─────────────────────────────────────────────────────────────────────

        [Theory]
        [InlineData("abc")]
        [InlineData("MyPass1234")]
        [InlineData("AAAAAAAAAAAAA")] // 13 chars
        [InlineData("AAAAAAAAAAAAAA")] // 14 chars (max)
        public void EncryptThenDecrypt_RoundTrip_ReturnsSamePassword(string password)
        {
            // Arrange
            byte[] encrypted = SecurityReader.EncryptDatabasePassword(password);
            byte[] page0 = new byte[2048];
            Array.Copy(encrypted, 0, page0, 0x42, encrypted.Length);

            // Also set the signature so DetectJetVersion works (not strictly needed here)
            byte[] sig = System.Text.Encoding.ASCII.GetBytes("Standard Jet DB");
            Array.Copy(sig, 0, page0, 4, sig.Length);
            page0[0x14] = 0x01;

            // Act
            string? decrypted = SecurityReader.DecryptDatabasePassword(page0);

            // Assert
            Assert.Equal(password, decrypted);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 4: DetectJetVersion – Jet 3.5 identified correctly
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void DetectJetVersion_Jet35Page_ReturnsJet35String()
        {
            byte[] page0 = BuildJet35Page0();
            string version = SecurityReader.DetectJetVersion(page0);
            Assert.Contains("3.5", version);
            Assert.Contains("Access 97", version);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 5: DetectJetVersion – wrong signature returns empty
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void DetectJetVersion_InvalidHeader_ReturnsEmpty()
        {
            byte[] page0 = new byte[2048]; // all zeros, no "Jet DB" signature
            string version = SecurityReader.DetectJetVersion(page0);
            Assert.Empty(version);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 6: InspectDatabase – file not found returns error
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void InspectDatabase_FileNotFound_ReturnsErrorMessage()
        {
            string missing = Path.Combine(_tempDir, "nonexistent.mdb");
            var result = SecurityReader.InspectDatabase(missing);
            Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
            Assert.False(result.IsValidJetDatabase);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 7: InspectDatabase – password-protected database detected
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void InspectDatabase_WithPassword_DetectsPasswordProtection()
        {
            // Arrange
            CreateMdbWithPassword("MySecret99");

            // Act
            var result = SecurityReader.InspectDatabase(_tempMdb);

            // Assert
            Assert.True(result.IsPasswordProtected, "Should detect password protection");
            Assert.Equal("MySecret99", result.DatabasePassword);
            Assert.Equal("Success", result.InspectionStatus);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 8: InspectDatabase – no password database
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void InspectDatabase_NoPassword_ReportsNotPasswordProtected()
        {
            // Arrange: mask bytes at 0x42 → decrypted all zeros → no password
            CreateMdbWithPassword(null);

            // Act
            var result = SecurityReader.InspectDatabase(_tempMdb);

            // Assert
            Assert.False(result.IsPasswordProtected, "Should not detect a password");
            Assert.Null(result.DatabasePassword);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 9: InspectWorkgroup – file not found returns error
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void InspectWorkgroup_FileNotFound_ReturnsError()
        {
            string missing = Path.Combine(_tempDir, "missing.mdw");
            var result = SecurityReader.InspectWorkgroup(missing);
            Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
            Assert.False(result.IsValidWorkgroupFile);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 10: InspectWorkgroup – valid System.mdw detects groups
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void InspectWorkgroup_ValidMdw_DetectsDefaultGroups()
        {
            // Arrange
            CreateMinimalMdwFile();

            // Act
            var result = SecurityReader.InspectWorkgroup(_tempMdw);

            // Assert
            Assert.True(result.IsValidWorkgroupFile, "Should recognize Jet DB signature");
            Assert.Equal("Success", result.InspectionStatus);
            Assert.NotEmpty(result.Groups); // "Admins" and/or "Users" should be detected
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 11: EncryptDatabasePassword – produces 14-byte block
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void EncryptDatabasePassword_AlwaysReturns14Bytes()
        {
            byte[] result = SecurityReader.EncryptDatabasePassword("Hi");
            Assert.Equal(14, result.Length);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 12: ReadPage0 – reads exactly 2048 bytes from a valid file
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void ReadPage0_ValidFile_Returns2048Bytes()
        {
            CreateMdbWithPassword(null);
            byte[] page0 = SecurityReader.ReadPage0(_tempMdb);
            Assert.Equal(2048, page0.Length);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Cleanup
        // ─────────────────────────────────────────────────────────────────────

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* ignore */ }
        }
    }
}
