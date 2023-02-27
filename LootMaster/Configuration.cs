using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;

namespace DalamudPluginProjectTemplate
{
    public class Configuration : IPluginConfiguration
    {
        public bool EnableChatLogMessage = true;
        public bool EnableDelay = true;

        public int LowNum = 500;
        public int HighNum = 750;
        [JsonIgnore]
        private DalamudPluginInterface pluginInterface;

        public int Version { get; set; }

        public void Initialize(DalamudPluginInterface pluginInterface) => this.pluginInterface = pluginInterface;

        public void Save() => pluginInterface.SavePluginConfig(this);
    }
}
