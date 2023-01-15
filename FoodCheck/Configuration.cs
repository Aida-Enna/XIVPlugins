using Dalamud.Configuration;
using Dalamud.Plugin;

namespace FoodCheck
{
    public class Configuration : IPluginConfiguration
    {
        int IPluginConfiguration.Version { get; set; }

        #region Saved configuration values
        public string CoolText { get; set; }
        #endregion

        private readonly DalamudPluginInterface pluginInterface;

        public Configuration(DalamudPluginInterface pi)
        {
            this.pluginInterface = pi;
        }

        public void Save()
        {
            this.pluginInterface.SavePluginConfig(this);
        }
    }
}
