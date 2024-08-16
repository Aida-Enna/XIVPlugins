using CustomSounds.Services;
using CustomSounds.Utility;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Veda;

namespace CustomSounds
{
    //A large part of this code was lifted directly from StanleyParableXIV, go check them out! https://github.com/rekyuu/StanleyParableXiv
    public class Plugin : IDalamudPlugin
    {
        public string Name => "CustomSounds";

        [PluginService] public static IDalamudPluginInterface PluginInterface { get; set; }
        [PluginService] public static ICommandManager Commands { get; set; }
        [PluginService] public static IChatGui Chat { get; set; }
        [PluginService] public static IPluginLog Log { get; set; }
        [PluginService] public static INotificationManager NotificationManager { get; set; }
        [PluginService] public static IFramework Framework { get; set; }

        private PluginCommandManager<Plugin> commandManager;
        private PluginUI ui;
        public static Configuration PluginConfig { get; set; }

        private uint _lastXivVolumeSource = 0;
        private uint _lastXivMasterVolume = 0;

        public Plugin(IDalamudPluginInterface pluginInterface, IChatGui chat, ICommandManager commands)
        {
            PluginInterface = pluginInterface;
            Chat = chat;

            // Get or create a configuration object
            PluginConfig = (Configuration)PluginInterface.GetPluginConfig() ?? new Configuration();
            PluginConfig.Initialize(PluginInterface);

            Chat.ChatMessage += this.OnChatMessage;

            ui = new PluginUI();
            PluginInterface.UiBuilder.Draw += new System.Action(ui.Draw);
            PluginInterface.UiBuilder.OpenConfigUi += () =>
            {
                PluginUI ui = this.ui;
                ui.IsVisible = !ui.IsVisible;
            };

            Commands.AddHandler("/soundconfig", new CommandInfo(OnConfigCommand)
            {
                HelpMessage = "Opens the Stanley Parable XIV configuration."
            });

            Commands.AddHandler("/soundvolume", new CommandInfo(OnVolumeCommand)
            {
                HelpMessage = "Sets the volume for the Narrator."
            });

            Commands.AddHandler("/soundtest", new CommandInfo(OnTestCommand)
            {
                ShowInHelp = false
            });

            Commands.AddHandler("/soundconfigreload", new CommandInfo(OnConfigReload)
            {
                ShowInHelp = false
            });

            // Update assets.
            UpdateVoiceLines();

            Framework.Update += OnFrameworkUpdate;

            // Load all of our commands
            this.commandManager = new PluginCommandManager<Plugin>(this, commands);

            ui.IsVisible = true;
        }

        private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            try
            {
                if (isHandled)
                {
                    return;
                }
                var ChatMessage = message.Payloads.FirstOrDefault(x => x is TextPayload) as TextPayload;
                var playerPayload = sender.Payloads.SingleOrDefault(x => x is PlayerPayload) as PlayerPayload;
                if (ChatMessage.Text.Contains("??"))
                {
                    Chat.Print("Playing league ping");
                    AudioPlayer.Instance.PlaySoundSimple("lol_ping");
                }
                if (Regex.Match(ChatMessage.Text, "\\boof\\b|\\boof$").Success)
                {
                    Chat.Print("Playing Roblox OOF");
                    AudioPlayer.Instance.PlaySoundSimple("oof");
                }
            }
            catch (Exception f)
            {
                // Ignore exception
            }
        }

        public static void UpdateVoiceLines(bool force = false)
        {
            try
            {
                Task.Run(() => AssetsManager.UpdateVoiceLines(force));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception occurred while updating assets");
            }
        }

        private void OnConfigCommand(string command, string commandArgs) => ui.IsVisible = !ui.IsVisible;

        private static void OnVolumeCommand(string command, string commandArgs)
        {
            try
            {
                uint volumeSetting = 0;
                string[] args = commandArgs.Split(" ");

                if (args.Length >= 1 && !string.IsNullOrEmpty(args[0])) volumeSetting = uint.Parse(args[0]);
                if (volumeSetting > 100) throw new Exception("Volume must be between 0 and 100");

                Configuration.Instance.Volume = volumeSetting;
                Configuration.Instance.BindToXivVolumeSource = false;
                Configuration.Instance.Save();

                AudioPlayer.Instance.UpdateVolume();

                Chat.Print($"Narrator volume set to {volumeSetting}.");
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Exception occurred while setting volume via command");
                Chat.PrintError($"\"{commandArgs}\" is not a valid setting.");
            }
        }

        private static void OnTestCommand(string command, string commandArgs)
        {
            // Sorry, nothing
        }

        private static void OnConfigReload(string command, string arguments) => Configuration.Reload();

        private void OnFrameworkUpdate(IFramework framework)
        {
            // Updates the mixer volume when bound to an FFXIV volume source when changed.
            if (!Configuration.Instance.BindToXivVolumeSource) return;

            uint nextVolumeSource = XivUtility.GetVolume(Configuration.Instance.XivVolumeSource);
            uint nextMasterVolume = XivUtility.GetVolume(XivVolumeSource.Master);

            if (_lastXivVolumeSource == nextVolumeSource && _lastXivMasterVolume == nextMasterVolume) return;

            Log.Debug("Updating volume due to framework update");
            AudioPlayer.Instance.UpdateVolume();

            _lastXivVolumeSource = nextVolumeSource;
            _lastXivMasterVolume = nextMasterVolume;
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            this.commandManager.Dispose();

            PluginInterface.SavePluginConfig(PluginConfig);

            PluginInterface.UiBuilder.Draw -= ui.Draw;
            PluginInterface.UiBuilder.OpenConfigUi -= () =>
            {
                PluginUI ui = this.ui;
                ui.IsVisible = !ui.IsVisible;
            };

            Commands.RemoveHandler("/soundconfig");
            Commands.RemoveHandler("/soundvolume");
            Commands.RemoveHandler("/soundtest");
            Commands.RemoveHandler("/soundconfigreload");

            Framework.Update -= OnFrameworkUpdate;
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