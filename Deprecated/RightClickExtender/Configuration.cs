using Dalamud.Configuration;
using Dalamud.Plugin;

namespace RightClickExtender
{
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; }

        private IDalamudPluginInterface pluginInterface;

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
