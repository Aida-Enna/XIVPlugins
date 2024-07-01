using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Runtime.InteropServices;
using Veda;

namespace FATEAutoSync
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "FATE AutoSync";

        [PluginService] public static IDalamudPluginInterface PluginInterface { get; set; }
        [PluginService] public static ICommandManager Commands { get; set; }
        [PluginService] public static ICondition Conditions { get; set; }
        [PluginService] public static IDataManager Data { get; set; }
        [PluginService] public static IFramework Framework { get; set; }
        [PluginService] public static IGameGui GameGui { get; set; }
        [PluginService] public static ISigScanner SigScanner { get; set; }
        [PluginService] public static IKeyState KeyState { get; set; }
        [PluginService] public static IChatGui Chat { get; set; }
        [PluginService] internal static IClientState ClientState { get; set; }
        [PluginService] public static IPartyList PartyList { get; set; }
        [PluginService] public static IPluginLog PluginLog { get; set; }

        public static Configuration PluginConfig { get; set; }
        private PluginCommandManager<Plugin> CommandManager;

        // private PluginUI ui;

        // Command execution
        private delegate void ProcessChatBoxDelegate(IntPtr uiModule, IntPtr message, IntPtr unused, byte a4);

        private delegate IntPtr GetUIModuleDelegate(IntPtr basePtr);

        private ProcessChatBoxDelegate? ProcessChatBox;
        private IntPtr uiModule = IntPtr.Zero;

        // Our specific stuff
        private IntPtr inFateAreaPtr = IntPtr.Zero;

        private bool inFateArea = false;
        private bool firstRun = true;
        private bool IsMounted = false;
        private bool TankStanceShouldBeOnBitch = false;

        public Plugin(IDalamudPluginInterface pluginInterface, IChatGui chat, IFramework framework, ICommandManager commands, ISigScanner sigScanner)
        {
            PluginInterface = pluginInterface;
            Chat = chat;
            Framework = framework;
            SigScanner = sigScanner;
            Commands = commands;

            PluginConfig = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            PluginConfig.Initialize(PluginInterface);

            framework.Update += Update;

            CommandManager = new PluginCommandManager<Plugin>(this, commands);

            InitializePointers();
        }

        private bool TankStanceEnabled()
        {
            foreach (var Status in ClientState.LocalPlayer.StatusList)
            {
                if (Status.StatusId == 79) { return true; } //Iron Will
                if (Status.StatusId == 91) { return true; } //Defiance
                if (Status.StatusId == 743) { return true; } //Grit
                if (Status.StatusId == 1833) { return true; } //Royal Guard
            }
            return false;
        }

        private void StanceToggle()
        {
            if (!PluginConfig.AutoStanceEnabled) return;
            //Check for their class and use the appropriate stance
            string ClassNameAbbr = ClientState.LocalPlayer.ClassJob.GameData.Abbreviation.ToString();
            if (ClassNameAbbr == "PLD" || ClassNameAbbr == "GLA") { ExecuteCommand("/action \"Iron Will\""); }
            if (ClassNameAbbr == "WAR" || ClassNameAbbr == "MRD") { ExecuteCommand("/action \"Defiance\""); }
            if (ClassNameAbbr == "DRK") { ExecuteCommand("/action \"Grit\""); }
            if (ClassNameAbbr == "GNB") { ExecuteCommand("/action \"Royal Guard\""); }
        }

        private void Update(IFramework framework)
        {
            if (!PluginConfig.FateAutoSyncEnabled) return;

            var wasInFateArea = inFateArea;
            inFateArea = Marshal.ReadByte(inFateAreaPtr) == 1;

            if (inFateArea && IsMounted)
            {
                if (Conditions[ConditionFlag.Mounted] == false && Conditions[ConditionFlag.Mounted2] == false)
                {
                    StanceToggle();
                    IsMounted = false;
                }
            }
            if (inFateArea && TankStanceShouldBeOnBitch && TankStanceEnabled() == false)
            {
                StanceToggle();
            }

            if (wasInFateArea != inFateArea)
            {
                if (inFateArea)
                {
                    if (firstRun)
                    {
                        //chat.Print("FATE Auto Sync ran for the first time in this session (/fateautosync to toggle)");
                        firstRun = false;
                    }
                    ExecuteCommand("/levelsync on");
                    if (Conditions[ConditionFlag.Mounted] || Conditions[ConditionFlag.Mounted2])
                    {
                        IsMounted = true;
                    }
                    else
                    {
                        if (TankStanceEnabled())
                        {
                            TankStanceShouldBeOnBitch = true;
                        }
                        StanceToggle();
                    }
                }
            }
        }

        [Command("/autofate")]
        [HelpMessage("Toggles auto fate syncing on/off.")]
        public void ToggleAutoFate(string command, string args)
        {
            PluginConfig.FateAutoSyncEnabled = !PluginConfig.FateAutoSyncEnabled;
            Chat.Print($"Toggled auto fate syncing {(PluginConfig.FateAutoSyncEnabled ? "on" : "off")}.");
        }

        [Command("/autostance")]
        [HelpMessage("Toggles auto stancing on/off.")]
        public void ToggleAutoStance(string command, string args)
        {
            PluginConfig.AutoStanceEnabled = !PluginConfig.AutoStanceEnabled;
            Chat.Print($"Toggled auto stance syncing {(PluginConfig.AutoStanceEnabled ? "on" : "off")}.");
        }

        // Courtesy of https://github.com/UnknownX7/QoLBar
        private unsafe void InitializePointers()
        {
            // FATE pointer
            try
            {
                var sig = SigScanner.ScanText("80 3D ?? ?? ?? ?? ?? 0F 84 ?? ?? ?? ?? 48 8B 42 20");
                inFateAreaPtr = sig + Marshal.ReadInt32(sig, 2) + 7;
                //chat.Print("Retrieved 'inFateAreaPtr' successfully");
                //chat.Print(inFateAreaPtr.ToString("X8"));
            }
            catch
            {
                PluginLog.Error("Failed loading 'inFateAreaPtr'");
            }

            // for ExecuteCommand
            try
            {
                var getUIModulePtr = SigScanner.ScanText("E8 ?? ?? ?? ?? 48 83 7F ?? 00 48 8B F0");
                var easierProcessChatBoxPtr = SigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 45 84 C9");
                var uiModulePtr = SigScanner.GetStaticAddressFromSig("48 8B 0D ?? ?? ?? ?? 48 8D 54 24 ?? 48 83 C1 10 E8");

                var GetUIModule = Marshal.GetDelegateForFunctionPointer<GetUIModuleDelegate>(getUIModulePtr);

                uiModule = GetUIModule(*(IntPtr*)uiModulePtr);
                ProcessChatBox = Marshal.GetDelegateForFunctionPointer<ProcessChatBoxDelegate>(easierProcessChatBoxPtr);
            }
            catch { PluginLog.Error("Failed loading 'ExecuteCommand'"); }
        }

        public void ExecuteCommand(string command)
        {
            try
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(command);

                var mem1 = Marshal.AllocHGlobal(400);
                var mem2 = Marshal.AllocHGlobal(bytes.Length + 30);

                Marshal.Copy(bytes, 0, mem2, bytes.Length);
                Marshal.WriteByte(mem2 + bytes.Length, 0);
                Marshal.WriteInt64(mem1, mem2.ToInt64());
                Marshal.WriteInt64(mem1 + 8, 64);
                Marshal.WriteInt64(mem1 + 8 + 8, bytes.Length + 1);
                Marshal.WriteInt64(mem1 + 8 + 8 + 8, 0);

                ProcessChatBox(uiModule, mem1, IntPtr.Zero, 0);

                Marshal.FreeHGlobal(mem1);
                Marshal.FreeHGlobal(mem2);
            }
            catch (Exception err) { Chat.PrintError(err.Message); }
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            CommandManager.Dispose();

            PluginInterface.SavePluginConfig(PluginConfig);

            Framework.Update -= Update;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}