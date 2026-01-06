using Dalamud.Bindings.ImGui;
using Dalamud.Configuration;
using Dalamud.Plugin;
using Lumina.Excel.Sheets;
using System.Linq;
using Veda;

namespace AutoLogin
{
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; }
        private bool ShowSupport;

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            Plugin.PluginInterface = pluginInterface;
        }

        public void Save()
        {
            Plugin.PluginInterface.SavePluginConfig(this);
        }

        public uint DataCenter;
        public uint World;
        public uint CharacterSlot;
        public bool RelogAfterDisconnect = true;
        public bool SendNotif;
        public string WebhookURL = "";
        public string WebhookMessage = "[AutoLogin] Your game has lost connection!";
        public bool SeenReconnectionExplanation = false;
        public bool DebugMode = false;
        public uint LastErrorCode = 0;
    }
}