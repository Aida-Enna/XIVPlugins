using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;
using System;

namespace FoodCheck
{
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; }
        public bool PostToEcho = true;
        public bool PostToParty = false;
        public bool OnlyDoHighEndDuties = true;
        public string CustomizableMessage = "<names> should EAT FOOD! <se.7>";

        private DalamudPluginInterface pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface.SavePluginConfig(this);
        }
    }
}
