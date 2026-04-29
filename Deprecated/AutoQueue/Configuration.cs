using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;
using System;

namespace AutoQueue
{
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; }
        public bool EmptyVariable = false;
        public bool AutoQueueEnabled = false;
        public bool AutoAcceptEnabled = false;

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
