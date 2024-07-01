using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;

namespace FATEAutoSync
{
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; }

        public bool AutoStanceEnabled { get; set; } = true;
        public bool FateAutoSyncEnabled = true;

        // Add any other properties or methods here.
        [JsonIgnore] private IDalamudPluginInterface? pluginInterface;

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface.SavePluginConfig(this);
        }
    }
}