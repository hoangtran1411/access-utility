using System.Text.Json;
using AccessUtility.Models;
using Xunit;

namespace AccessUtility.Tests
{
    public class AutoUpdaterTests
    {
        [Fact]
        public void AutoUpdaterJsonContext_DeserializesGithubReleaseCorrectly()
        {
            // Arrange
            string json = @"
            {
                ""tag_name"": ""v1.2.3"",
                ""assets"": [
                    {
                        ""name"": ""AccessUtility-win-x64.zip"",
                        ""browser_download_url"": ""https://github.com/hoangtran1411/access-utility/releases/download/v1.2.3/AccessUtility-win-x64.zip""
                    },
                    {
                        ""name"": ""AccessUtility-linux-x64.tar.gz"",
                        ""browser_download_url"": ""https://github.com/hoangtran1411/access-utility/releases/download/v1.2.3/AccessUtility-linux-x64.tar.gz""
                    }
                ]
            }";

            // Act
            var release = JsonSerializer.Deserialize<GithubRelease>(json, AutoUpdaterJsonContext.Default.GithubRelease);

            // Assert
            Assert.NotNull(release);
            Assert.Equal("v1.2.3", release.TagName);
            Assert.NotNull(release.Assets);
            Assert.Equal(2, release.Assets.Count);
            
            Assert.Equal("AccessUtility-win-x64.zip", release.Assets[0].Name);
            Assert.Equal("https://github.com/hoangtran1411/access-utility/releases/download/v1.2.3/AccessUtility-win-x64.zip", release.Assets[0].BrowserDownloadUrl);
            
            Assert.Equal("AccessUtility-linux-x64.tar.gz", release.Assets[1].Name);
        }
        
        [Fact]
        public void AutoUpdaterJsonContext_DeserializesEmptyAssetsCorrectly()
        {
            // Arrange
            string json = @"
            {
                ""tag_name"": ""v2.0.0"",
                ""assets"": []
            }";

            // Act
            var release = JsonSerializer.Deserialize<GithubRelease>(json, AutoUpdaterJsonContext.Default.GithubRelease);

            // Assert
            Assert.NotNull(release);
            Assert.Equal("v2.0.0", release.TagName);
            Assert.NotNull(release.Assets);
            Assert.Empty(release.Assets);
        }
    }
}
