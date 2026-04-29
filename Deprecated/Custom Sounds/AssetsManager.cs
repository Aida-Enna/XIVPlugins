using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using Dalamud.Interface.ImGuiNotification;
using Newtonsoft.Json;

namespace CustomSounds;

public enum AssetsFileType
{
    Mp3
}

public static class AssetsManager
{

    public const string RequiredAssetsVersion = "1.0.0.0";

    public static bool IsUpdating { get; private set; } = false;

    public static bool HasEnoughFreeDiskSpace { get; private set; } = true;

    public static string? CurrentAssetsVersion { get; private set; }

    private const long RequiredDiskSpaceMp3Compressed = 40_101_040;
    private const long RequiredDiskSpaceMp3Extracted = 44_343_296;

    /// <summary>
    /// Checks if the assets exist, are the current version, and downloads them if necessary.
    /// </summary>
    public static void UpdateVoiceLines(bool force = false)
    {
        Plugin.Log.Information("Validating assets");

        // MIGRATION: delete old assets folder if it exists
        string oldAssetsDir = $"{Plugin.PluginInterface.GetPluginConfigDirectory()}/assets";
        if (Directory.Exists(oldAssetsDir)) Directory.Delete(oldAssetsDir, true);

        bool updateNeeded = force;

        // Check if assets are already downloaded and are the current version
        string configDir = Plugin.PluginInterface.GetPluginConfigDirectory();
        string assetsDir = GetAssetsDirectory();

        CurrentAssetsVersion = CurrentDownloadedAssetVersion();
        if (CurrentAssetsVersion != RequiredAssetsVersion) updateNeeded = true;

        if (!updateNeeded)
        {
            Plugin.Log.Information("Assets validated, nothing to do");
            return;
        }

        IsUpdating = true;
        LogAndNotify("Downloading assets", NotificationType.Info);

        // Clear folder if it exists
        if (Directory.Exists(assetsDir)) Directory.Delete(assetsDir, true);

        // Download assets
        string downloadLocation = $"{configDir}/assets-{RequiredAssetsVersion}.zip";
        Uri assetUri = new($"https://github.com/Aida-Enna/XIVPlugins/raw/main/Plogon/Assets/{RequiredAssetsVersion}/assets.zip");

        HasEnoughFreeDiskSpace = true;
        long freeDiskSpace = new DriveInfo(downloadLocation).AvailableFreeSpace;
        if (freeDiskSpace < GetRequiredDiskSpace())
        {
            LogAndNotify("Not enough free disk space to extract assets", NotificationType.Error);

            HasEnoughFreeDiskSpace = false;
            IsUpdating = false;
            return;
        }

        if (File.Exists(downloadLocation)) File.Delete(downloadLocation);

        HttpClient httpClient = new();
        HttpResponseMessage response = httpClient.GetAsync(assetUri).Result;

        if (!response.IsSuccessStatusCode)
        {
            LogAndNotify($"Unable to download assets: {response.StatusCode} - {response.Content}", NotificationType.Error);
        }

        using (FileStream fs = new(downloadLocation, FileMode.CreateNew))
        {
            response.Content.CopyToAsync(fs).Wait();
        }

        // Extract assets
        LogAndNotify("Extracting assets", NotificationType.Info);

        ZipFile.ExtractToDirectory(downloadLocation, assetsDir);
        File.Delete(downloadLocation);

        LogAndNotify("Asset extraction complete", NotificationType.Success);
        IsUpdating = false;

        // Validate the downloaded assets
        CurrentAssetsVersion = CurrentDownloadedAssetVersion();
        if (CurrentAssetsVersion == RequiredAssetsVersion) return;

        Plugin.Log.Error("Downloaded assets do not match the requested version. Requested = {RequiredAssetsVersion}, Downloaded = {CurrentAssetsVersion}", RequiredAssetsVersion, CurrentAssetsVersion ?? "null");
    }

    public static long GetRequiredDiskSpace()
    {
        return Configuration.Instance.AssetsFileType switch
        {
            AssetsFileType.Mp3 => RequiredDiskSpaceMp3Compressed + RequiredDiskSpaceMp3Extracted,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static string GetAssetsDirectory()
    {
        string configDir = Plugin.PluginInterface.GetPluginConfigDirectory();
        string baseAssetsDir = $"{configDir}/assets";

        return Configuration.Instance.AssetsFileType switch
        {
            AssetsFileType.Mp3 => $"{baseAssetsDir}-mp3",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static string? CurrentDownloadedAssetVersion()
    {
        string assetsDir = GetAssetsDirectory();
        string manifestFile = $"{assetsDir}/manifest.json";

        if (!File.Exists(manifestFile)) return null;

        string jsonData = File.ReadAllText(manifestFile);
        AssetsManifest? manifest = JsonConvert.DeserializeObject<AssetsManifest>(jsonData);

        return manifest?.Version;
    }

    private static void LogAndNotify(string message, NotificationType type)
    {
        switch (type)
        {
            case NotificationType.Success:
            case NotificationType.Info:
                Plugin.Log.Info(message);
                break;
            case NotificationType.Warning:
                Plugin.Log.Warning(message);
                break;
            case NotificationType.Error:
                Plugin.Log.Error(message);
                break;
            default:
            case NotificationType.None:
                Plugin.Log.Debug(message);
                break;
        }

        Notification notification = new()
        {
            Content = message,
            Type = type
        };

        Plugin.NotificationManager.AddNotification(notification);
    }
}

public class AssetsManifest
{
    [JsonProperty("version")]
    public string? Version { get; set; }
}