using System.Xml.Linq;

namespace ChrisKaczor.ApplicationUpdate;

public class VersionInfo
{
    public Version? Version { get; set; }
    public string? InstallFile { get; set; }
    public DateTimeOffset? InstallCreated { get; set; }

    public static async Task<VersionInfo?> Load(ServerType updateServerType, string server, string file)
    {
        return updateServerType switch
        {
            ServerType.Generic => LoadFile(server, file),
            ServerType.GitHub => await LoadGitHub(server),
            _ => null
        };
    }

    private static VersionInfo? LoadFile(string server, string file)
    {
        try
        {
            var document = XDocument.Load(server + file);

            var versionInformationElement = document.Element("versionInformation");

            if (versionInformationElement == null)
                return null;

            var versionElement = versionInformationElement.Element("version");
            var installFileElement = versionInformationElement.Element("installFile");
            var installCreatedElement = versionInformationElement.Element("installCreated");

            if (versionElement == null || installFileElement == null || installCreatedElement == null)
                return null;

            var versionInfo = new VersionInfo
            {
                Version = Version.Parse(versionElement.Value),
                InstallFile = installFileElement.Value,
                InstallCreated = DateTimeOffset.Parse(installCreatedElement.Value)
            };

            return versionInfo;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static async Task<VersionInfo?> LoadGitHub(string server)
    {
        try
        {
            var httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UpdateCheck.ApplicationName);

            var json = await httpClient.GetStringAsync(server);

            var release = GitHubRelease.FromJson(json);

            if (release?.TagName == null || release.Assets == null || release.Assets.Count == 0)
                return null;

            var versionInfo = new VersionInfo
            {
                Version = Version.Parse(release.TagName),
                InstallFile = release.Assets[0].BrowserDownloadUrl,
                InstallCreated = release.Assets[0].CreatedAt?.UtcDateTime
            };

            return versionInfo;
        }
        catch (Exception)
        {
            return null;
        }
    }
}