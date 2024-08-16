using Dalamud.Configuration;
using Dalamud.Plugin;

namespace AutoPillion
{
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; }
        public int CooldownInSeconds = 20;
        public bool OnlyMountFriends = false;

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
