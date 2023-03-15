using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Veda;

namespace PotatoFamine2
{
    public class Configuration : IPluginConfiguration {
        [NonSerialized]
        private DalamudPluginInterface pluginInterface;

        public int Version { get; set; } = 1;

        public bool OnlyChangeLalafells { get; set; } = true;

        public Race ChangeOthersTargetRace { get; set; } = Race.HYUR;

        public bool ChangeSelf { get; set; } = false;

        public Race ChangeSelfTargetRace { get; set; } = Race.HYUR;

        public bool ShouldChangeOthers { get; set; } = false;

        public bool ForciblyChangePeople { get; set; } = false;
        public Race ForciblyChangePeopleTargetRace { get; set; } = Race.HYUR;

        public bool UseTrustedList { get; set; } = false;

        //[JsonIgnore] // Experimental feature - do not load/save
        public bool ImmersiveMode { get; set; } = false;

        public List<PlayerData> TrustedList { get; set; } = new List<PlayerData>();
        public List<PlayerData> ForciblyChangeList { get; } = new List<PlayerData>();

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
