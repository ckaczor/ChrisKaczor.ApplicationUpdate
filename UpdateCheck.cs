using System.Diagnostics;
using System.Reflection;

namespace ChrisKaczor.ApplicationUpdate;

public enum ServerType
{
    Generic,
    GitHub
}

public static class UpdateCheck
{
    public delegate void ApplicationShutdownDelegate();

    public delegate void ApplicationCurrentMessageDelegate(string title, string message);

    public delegate bool ApplicationUpdateMessageDelegate(string title, string message);

    public static ApplicationShutdownDelegate? ApplicationShutdown { get; private set; }
    public static ApplicationCurrentMessageDelegate? ApplicationCurrentMessage { get; private set; }
    public static ApplicationUpdateMessageDelegate? ApplicationUpdateMessage { get; private set; }

    public static ServerType UpdateServerType { get; private set; } = ServerType.Generic;
    public static string UpdateServer { get; private set; } = string.Empty;
    public static string UpdateFile { get; private set; } = string.Empty;
    public static string ApplicationName { get; private set; } = string.Empty;

    public static VersionInfo? RemoteVersion { get; private set; }
    public static string? LocalInstallFile { get; private set; }
    public static bool UpdateAvailable { get; private set; }

    public static Version LocalVersion => Assembly.GetEntryAssembly()?.GetName().Version ?? new Version();

    public static DefaultResources Resources => new();

    public static void Initialize(ServerType serverType, string updateServer, string updateFile, string applicationName, ApplicationShutdownDelegate applicationShutdownDelegate, ApplicationCurrentMessageDelegate applicationCurrentMessageDelegate, ApplicationUpdateMessageDelegate applicationUpdateMessageDelegate)
    {
        UpdateServerType = serverType;
        UpdateServer = updateServer;
        UpdateFile = updateFile;
        ApplicationName = applicationName;
        ApplicationShutdown = applicationShutdownDelegate;
        ApplicationCurrentMessage = applicationCurrentMessageDelegate;
        ApplicationUpdateMessage = applicationUpdateMessageDelegate;
    }

    public static async Task<bool> CheckForUpdate()
    {
        RemoteVersion = await VersionInfo.Load(UpdateServerType, UpdateServer, UpdateFile);

        if (RemoteVersion == null)
            return false;

        var serverVersion = RemoteVersion.Version;
        var localVersion = LocalVersion;

        UpdateAvailable = serverVersion > localVersion;

        return true;
    }

    public static async Task<bool> DownloadUpdate()
    {
        if (RemoteVersion == null)
            return false;

        var remoteFile = UpdateServerType == ServerType.GitHub ? RemoteVersion.InstallFile : UpdateServer + RemoteVersion.InstallFile;

        if (remoteFile == null)
            return false;

        var remoteUri = new Uri(remoteFile);

        LocalInstallFile = Path.Combine(Path.GetTempPath(), remoteUri.Segments.Last());

        using var client = new HttpClient();

        using var result = await client.GetAsync(remoteUri);

        var content = result.IsSuccessStatusCode ? await result.Content.ReadAsByteArrayAsync() : null;

        if (content == null)
            return false;

        await File.WriteAllBytesAsync(LocalInstallFile, content);

        return true;
    }

    public static bool InstallUpdate()
    {
        if (RemoteVersion == null || LocalInstallFile == null)
            return false;

        Process.Start(LocalInstallFile, "/passive");

        return true;
    }

    public static async void DisplayUpdateInformation(bool showIfCurrent)
    {
        await CheckForUpdate();

        // Check for an update
        if (UpdateAvailable)
        {
            // Load the version string from the server
            var serverVersion = RemoteVersion!.Version;

            // Format the check title
            var updateCheckTitle = string.Format(Resources.UpdateCheckTitle, ApplicationName);

            // Format the message
            var updateCheckMessage = string.Format(Resources.UpdateCheckNewVersion, ApplicationName, serverVersion);

            // Ask the user to update
            if (ApplicationUpdateMessage!(updateCheckTitle, updateCheckMessage))
                return;

            // Get the update
            await DownloadUpdate();

            // Start to install the update
            InstallUpdate();

            // Restart the application
            ApplicationShutdown!();
        }
        else if (showIfCurrent)
        {
            // Format the check title
            var updateCheckTitle = string.Format(Resources.UpdateCheckTitle, ApplicationName);

            // Format the message
            var updateCheckMessage = string.Format(Resources.UpdateCheckCurrent, ApplicationName);

            ApplicationCurrentMessage!(updateCheckTitle, updateCheckMessage);
        }
    }
}