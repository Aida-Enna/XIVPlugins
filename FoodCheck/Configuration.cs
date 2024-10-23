using Dalamud.Configuration;
using Dalamud.Game.Text;
using Dalamud.Plugin;
using Newtonsoft.Json;
using System;

namespace FoodCheck
{
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; }
        public bool PostToParty = false;
        public bool PostOnReadyCheck = true;
        public bool PostOnCountdown = true;
        public bool OnlyDoHighEndDuties = true;
        public bool OnlyUseFirstNames = true;
        public string CustomizableMessage = "<names> should EAT FOOD! <se.7>";
        public XivChatType ChatType;

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
