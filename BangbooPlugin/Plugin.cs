using BangbooPlugin.Utils;
using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;

namespace BangbooPlugin
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "BangbooPlugin";

        [PluginService] public static IDalamudPluginInterface PluginInterface { get; set; }
        [PluginService] public static IFramework Framework { get; set; }
        [PluginService] public static ISigScanner SigScanner { get; set; }
        [PluginService] public static IChatGui Chat { get; set; }
        [PluginService] public static IPluginLog PluginLog { get; set; }
        public static PenumbraIPC PenumbraApi { get; set; }
        public static GlamourerIPC GlamourerApi { get; set; }
        
        public static Configuration PluginConfig { get; set; }

        public Plugin(IDalamudPluginInterface pluginInterface, IChatGui chat, ICommandManager commands, IFramework framework, ISigScanner sigScanner)
        {
            PluginInterface = pluginInterface;
            Chat = chat;
            Framework = framework;
            SigScanner = sigScanner;

            // Get or create a configuration object
            PluginConfig = (Configuration)PluginInterface.GetPluginConfig() ?? new Configuration();
            PluginConfig.Initialize(PluginInterface);

            PenumbraApi = new PenumbraIPC(pluginInterface);
            GlamourerApi = new GlamourerIPC(pluginInterface);
        }

        private void Log(string Message)
        {
            Chat.Print(Message);
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);

            //Marshal.FreeHGlobal(textPtr);
        }

        #endregion IDisposable Support
    }
}