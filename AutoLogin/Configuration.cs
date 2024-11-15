using Dalamud.Configuration;
using ImGuiNET;
using System;
using System.Linq;
using Lumina.Excel.Sheets;
using Veda;
using Dalamud.Plugin;

namespace AutoLogin {
    public class Configuration : IPluginConfiguration {

        public int Version { get; set; }
        private bool ShowSupport;
        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            Plugin.PluginInterface = pluginInterface;
        }

        public void Save() {
            Plugin.PluginInterface.SavePluginConfig(this);
        }

        public uint DataCenter;
        public uint World;
        public uint CharacterSlot;

        public bool DrawConfigUI() {
            var drawConfig = true;
            const ImGuiWindowFlags windowFlags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse;

            var dcSheet = Plugin.Data.Excel.GetSheet<WorldDCGroupType>();
            if (dcSheet == null) return false;
            var worldSheet = Plugin.Data.Excel.GetSheet<World>();
            if (worldSheet == null) return false;

            var currentDc = dcSheet.GetRow(DataCenter);
            if (currentDc.Region == 0) {
                DataCenter = 0;
                return true;
            }

            if (ImGui.Begin("AutoLogin Config", ref drawConfig, windowFlags)) {

                if (ImGui.BeginCombo("Data Center", DataCenter == 0 ? "Not Selected" : currentDc.Name.ToString())) {
                    foreach (var dc  in dcSheet.Where(w => w.Region > 0 && w.Name.ToString().Trim().Length > 0)) {
                        if (ImGui.Selectable(dc.Name.ToString(), dc.RowId == DataCenter)) {
                            DataCenter = dc.RowId;
                            Save();
                        }
                    }
                    ImGui.EndCombo();
                }

                if (currentDc.Region != 0) {

                    var currentWorld = worldSheet.GetRow(World);
                    if (/*currentWorld.RowId == 0 || */World != 0 && currentWorld.DataCenter.RowId != DataCenter) {
                        World = 0;
                        return true;
                    }

                    if (ImGui.BeginCombo("World", World == 0 ? "Not Selected" : currentWorld.Name.ToString())) {
                        foreach (var w in worldSheet.Where(w => w.DataCenter.RowId == DataCenter && w.IsPublic)) {
                            if (ImGui.Selectable(w.Name.ToString(), w.RowId == World)) {
                                World = w.RowId;
                                Save();
                            }
                        }
                        ImGui.EndCombo();
                    }

                    if (currentWorld.IsPublic) {
                        if (ImGui.BeginCombo("Character Slot", $"Slot #{CharacterSlot+1}")) {
                            for (uint i = 0; i < 8; i++) {
                                if (ImGui.Selectable($"Slot #{i+1}", CharacterSlot == i)) {
                                    CharacterSlot = i;
                                    Save();
                                }
                            }
                            ImGui.EndCombo();
                        }
                    }

                }
                ImGui.Spacing();
                if (ImGui.Button("Want to help support my work?"))
                {
                    ShowSupport = !ShowSupport;
                }
                if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Click me!"); }
                if (ShowSupport)
                {
                    ImGui.Text("Here are the current ways you can support the work I do.\nEvery bit helps, thank you! Have a great day!");
                    ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.19f, 0.52f, 0.27f, 1));
                    if (ImGui.Button("Donate via Paypal"))
                    {
                        Functions.OpenWebsite("https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=QXF8EL4737HWJ");
                    }
                    ImGui.PopStyleColor();
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.95f, 0.39f, 0.32f, 1));
                    if (ImGui.Button("Become a Patron"))
                    {
                        Functions.OpenWebsite("https://www.patreon.com/bePatron?u=5597973");
                    }
                    ImGui.PopStyleColor();
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.25f, 0.67f, 0.87f, 1));
                    if (ImGui.Button("Support me on Ko-Fi"))
                    {
                        Functions.OpenWebsite("https://ko-fi.com/Y8Y114PMT");
                    }
                    ImGui.PopStyleColor();
                }

            }

            ImGui.End();

            return drawConfig;
        }
    }
}
