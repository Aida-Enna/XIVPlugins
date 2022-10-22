using Dalamud.Configuration;
using ImGuiNET;
using System;
using System.Linq;
using Lumina.Excel.GeneratedSheets;

namespace AutoLogin {
    public class Config : IPluginConfiguration {
        [NonSerialized]
        private Plugin plugin;

        public int Version { get; set; }

        public void Init(Plugin plugin) {
            this.plugin = plugin;
        }

        public void Save() {
            Service.PluginInterface.SavePluginConfig(this);
        }

        public uint DataCenter;
        public uint World;
        public uint CharacterSlot;

        public bool DrawConfigUI() {
            var drawConfig = true;
            const ImGuiWindowFlags windowFlags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse;

            var dcSheet = Service.Data.Excel.GetSheet<WorldDCGroupType>();
            if (dcSheet == null) return false;
            var worldSheet = Service.Data.Excel.GetSheet<World>();
            if (worldSheet == null) return false;

            var currentDc = dcSheet.GetRow(DataCenter);
            if (currentDc == null) {
                DataCenter = 0;
                return true;
            }

            if (ImGui.Begin($"{plugin.Name} Config", ref drawConfig, windowFlags)) {

                if (ImGui.BeginCombo("Data Center", DataCenter == 0 ? "Not Selected" : currentDc.Name.RawString)) {
                    foreach (var dc  in dcSheet.Where(w => w.Region > 0 && w.Name.RawString.Trim().Length > 0)) {
                        if (ImGui.Selectable(dc.Name.RawString, dc.RowId == DataCenter)) {
                            DataCenter = dc.RowId;
                            Save();
                        }
                    }
                    ImGui.EndCombo();
                }

                if (currentDc.Region != 0) {

                    var currentWorld = worldSheet.GetRow(World);
                    if (currentWorld == null || (World != 0 && currentWorld.DataCenter.Row != DataCenter)) {
                        World = 0;
                        return true;
                    }

                    if (ImGui.BeginCombo("World", World == 0 ? "Not Selected" : currentWorld.Name.RawString)) {
                        foreach (var w in worldSheet.Where(w => w.DataCenter.Row == DataCenter && w.IsPublic)) {
                            if (ImGui.Selectable(w.Name.RawString, w.RowId == World)) {
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


            }

            ImGui.End();

            return drawConfig;
        }
    }
}
