using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;
using System;

namespace Maily
{
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; }
        public bool DeleteLetters = true;
        public int MaxLettersToOpen = 20;

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
