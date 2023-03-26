using Dalamud.ContextMenu;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Runtime.InteropServices;
using Veda;

namespace RightClickExtender
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "RightClickExtender";

        [PluginService] public static DalamudPluginInterface PluginInterface { get; set; }
        [PluginService] public static CommandManager Commands { get; set; }
        [PluginService] public static Dalamud.Game.ClientState.Conditions.Condition Conditions { get; set; }
        [PluginService] public static DataManager Data { get; set; }
        [PluginService] public static Dalamud.Game.Framework Framework { get; set; }
        [PluginService] public static GameGui GameGui { get; set; }
        [PluginService] public static SigScanner SigScanner { get; set; }
        [PluginService] public static KeyState KeyState { get; set; }
        [PluginService] public static ChatGui Chat { get; set; }
        [PluginService] public static ClientState ClientState { get; set; }

        private PluginCommandManager<Plugin> commandManager;
        private PluginUI ui;
        public static Configuration PluginConfig { get; set; }
        private DalamudContextMenu contextMenu = new DalamudContextMenu();

        private delegate void ProcessChatBoxDelegate(IntPtr uiModule, IntPtr message, IntPtr unused, byte a4);

        private delegate IntPtr GetUIModuleDelegate(IntPtr basePtr);

        private ProcessChatBoxDelegate? ProcessChatBox;
        private IntPtr uiModule = IntPtr.Zero;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        private delegate IntPtr CountdownTimer(ulong p1);

        public Plugin(DalamudPluginInterface pluginInterface, ChatGui chat, CommandManager commands, SigScanner sigScanner)
        {
            PluginInterface = pluginInterface;
            Chat = chat;
            SigScanner = sigScanner;

            // Get or create a configuration object
            PluginConfig = (Configuration)PluginInterface.GetPluginConfig() ?? new Configuration();
            PluginConfig.Initialize(PluginInterface);

            ui = new PluginUI();
            PluginInterface.UiBuilder.Draw += new System.Action(ui.Draw);
            PluginInterface.UiBuilder.OpenConfigUi += () =>
            {
                PluginUI ui = this.ui;
                ui.IsVisible = !ui.IsVisible;
            };

            // add event handler
            contextMenu.OnOpenGameObjectContextMenu += OpenGameObjectContextMenu;

            // /tell Ava Starlight@Adamantoise
            // Load all of our commands
            this.commandManager = new PluginCommandManager<Plugin>(this, commands);

            InitializePointers();
        }

        private void Log(string Message)
        {
            Chat.Print(Functions.BuildSeString(this.Name, Message));
        }

        private void OpenGameObjectContextMenu(GameObjectContextMenuOpenArgs args)
        {
            //Chat.Print(args.Text.TextValue);
            Chat.Print(args.ParentAddonName);
            if (args.ObjectWorld == 0 || args.ObjectWorld == 65535 || args.Text.TextValue.Contains(" ") == false)
            {
                //Not a player
                return;
            }
            args.AddCustomItem(
                new GameObjectContextMenuItem(
                        new SeString(
                            new UIForegroundPayload(539),
                            new TextPayload($"{SeIconChar.MouseRightClick.ToIconString()} "),
                            new UIForegroundPayload(0),
                            new TextPayload("Send Tell")),
                        SendTell));
        }

        public void SendTell(GameObjectContextMenuItemSelectedArgs args)
        {
            try
            {
                Chat.Print("Player name: " + args.Text.TextValue);
                Chat.Print("World ID: " + args.ObjectWorld);
                ExecuteCommand("/tell " + args.Text.TextValue + "@" + Data.Excel.GetSheet<World>().GetRow(args.ObjectWorld).Name.RawString);
            }
            catch (Exception f)
            {
                Chat.PrintError(f.ToString());
            }
        }

        [Command("/config")]
        [HelpMessage("Command help message")]
        public void ToggleConfig(string command, string args)
        {
            ui.IsVisible = !ui.IsVisible;
        }
        private unsafe void InitializePointers()
        {
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

        private unsafe void HookCountdownPointer()
        {
            var getUIModulePtr = SigScanner.ScanText("E8 ?? ?? ?? ?? 48 83 7F ?? 00 48 8B F0");
            var easierProcessChatBoxPtr = SigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 45 84 C9");
            var uiModulePtr = SigScanner.GetStaticAddressFromSig("48 8B 0D ?? ?? ?? ?? 48 8D 54 24 ?? 48 83 C1 10 E8");

            var GetUIModule = Marshal.GetDelegateForFunctionPointer<GetUIModuleDelegate>(getUIModulePtr);

            uiModule = GetUIModule(*(IntPtr*)uiModulePtr);
            ProcessChatBox = Marshal.GetDelegateForFunctionPointer<ProcessChatBoxDelegate>(easierProcessChatBoxPtr);
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
            contextMenu.OnOpenGameObjectContextMenu -= OpenGameObjectContextMenu;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}