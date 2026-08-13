using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AccessUtility.Models;

namespace AccessUtility.Engine
{
    /// <summary>
    /// Feature 01: Database Password &amp; Security Inspector
    /// Decrypts Access 97 (Jet 3.5) database passwords from Page 0 using XOR masking,
    /// and parses System.mdw workgroup files to enumerate users and groups.
    /// </summary>
    public static class SecurityReader
    {
        // ── Jet 3.5 XOR mask (14 bytes) applied to Page 0 offset 0x42..0x4F ──
        // This static mask is hardcoded in the Jet 3.5 database engine.
        private static readonly byte[] Jet3PasswordMask = new byte[]
        {
            0x86, 0xFB, 0xEC, 0x37, 0x5D, 0x44, 0x9C, 0xFA,
            0xC6, 0x5E, 0x28, 0xE6, 0x13, 0xB6
        };

        private const int PasswordOffset = 0x42;      // 66 decimal
        private const int PasswordLength = 14;         // 14 bytes
        private const int PageSize = 2048;             // Jet 3.5 page size

        // ── Jet 3.5 version byte at Page 0 offset 0x14 ──
        private const int VersionOffset = 0x14;
        private const byte Jet35VersionByte = 0x01;

        // ── System.mdw well-known section identifiers ──
        private const string MdwMagicAscii = "Standard Jet DB";
        private const string MdwMagicAccess = "Jet DB";

        // ── MSysAccounts entry magic bytes for workgroup parsing ──
        // System.mdw stores user SIDs in a repeating 64-byte structure block.
        private const int MdwUserBlockSize = 64;

        // ─────────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Inspects the Access 97 database password and security settings.
        /// </summary>
        /// <param name="mdbFilePath">Path to the .mdb database file.</param>
        /// <returns>Full security inspection result.</returns>
        public static SecurityInspectionResult InspectDatabase(string mdbFilePath)
        {
            var result = new SecurityInspectionResult
            {
                DatabasePath = mdbFilePath
            };

            if (!File.Exists(mdbFilePath))
            {
                result.ErrorMessage = $"File not found: {mdbFilePath}";
                return result;
            }

            try
            {
                byte[] page0 = ReadPage0(mdbFilePath);
                if (page0 == null || page0.Length < PageSize)
                {
                    result.ErrorMessage = "Database file is too small to contain a valid Jet 3.5 header page.";
                    return result;
                }

                // Verify Jet 3.5 signature
                result.JetVersion = DetectJetVersion(page0);
                result.IsValidJetDatabase = !string.IsNullOrEmpty(result.JetVersion);

                // Decrypt the database password from Page 0 offset 0x42
                result.DatabasePassword = DecryptDatabasePassword(page0);
                result.IsPasswordProtected = !string.IsNullOrEmpty(result.DatabasePassword);

                // Read database creation date / SID owner at Page 0 (offset 0x5A)
                result.DatabaseOwnerSid = ReadOwnerSid(page0);

                // Check User-Level Security encryption flag
                result.HasUserLevelSecurity = CheckUserLevelSecurityFlag(page0);

                // Check encryption-at-rest flag (encrypted database bit)
                result.IsEncryptedAtRest = CheckEncryptionFlag(page0);

                result.InspectionStatus = "Success";
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Inspection failed: {ex.Message}";
                result.InspectionStatus = "Error";
            }

            return result;
        }

