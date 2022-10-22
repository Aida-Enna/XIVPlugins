using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using FATEAutoSync.Attributes;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Runtime.InteropServices;

namespace FATEAutoSync
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "FATE AutoSync";

        private DalamudPluginInterface pluginInterface;
        private Framework framework;
        private SigScanner sigScanner;
        private PluginCommandManager<Plugin> commandManager;
        private Configuration config;

        [PluginService]
        internal static ClientState ClientState { get; private set; }

        public Configuration Configuration { get; init; }

        [PluginService] public static Condition Condition { get; private set; }

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
        private ChatGui chat;
        private bool IsMounted = false;
        private bool TankStanceShouldBeOnBitch = false;

        public Plugin(DalamudPluginInterface pluginInterface, ChatGui chat, Framework framework, CommandManager commands, SigScanner sigScanner)
        {
            this.pluginInterface = pluginInterface;
            this.chat = chat;
            this.framework = framework;
            this.sigScanner = sigScanner;


            config = this.pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            config.Initialize(this.pluginInterface);

            framework.Update += Update;

            commandManager = new PluginCommandManager<Plugin>(this, commands);

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
            if (!this.config.AutoStanceEnabled) return;
            //Check for their class and use the appropriate stance
            string ClassNameAbbr = ClientState.LocalPlayer.ClassJob.GameData.Abbreviation.ToString();
            if (ClassNameAbbr == "PLD" || ClassNameAbbr == "GLA") { ExecuteCommand("/action \"Iron Will\""); }
            if (ClassNameAbbr == "WAR" || ClassNameAbbr == "MRD") { ExecuteCommand("/action \"Defiance\""); }
            if (ClassNameAbbr == "DRK") { ExecuteCommand("/action \"Grit\""); }
            if (ClassNameAbbr == "GNB") { ExecuteCommand("/action \"Royal Guard\""); }
        }

        private void Update(Dalamud.Game.Framework framework)
        {
            if (!this.config.FateAutoSyncEnabled) return;

            var wasInFateArea = inFateArea;
            inFateArea = Marshal.ReadByte(inFateAreaPtr) == 1;

            if (inFateArea && IsMounted)
            {
                if (Condition[ConditionFlag.Mounted] == false && Condition[ConditionFlag.Mounted2] == false)
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
                    if (Condition[ConditionFlag.Mounted] || Condition[ConditionFlag.Mounted2])
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
            this.config.FateAutoSyncEnabled = !this.config.FateAutoSyncEnabled;
            chat.Print($"Toggled auto fate syncing {(this.config.FateAutoSyncEnabled ? "on" : "off")}.");
        }

        [Command("/autostance")]
        [HelpMessage("Toggles auto stancing on/off.")]
        public void ToggleAutoStance(string command, string args)
        {
            this.config.AutoStanceEnabled = !this.config.AutoStanceEnabled;
            chat.Print($"Toggled auto stance syncing {(this.config.AutoStanceEnabled ? "on" : "off")}.");
        }

        // Courtesy of https://github.com/UnknownX7/QoLBar
        private unsafe void InitializePointers()
        {
            // FATE pointer (thanks to Pohky#8008)
            try
            {
                var sig = sigScanner.ScanText("80 3D ?? ?? ?? ?? ?? 0F 84 ?? ?? ?? ?? 48 8B 42 20");
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
                var getUIModulePtr = sigScanner.ScanText("E8 ?? ?? ?? ?? 48 83 7F ?? 00 48 8B F0");
                var easierProcessChatBoxPtr = sigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 45 84 C9");
                var uiModulePtr = sigScanner.GetStaticAddressFromSig("48 8B 0D ?? ?? ?? ?? 48 8D 54 24 ?? 48 83 C1 10 E8");

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
            catch (Exception err) { chat.PrintError(err.Message); }
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            this.commandManager.Dispose();

            this.pluginInterface.SavePluginConfig(this.config);

            framework.Update -= Update;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}