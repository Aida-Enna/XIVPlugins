using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;

namespace LootMaster
{
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; }

        public bool EnableChatLogMessage = true;
        public bool EnableDelay = true;

        public int LowNum = 500;
        public int HighNum = 750;

        public bool AutoRoll = false;
        public AutoRollOption AutoRollOption = AutoRollOption.NeedThenGreed;
        public bool NotifyOnCFPop = true;
        public bool PassOnFail = false;
        public bool InventoryCheck = true;
        public bool DoNotRollInHighEndDuties = true;

        [JsonIgnore] private IDalamudPluginInterface pluginInterface;

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