        /// <summary>
        /// Parses a System.mdw workgroup file and returns all user and group accounts.
        /// </summary>
        /// <param name="mdwFilePath">Path to the System.mdw workgroup file.</param>
        /// <returns>Workgroup inspection result with users, groups, and SIDs.</returns>
        public static WorkgroupInspectionResult InspectWorkgroup(string mdwFilePath)
        {
            var result = new WorkgroupInspectionResult
            {
                WorkgroupPath = mdwFilePath
            };

            if (!File.Exists(mdwFilePath))
            {
                result.ErrorMessage = $"Workgroup file not found: {mdwFilePath}";
                return result;
            }

            try
            {
                byte[] fileBytes = File.ReadAllBytes(mdwFilePath);

                if (fileBytes.Length < PageSize)
                {
                    result.ErrorMessage = "File is too small to be a valid System.mdw workgroup file.";
                    return result;
                }

                // Verify Jet 3.5 header signature
                string headerStr = Encoding.ASCII.GetString(fileBytes, 4, 15).TrimEnd('\0');
                result.IsValidWorkgroupFile = headerStr.Contains(MdwMagicAccess) ||
                                              headerStr.Contains(MdwMagicAscii);

                if (!result.IsValidWorkgroupFile)
                {
                    result.ErrorMessage = "Invalid workgroup file: Jet DB signature not found.";
                    return result;
                }

                // Scan for user/group account structures in the workgroup file
                result.Users = ParseWorkgroupAccounts(fileBytes);
                result.Groups = ParseWorkgroupGroups(fileBytes);

                // Read workgroup ID (WID) from Page 0 offset 0x38 (8 bytes)
                if (fileBytes.Length >= 0x40)
                {
                    result.WorkgroupId = ReadHexBlock(fileBytes, 0x38, 8);
                }

                result.InspectionStatus = "Success";
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Workgroup parsing failed: {ex.Message}";
                result.InspectionStatus = "Error";
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Password Decryption
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Decrypts the Jet 3.5 database password from a Page 0 byte array.
        /// Reads 14 bytes at offset 0x42 and XORs each byte with the static Jet3 mask.
        /// Returns null or empty string if no password is set (all XOR-result bytes are 0x00).
        /// </summary>
        public static string? DecryptDatabasePassword(byte[] page0)
        {
            if (page0 == null || page0.Length < PasswordOffset + PasswordLength)
                return null;

            byte[] decrypted = new byte[PasswordLength];
            bool hasPassword = false;

            for (int i = 0; i < PasswordLength; i++)
            {
                decrypted[i] = (byte)(page0[PasswordOffset + i] ^ Jet3PasswordMask[i]);
                if (decrypted[i] != 0x00)
                    hasPassword = true;
            }

            if (!hasPassword)
                return null; // No password set

            // Trim null terminators to reveal plaintext password
            string password = Encoding.ASCII.GetString(decrypted).TrimEnd('\0').Trim();
            return string.IsNullOrEmpty(password) ? null : password;
        }

        /// <summary>
        /// Encrypts a plaintext password back into the Jet 3.5 XOR format.
        /// Returns the 14-byte encrypted block ready for writing to Page 0 offset 0x42.
        /// </summary>
        public static byte[] EncryptDatabasePassword(string password)
        {
            byte[] plainBytes = new byte[PasswordLength];
            byte[] passwordBytes = Encoding.ASCII.GetBytes(password);

            int copyLen = Math.Min(passwordBytes.Length, PasswordLength);
            Array.Copy(passwordBytes, plainBytes, copyLen);

            byte[] encrypted = new byte[PasswordLength];
            for (int i = 0; i < PasswordLength; i++)
            {
                encrypted[i] = (byte)(plainBytes[i] ^ Jet3PasswordMask[i]);
            }
            return encrypted;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Jet Header Parsing Helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Reads exactly 2048 bytes (Page 0) from the database file.</summary>
        public static byte[] ReadPage0(string mdbFilePath)
        {
            byte[] page0 = new byte[PageSize];
            using var fs = new FileStream(mdbFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            int read = fs.Read(page0, 0, PageSize);
            if (read < PageSize)
                Array.Resize(ref page0, read);
            return page0;
        }

        /// <summary>Identifies the Jet version from Page 0 version byte at offset 0x14.</summary>
        public static string DetectJetVersion(byte[] page0)
        {
            if (page0 == null || page0.Length <= VersionOffset) return string.Empty;

            string headerSig = page0.Length >= 20
                ? Encoding.ASCII.GetString(page0, 4, Math.Min(16, page0.Length - 4)).TrimEnd('\0')
                : string.Empty;

            if (!headerSig.Contains("Jet") && !headerSig.Contains("Standard"))
                return string.Empty;

            return page0[VersionOffset] switch
            {
                0x00 => "Jet 3.0 (Access 95)",
                0x01 => "Jet 3.5 (Access 97)",
                0x02 => "Jet 4.0 (Access 2000/2002/2003)",
                0x03 => "Jet 4.0 Extended (Access 2007+)",
                _ => $"Unknown Jet Version (byte=0x{page0[VersionOffset]:X2})"
            };
        }

        /// <summary>
        /// Reads the database owner SID from Page 0.
        /// In Jet 3.5, the SID is at offset 0x5A (8 bytes).
        /// Returns hex representation of the raw bytes.
        /// </summary>
        private static string ReadOwnerSid(byte[] page0)
        {
            const int sidOffset = 0x5A;
            const int sidLength = 8;
            if (page0.Length < sidOffset + sidLength) return string.Empty;
            return ReadHexBlock(page0, sidOffset, sidLength);
        }

        /// <summary>
        /// Checks if the database has the User-Level Security (ULS) flag set.
        /// In Jet 3.5, this is indicated by the flags byte at Page 0 offset 0x5C.
        /// Bit 0x08 = User-Level Security enabled.
        /// </summary>
        private static bool CheckUserLevelSecurityFlag(byte[] page0)
        {
            const int flagsOffset = 0x5C;
            if (page0.Length <= flagsOffset) return false;
            return (page0[flagsOffset] & 0x08) != 0;
        }

        /// <summary>
        /// Checks if the database has the XOR/RC4 encryption-at-rest flag.
        /// Jet 3.5 databases store this at Page 0 offset 0x12, bit 0x04.
        /// </summary>
        private static bool CheckEncryptionFlag(byte[] page0)
        {
            const int encOffset = 0x12;
            if (page0.Length <= encOffset) return false;
            return (page0[encOffset] & 0x04) != 0;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Workgroup (System.mdw) Parsing
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Scans all pages of a System.mdw file for user account blocks.
        /// User account blocks are identified by an ANSI name segment preceded by
        /// the ASCII marker bytes 0x1F (Unit Separator) or other known patterns.
        /// </summary>
        private static List<WorkgroupUser> ParseWorkgroupAccounts(byte[] fileBytes)
        {
            var users = new List<WorkgroupUser>();
            int totalPages = fileBytes.Length / PageSize;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int page = 1; page < totalPages; page++)
            {
                int pageOffset = page * PageSize;

                // Only scan TDEF pages (magic 0x02, 0x01) which store system catalog objects
                if (fileBytes[pageOffset] == 0x02 && fileBytes[pageOffset + 1] == 0x01)
                {
                    ScanPageForUsers(fileBytes, pageOffset, users, seen);
                }
            }

            // If no structured users found, try heuristic ANSI string scan
            if (users.Count == 0)
            {
                HeuristicUserScan(fileBytes, users, seen);
            }

            return users;
        }

        private static void ScanPageForUsers(byte[] fileBytes, int pageOffset, List<WorkgroupUser> users, HashSet<string> seen)
        {
            // Walk through the page looking for user name length-prefix patterns
            int pos = pageOffset + 8;
            int pageEnd = pageOffset + PageSize;

            while (pos < pageEnd - 4)
            {
                byte nameLen = fileBytes[pos];
                if (nameLen >= 1 && nameLen <= 40 && pos + 1 + nameLen <= pageEnd)
                {
                    string candidate = Encoding.ASCII.GetString(fileBytes, pos + 1, nameLen);
                    if (IsValidAccountName(candidate) && !seen.Contains(candidate))
                    {
                        // Check next byte after name for SID indicator or another name
                        int afterName = pos + 1 + nameLen;
                        string sid = string.Empty;
                        if (afterName + 8 <= pageEnd)
                        {
                            // Try to read a SID block
                            sid = ReadHexBlock(fileBytes, afterName, Math.Min(8, pageEnd - afterName));
                        }

                        seen.Add(candidate);
                        users.Add(new WorkgroupUser
                        {
                            AccountName = candidate,
                            Sid = sid,
                            AccountType = WorkgroupAccountType.User
                        });
                    }
                }
                pos++;
            }
        }

        private static void HeuristicUserScan(byte[] fileBytes, List<WorkgroupUser> users, HashSet<string> seen)
        {
            // Fallback: scan entire file for length-prefixed ANSI strings between 1-40 bytes
            int pos = PageSize; // Skip Page 0
            while (pos < fileBytes.Length - 2)
            {
                byte nameLen = fileBytes[pos];
                if (nameLen >= 1 && nameLen <= 40 && pos + 1 + nameLen <= fileBytes.Length)
                {
                    string candidate = Encoding.ASCII.GetString(fileBytes, pos + 1, nameLen);
                    if (IsValidAccountName(candidate) && !seen.Contains(candidate)
                        && !candidate.StartsWith("MSys") && !candidate.StartsWith("~"))
                    {
                        seen.Add(candidate);
                        users.Add(new WorkgroupUser
                        {
                            AccountName = candidate,
                            Sid = string.Empty,
                            AccountType = WorkgroupAccountType.User
                        });
                        pos += nameLen; // Skip past the name
                    }
                }
                pos++;
            }
        }

        private static List<WorkgroupGroup> ParseWorkgroupGroups(byte[] fileBytes)
        {
            var groups = new List<WorkgroupGroup>();

            // Known default Access 97 workgroup groups
            string[] defaultGroups = { "Admins", "Users", "Guests" };
            foreach (var grp in defaultGroups)
            {
                if (ContainsAsciiString(fileBytes, grp))
                {
                    groups.Add(new WorkgroupGroup { GroupName = grp });
                }
            }

            return groups;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Utilities
        // ─────────────────────────────────────────────────────────────────────

        private static string ReadHexBlock(byte[] data, int offset, int length)
        {
            var sb = new StringBuilder(length * 3);
            for (int i = 0; i < length && offset + i < data.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(data[offset + i].ToString("X2"));
            }
            return sb.ToString();
        }

        private static bool IsValidAccountName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            foreach (char c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != ' ' && c != '-' && c != '.') return false;
            }
            return true;
        }

        private static bool ContainsAsciiString(byte[] data, string target)
        {
            byte[] targetBytes = Encoding.ASCII.GetBytes(target);
            for (int i = 0; i <= data.Length - targetBytes.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < targetBytes.Length; j++)
                {
                    if (data[i + j] != targetBytes[j]) { match = false; break; }
                }
                if (match) return true;
            }
            return false;
        }
    }
}
