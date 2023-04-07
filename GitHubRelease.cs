using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Globalization;

namespace ChrisKaczor.ApplicationUpdate
{
    public partial class GitHubRelease
    {
        [JsonProperty("tag_name")]
        public string? TagName { get; set; }

        [JsonProperty("assets")]
        public List<Asset>? Assets { get; set; }
    }

    public class Asset
    {
        [JsonProperty("created_at")]
        public DateTimeOffset? CreatedAt { get; set; }

        [JsonProperty("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }
    }

    public partial class GitHubRelease
    {
        public static GitHubRelease? FromJson(string json) => JsonConvert.DeserializeObject<GitHubRelease>(json, Converter.Settings);
    }

    internal class Converter
    {
        public static readonly JsonSerializerSettings Settings = new()
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            }
        };
    }
}