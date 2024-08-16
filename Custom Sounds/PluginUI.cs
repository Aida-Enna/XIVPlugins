using CustomSounds.Services;
using CustomSounds.Utility;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CustomSounds
{
    public class PluginUI
    {
        public bool IsVisible;

        public void Draw()
        {
            if (!IsVisible || !ImGui.Begin("Custom Sounds Config", ref IsVisible, ImGuiWindowFlags.AlwaysAutoResize))
                return;
            if (ImGui.BeginTabBar("##StanleyParableConfigurationTabBar", ImGuiTabBarFlags.None))
            {
                if (ImGui.BeginTabItem("Volume"))
                {
                    Configuration.Instance.BindToXivVolumeSource = false;
                    //int bindToXivVolumeSourceState = Configuration.Instance.BindToXivVolumeSource ? 1 : 0;
                    //string[] bindToXivVolumeSourceOptions =
                    //{
                    //    "Set Volume",
                    //    "Bind to game volume"
                    //};

                    //if (ImGui.Combo("##BindToXivVolumeSource", ref bindToXivVolumeSourceState, bindToXivVolumeSourceOptions, bindToXivVolumeSourceOptions.Length))
                    //{
                    //    Configuration.Instance.BindToXivVolumeSource = bindToXivVolumeSourceState == 1;
                    //    Configuration.Instance.Save();

                    //    AudioPlayer.Instance.UpdateVolume();
                    //}

                    //if (Configuration.Instance.BindToXivVolumeSource)
                    //{
                    //    XivVolumeSource xivVolumeSource = Configuration.Instance.XivVolumeSource;
                    //    int xivVolumeSourceState = (int)xivVolumeSource;
                    //    string[] xivVolumeSourceOptions =
                    //    {
                    //    "BGM",
                    //    "Sound Effects",
                    //    "Voice",
                    //    "System Sounds",
                    //    "Ambient Sounds",
                    //    "Performance"
                    //};

                    //    if (ImGui.Combo("##XivVolumeSource", ref xivVolumeSourceState, xivVolumeSourceOptions,
                    //            xivVolumeSourceOptions.Length))
                    //    {
                    //        Configuration.Instance.XivVolumeSource = (XivVolumeSource)xivVolumeSourceState;
                    //        Configuration.Instance.Save();

                    //        AudioPlayer.Instance.UpdateVolume();
                    //    }

                    //    int volumeBoostValue = (int)Configuration.Instance.XivVolumeSourceBoost;
                    //    if (ImGui.SliderInt("Volume Boost", ref volumeBoostValue, 0, 100))
                    //    {
                    //        Configuration.Instance.XivVolumeSourceBoost = (uint)volumeBoostValue;
                    //        Configuration.Instance.Save();

                    //        AudioPlayer.Instance.UpdateVolume();
                    //    }
                    //}
                    //else
                    //{
                        int volumeValue = (int)Configuration.Instance.Volume;
                        if (ImGui.SliderInt("Volume", ref volumeValue, 0, 100))
                        {
                            Configuration.Instance.Volume = (uint)volumeValue;
                            Configuration.Instance.Save();

                            AudioPlayer.Instance.UpdateVolume();
                        }
                    //}

                    string buttonText = "Play random voice line";
                    if (AssetsManager.IsUpdating)
                    {
                        buttonText = "Voice lines are currently downloading, please wait...";
                        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * 0.5f);
                    }

                    if (ImGui.Button(buttonText))
                    {
                        if (!AssetsManager.IsUpdating)
                        {
                            AudioPlayer.Instance.PlaySoundSimple("lol_ping");
                        }
                    }

                    if (AssetsManager.IsUpdating) ImGui.PopStyleVar();

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Assets"))
                {
                    ImGui.PushID("Assets");

                    string configDir = Plugin.PluginInterface.GetPluginConfigDirectory();
                    string baseAssetsDir = $"{configDir}/assets";

                    bool mp3AssetsDownloaded = Directory.Exists($"{baseAssetsDir}-mp3");

                    List<string> assetsDownloaded = new();
                    if (mp3AssetsDownloaded) assetsDownloaded.Add("mp3");

                    ImGui.Text("Asset file type");

                    int assetsFileType = (int)Configuration.Instance.AssetsFileType;
                    string[] assetsFileTypeOptions =
                    {
                    "MP3",
                };

                    if (ImGui.Combo("##AssetsFileType", ref assetsFileType, assetsFileTypeOptions,
                            assetsFileTypeOptions.Length))
                    {
                        Configuration.Instance.AssetsFileType = (AssetsFileType)assetsFileType;
                        Configuration.Instance.Save();

                        if ((Configuration.Instance.AssetsFileType == AssetsFileType.Mp3 && !mp3AssetsDownloaded))
                        {
                            Plugin.UpdateVoiceLines();
                        }
                    }

                    ImGui.Text($"Assets currently downloaded: {string.Join(", ", assetsDownloaded)}");

                    ImGui.Separator();

                    if (!AssetsManager.HasEnoughFreeDiskSpace)
                    {
                        long diskSpaceRequired = AssetsManager.GetRequiredDiskSpace();
                        long diskSpaceRequiredMb = diskSpaceRequired / 1024 / 1024;

                        ImGui.Text($"\nUnable to download voice lines!\n\n{diskSpaceRequiredMb}MB of free disk space is required.\nPlease clear some space and try again.\n\n");

                        if (ImGui.Button("Download voice lines"))
                        {
                            Plugin.UpdateVoiceLines();
                        }
                    }
                    else
                    {
                        string buttonText = "Re-download assets";
                        if (AssetsManager.IsUpdating)
                        {
                            buttonText = "Voice lines are currently downloading, please wait...";
                            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * 0.5f);
                        }

                        if (ImGui.Button(buttonText))
                        {
                            if (!AssetsManager.IsUpdating) Plugin.UpdateVoiceLines(true);
                        }

                        if (AssetsManager.IsUpdating) ImGui.PopStyleVar();
                    }

                    if (!mp3AssetsDownloaded) ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * 0.5f);
                    if (ImGui.Button("Delete MP3 assets"))
                    {
                        if (mp3AssetsDownloaded)
                        {
                            Task.Run(() => Directory.Delete($"{baseAssetsDir}-mp3", true));
                        }
                    }
                    if (!mp3AssetsDownloaded) ImGui.PopStyleVar();

                    ImGui.Separator();

                    ImGui.Text($"Required assets version: {AssetsManager.RequiredAssetsVersion}");
                    ImGui.Text($"Current assets version: {AssetsManager.CurrentAssetsVersion}");

                    ImGui.PopID();

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Debug"))
                {
                    ImGui.PushID("Debug");
                    bool enableDebugLogging = Configuration.Instance.EnableDebugLogging;
                    if (ImGui.Checkbox("Enable debug logging", ref enableDebugLogging))
                    {
                        Configuration.Instance.EnableDebugLogging = enableDebugLogging;
                        Configuration.Instance.Save();
                    }
                    ImGui.PopID();

                    ImGui.EndTabItem();
                }
            }

            ImGui.EndTabBar();
            ImGui.End();
        }
    }
}