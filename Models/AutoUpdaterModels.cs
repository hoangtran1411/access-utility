using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AccessUtility.Models
{
    public class GithubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }

    public class GithubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("assets")]
        public List<GithubAsset> Assets { get; set; } = new();
    }

    [JsonSerializable(typeof(GithubRelease))]
    [JsonSerializable(typeof(GithubAsset))]
    [JsonSerializable(typeof(List<GithubAsset>))]
    public partial class AutoUpdaterJsonContext : JsonSerializerContext
    {
    }
}
