using Dalamud.Configuration;
using Dalamud.Plugin;

namespace PortraitFixer
{
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; }
        public bool AutoUpdatePortaitFromGearsetUpdate = true;
        public bool ShowMessageInChatWhenAutoUpdatingPortaitFromGearsetUpdate = true;

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
