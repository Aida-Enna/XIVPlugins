using CustomSounds.Utility;
using Dalamud.Configuration;
using Dalamud.Plugin;
using System.Collections.Generic;

namespace CustomSounds
{
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; }

        public static Configuration Instance { get; private set; } = Plugin.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();


        /// <summary>
        /// The manual volume set by the user.
        /// </summary>
        public uint Volume { get; set; } = 50;

        /// <summary>
        /// Binds the volume to an FFXIV sound channel.
        /// </summary>
        public bool BindToXivVolumeSource { get; set; } = true;

        /// <summary>
        /// The FFXIV sound channel to bind to.
        /// </summary>
        public XivVolumeSource XivVolumeSource { get; set; } = XivVolumeSource.Voice;

        /// <summary>
        /// The amount of volume boosting to apply to the bound FFXIV sound channel.
        /// </summary>
        public uint XivVolumeSourceBoost { get; set; } = 100;

        /// <summary>
        /// The file type of assets to use.
        /// </summary>
        public AssetsFileType AssetsFileType { get; set; } = AssetsFileType.Mp3;

        /// <summary>
        /// Enables AFK sound events.
        /// </summary>
        public bool EnableAfkEvent { get; set; } = true;

        /// <summary>
        /// Enables the countdown start event.
        /// </summary>
        public bool EnableCountdownStartEvent { get; set; } = true;

        /// <summary>
        /// Enables the countdown event when 10 seconds remain.
        /// </summary>
        public bool EnableCountdown10Event { get; set; } = true;

        /// <summary>
        /// Enables the event on starting duties.
        /// </summary>
        public bool EnableDutyStartEvent { get; set; } = true;

        /// <summary>
        /// Enables the duty completion event.
        /// </summary>
        public bool EnableDutyCompleteEvent { get; set; } = true;

        /// <summary>
        /// Enables the party wipe event.
        /// </summary>
        public bool EnableDutyPartyWipeEvent { get; set; } = true;

        /// <summary>
        /// Enables the event when leaving the duty before completion.
        /// </summary>
        public bool EnableDutyFailedEvent { get; set; } = true;

        /// <summary>
        /// Enables the player disconnect event.
        /// </summary>
        public bool EnableDutyPlayerDisconnectedEvent { get; set; } = true;

        /// <summary>
        /// Enables the player reconnect event.
        /// </summary>
        public bool EnableDutyPlayerReconnectedEvent { get; set; } = true;

        /// <summary>
        /// Enables the countdown start event in PvP.
        /// </summary>
        public bool EnablePvpCountdownStartEvent { get; set; } = true;

        /// <summary>
        /// Enables the countdown event when 10 seconds remain in PvP.
        /// </summary>
        public bool EnablePvpCountdown10Event { get; set; } = true;

        /// <summary>
        /// Enables the event when the first player dies in PvP.
        /// </summary>
        public bool EnablePvpFirstBloodEvent { get; set; } = true;

        /// <summary>
        /// Enables PvP kill streaks.
        /// </summary>
        public bool EnablePvpKillStreaksEvent { get; set; } = true;

        /// <summary>
        /// Enables PvP multikill streaks.
        /// </summary>
        public bool EnablePvpMultikillsEvent { get; set; } = true;

        /// <summary>
        /// Enables the event when PvP starts.
        /// </summary>
        public bool EnablePvpPrepareEvent { get; set; } = true;

        /// <summary>
        /// Enables the PvP win event.
        /// </summary>
        public bool EnablePvpWinEvent { get; set; } = true;

        /// <summary>
        /// Enables the PvP loss event.
        /// </summary>
        public bool EnablePvpLossEvent { get; set; } = true;

        /// <summary>
        /// Enables chat output for PvP kill events.
        /// </summary>
        public bool EnablePvpChatEvent { get; set; } = true;

        /// <summary>
        /// Enables the user login event.
        /// </summary>
        public bool EnableLoginEvent { get; set; } = true;

        /// <summary>
        /// Enables the market board purchase event.
        /// </summary>
        public bool EnableMarketBoardPurchaseEvent { get; set; } = true;

        /// <summary>
        /// Enables the user respawn event.
        /// </summary>
        public bool EnableRespawnEvent { get; set; } = true;

        /// <summary>
        /// Enables the user crafting failure event.
        /// </summary>
        public bool EnableSynthesisFailedEvent { get; set; } = true;

        /// <summary>
        /// Enables multikill announcements for high end duties.
        /// </summary>
        public bool EnableBossKillStreaks { get; set; } = true;

        /// <summary>
        /// Enables debug logging to the Dalamud log window.
        /// </summary>
        public bool EnableDebugLogging { get; set; } = false;

        /// <summary>
        /// Counts of the user's completed high end duties.
        /// </summary>
        public Dictionary<uint, uint> CompletedHighEndDuties { get; set; } = new();

        /// <summary>
        /// Saves the user configuration.
        /// </summary>
        public void Save()
        {
            Plugin.PluginInterface.SavePluginConfig(this);
        }

        /// <summary>
        /// Reloads the configuration.
        /// </summary>
        public static void Reload()
        {
            Instance = Plugin.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Plugin.Chat.Print("Custom Sounds config reloaded.");
        }

        private IDalamudPluginInterface pluginInterface;

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }
    }
}
