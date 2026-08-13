using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using AccessUtility.Models;
using Serilog;

namespace AccessUtility.Engine
{
    public static class AutoUpdater
    {
        private const string RepoApiUrl = "https://api.github.com/repos/hoangtran1411/access-utility/releases/latest";
        
        public static async Task CheckAndUpdateAsync()
        {
            Console.WriteLine("[+] Checking for updates...");
            
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("AccessUtility-AutoUpdater");
                
                string json = await client.GetStringAsync(RepoApiUrl);
                var release = JsonSerializer.Deserialize<GithubRelease>(json, AutoUpdaterJsonContext.Default.GithubRelease);
                
                if (release == null || string.IsNullOrEmpty(release.TagName))
                {
                    Log.Warning("Failed to parse release info from GitHub.");
                    Console.WriteLine("[-] Failed to check for updates. Could not parse release info.");
                    return;
                }

                string latestVersionStr = release.TagName.TrimStart('v');
                string currentVersionStr = "1.0.0"; // Normally from Assembly, hardcoding for this release logic or using reflection
                
                var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
                if (assemblyVersion != null)
                {
                    currentVersionStr = $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}";
                }

                Console.WriteLine($"    Current Version: v{currentVersionStr}");
                Console.WriteLine($"    Latest Version:  {release.TagName}");

                if (Version.TryParse(latestVersionStr, out var latestVersion) && Version.TryParse(currentVersionStr, out var currentVersion))
                {
                    if (latestVersion <= currentVersion)
                    {
                        Console.WriteLine("\n[SUCCESS] You are already running the latest version!");
                        return;
                    }
                }
                else
                {
                    // Fallback to simple string comparison if semantic version parsing fails (e.g. preview tags)
                    if (release.TagName.Equals($"v{currentVersionStr}", StringComparison.OrdinalIgnoreCase))
                    {
                         Console.WriteLine("\n[SUCCESS] You are already running the latest version!");
                         return;
                    }
                }

                Console.WriteLine("\n[!] A new version is available! Initiating update...");
                await DownloadAndReplaceAsync(release);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Update check failed.");
                Console.WriteLine($"\n[-] Update check failed: {ex.Message}");
            }
        }

        private static async Task DownloadAndReplaceAsync(GithubRelease release)
        {
            string osArch = GetOsArchString();
            string expectedAssetName = $"AccessUtility-{osArch}";
            
            // On Windows, the artifact might be a .zip. On Linux/Mac, it might be a .tar.gz
            var asset = release.Assets.FirstOrDefault(a => 
                a.Name.Contains(osArch, StringComparison.OrdinalIgnoreCase) && 
                (a.Name.EndsWith(".zip") || a.Name.EndsWith(".tar.gz")));

            if (asset == null)
            {
                Console.WriteLine($"[-] Could not find a matching release asset for your OS: {osArch}");
                return;
            }

            Console.WriteLine($"    Downloading: {asset.Name}...");
            
            string tempDir = Path.Combine(Path.GetTempPath(), "AccessUtility_Update");
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);

            string downloadPath = Path.Combine(tempDir, asset.Name);
            
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("AccessUtility-AutoUpdater");
                var response = await client.GetAsync(asset.BrowserDownloadUrl);
                response.EnsureSuccessStatusCode();
                using (var fs = new FileStream(downloadPath, FileMode.Create))
                {
                    await response.Content.CopyToAsync(fs);
                }
            }

            Console.WriteLine("    Extracting...");
            string currentExePath = Process.GetCurrentProcess().MainModule?.FileName ?? AppDomain.CurrentDomain.BaseDirectory;
            if (string.IsNullOrEmpty(currentExePath) || !File.Exists(currentExePath))
            {
                Console.WriteLine("[-] Could not determine current executable path.");
                return;
            }

            string extractedExePath = string.Empty;

            if (asset.Name.EndsWith(".zip"))
            {
                ZipFile.ExtractToDirectory(downloadPath, tempDir, true);
                extractedExePath = Path.Combine(tempDir, "AccessUtility.exe");
                if (!File.Exists(extractedExePath)) extractedExePath = Path.Combine(tempDir, "AccessUtility");
            }
            else if (asset.Name.EndsWith(".tar.gz"))
            {
                // Simple workaround for tar.gz if needed, typically Linux/macOS
                // A robust implementation would use a Tar extraction library or invoke the system 'tar' command
                Process.Start(new ProcessStartInfo { FileName = "tar", Arguments = $"-xzf {downloadPath} -C {tempDir}", UseShellExecute = false })?.WaitForExit();
                extractedExePath = Path.Combine(tempDir, "AccessUtility");
            }

            if (!File.Exists(extractedExePath))
            {
                 Console.WriteLine($"[-] Failed to extract executable from the archive. Looked for: {extractedExePath}");
                 return;
            }

            Console.WriteLine("    Replacing binary...");
            
            string backupPath = currentExePath + ".old";
            if (File.Exists(backupPath)) File.Delete(backupPath);
            
            // Move current to .old to bypass "file in use" locks (especially on Windows)
            File.Move(currentExePath, backupPath);
            File.Copy(extractedExePath, currentExePath, true);

            Console.WriteLine("\n[SUCCESS] AccessUtility has been updated to " + release.TagName);
            Console.WriteLine("          Please restart the application to use the new version.");
            
            // Optionally, we could launch a script to delete the .old file, but for a CLI, letting the user restart is fine.
        }

        private static string GetOsArchString()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "win-x64";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "osx-arm64";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "linux-x64";
            
            return "win-x64"; // Default fallback
        }
    }
}
