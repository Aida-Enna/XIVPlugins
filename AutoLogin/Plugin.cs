using AutoLogin.Windows;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Memory;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Veda;
using Veda.Windows;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;
using World = Lumina.Excel.Sheets.World;

namespace AutoLogin
{
    public unsafe class Plugin : IDalamudPlugin
    {
        public string Name => "AutoLogin";

        [PluginService] public static IDalamudPluginInterface PluginInterface { get; set; }
        [PluginService] public static ICommandManager Commands { get; set; }
        [PluginService] public static ICondition Conditions { get; set; }
        [PluginService] public static IDataManager Data { get; set; }
        [PluginService] public static IFramework Framework { get; set; }
        [PluginService] public static IGameGui GameGui { get; set; }
        [PluginService] public static IKeyState KeyState { get; set; }
        [PluginService] public static IChatGui Chat { get; set; }
        [PluginService] public static INotificationManager NotificationManager { get; set; }
        [PluginService] public static IPluginLog PluginLog { get; set; }
        [PluginService] public static IToastGui ToastyGoodness { get; set; }
        [PluginService] public static IGameInteropProvider Hook { get; set; }
        [PluginService] public static ISigScanner SigScanner { get; set; }
        [PluginService] public static IClientState ClientState { get; set; }

        public static Configuration PluginConfig { get; set; }
        private PluginCommandManager<Plugin> commandManager;
        private bool hasDoneLogin;
        public bool ReloggingFromDisconnect = false;
        public static Queue<Func<bool>> actionQueue = new();
        private readonly Stopwatch sw = new();
        private static uint Delay = 0;
        public static Notification NotifObject = new Notification();

        private static uint? tempDc = null;
        private static uint? tempWorld = null;
        private static uint? tempCharacter = null;

        internal IntPtr LobbyErrorHandler;

        private delegate char LobbyErrorHandlerDelegate(Int64 a1, Int64 a2, Int64 a3);

        private Hook<LobbyErrorHandlerDelegate> LobbyErrorHandlerHook;

        public readonly WindowSystem WindowSystem = new("AutoLogin");
        private ConfigWindow ConfigWindow { get; init; }
        private MessageBoxWindow MessageBoxWindow { get; init; }

        public Plugin(IDalamudPluginInterface pluginInterface, IToastGui ToastGui, ICommandManager commands)
        {
            PluginConfig = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            PluginConfig.Initialize(PluginInterface);

            ConfigWindow = new ConfigWindow(this);

            MessageBoxWindow = new MessageBoxWindow();

            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MessageBoxWindow);

            PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
            pluginInterface.UiBuilder.OpenConfigUi += ConfigWindow.Toggle;

            NotifObject.Title = "Auto Login";

            this.commandManager = new PluginCommandManager<Plugin>(this, commands);

            Framework.Update += OnFrameworkUpdate;
            ToastyGoodness = ToastGui;
            ToastyGoodness.ErrorToast += OnToastShown;
            ClientState.Logout += Logout;

            //Thank you NoKillPlugin
            this.LobbyErrorHandler = SigScanner.ScanText("40 53 48 83 EC 30 48 8B D9 49 8B C8 E8 ?? ?? ?? ?? 8B D0");
            this.LobbyErrorHandlerHook = Hook.HookFromAddress<LobbyErrorHandlerDelegate>(
                LobbyErrorHandler,
                new LobbyErrorHandlerDelegate(LobbyErrorHandlerDetour)
            );

            this.LobbyErrorHandlerHook.Enable();

            if (!PluginConfig.SeenReconnectionExplanation)
            {
                MessageBoxWindow.MessageBoxText = "The Auto Login plugin now supports automatically reconnecting once it detects that you have been suddenly disconnected from\nthe game due to a network error (such a DDoS or server side issues). You can also (separately) have it send a Discord webhook\nmessage when it detects you've been disconnected.\n\nThis reconnection behavior is enabled by default and can be disabled/configured in the settings menu. This window is a one-time\nnotification to explain this behavior and will be removed in a future update.\n\nEnjoy!";
                MessageBoxWindow.Toggle();
                PluginConfig.SeenReconnectionExplanation = true;
                Plugin.PluginConfig.Save();
            }
        }

        [Command("/autologin")]
        [HelpMessage("Open config window")]
        public void ToggleConfig(string command, string args)
        {
            ConfigWindow.Toggle();
        }

        [Command("/swapcharacter")]
        [HelpMessage("Swap character\nUsage: /swapcharacter WorldName CharacterIndex")]
        public void SwapCharacter(string command, string arguments)
        {
            var args = arguments.Split(' ');

            void ShowHelp() => Chat.PrintError("Usage: /swapcharacter WorldName CharacterIndex");

            if (args.Length != 2)
            {
                ShowHelp();
                return;
            }

            var world = Data.Excel.GetSheet<World>()?.FirstOrDefault(w => w.Name.ToDalamudString().TextValue.Equals(args[0], StringComparison.InvariantCultureIgnoreCase));

            if (world == null)
            {
                Chat.PrintError($"'{args[0]}' is not a valid world name.");
                ShowHelp();
                return;
            }

            //Disabled in case they're doing a DC/World visit
            if (!uint.TryParse(args[1], out var characterIndex))
            {
                Chat.PrintError("Invalid Character Index. Please make sure to choose a number that matches your character slot with 0 being the first character.");
                ShowHelp();
                return;
            }

            tempDc = world.Value.DataCenter.RowId;
            tempWorld = world.Value.RowId;
            tempCharacter = characterIndex;
            actionQueue.Clear();
            actionQueue.Enqueue(VariableDelay(5));
            actionQueue.Enqueue(Logout);
            actionQueue.Enqueue(SelectYes);
            actionQueue.Enqueue(VariableDelay(5));
            actionQueue.Enqueue(OpenDataCenterMenu);
            actionQueue.Enqueue(SelectDataCentre);
            actionQueue.Enqueue(SelectWorld);
            actionQueue.Enqueue(VariableDelay(10));
            actionQueue.Enqueue(SelectCharacter);
            actionQueue.Enqueue(SelectYes);
            actionQueue.Enqueue(Delay5s);
            actionQueue.Enqueue(ClearTemp);
        }

        private void Logout(int type, int code)
        {
            //Sudden disconnect code: 90002 | Type 2
            //Sudden disconnect code 2?: 90001 | Type 2
            PluginLog.Debug("Logged out! Code: " + code + " | Type: " + type);
            if (type == 2 && (code == 90002 || code == 90001))
            {
                PluginLog.Debug("We've been disconnected from the server!");
                if (PluginConfig.SendNotif)
                {
                    if (PluginConfig.WebhookURL.StartsWith("https://discord.com/api/webhooks/") && string.IsNullOrWhiteSpace(PluginConfig.WebhookMessage) == false)
                    {
                        Functions.SendDiscordWebhookAsync(PluginConfig.WebhookURL, PluginConfig.WebhookMessage);
                    }
                }
                //Reset the login so it logins in again after we click the error
                if (PluginConfig.RelogAfterDisconnect)
                {
                    NotifObject.Content = "Sudden disconnect detected, beginning re-login process.";
                    NotifObject.Title = "";
                    NotifObject.Type = NotificationType.Error;
                    NotifObject.InitialDuration = TimeSpan.FromSeconds(15);
                    NotificationManager.AddNotification(NotifObject).Minimized = false;
                    hasDoneLogin = false;
                    ReloggingFromDisconnect = true;
                }
            }
        }

        //Shamelessly ripped from Bluefissure's NoKill plugin (https://github.com/Bluefissure/NoKillPlugin/)
        private char LobbyErrorHandlerDetour(Int64 a1, Int64 a2, Int64 a3)
        {
            IntPtr p3 = new IntPtr(a3);
            var t1 = Marshal.ReadByte(p3);
            var v4 = ((t1 & 0xF) > 0) ? (uint)Marshal.ReadInt32(p3 + 8) : 0;
            UInt16 v4_16 = (UInt16)(v4);
            //PluginLog.Debug($"LobbyErrorHandler a1:{a1} a2:{a2} a3:{a3} t1:{t1} v4:{v4_16}");
            if (v4 > 0)
            {
                if (v4_16 == 0x332C) // Auth failed
                {
                    //PluginLog.Debug($"Skip Auth Error");
                }
                else
                {
                    Marshal.WriteInt64(p3 + 8, 0x3E80); // server connection lost
                    // 0x3390: maintenance
                    v4 = ((t1 & 0xF) > 0) ? (uint)Marshal.ReadInt32(p3 + 8) : 0;
                    v4_16 = (UInt16)(v4);
                }
            }
            //PluginLog.Debug($"After LobbyErrorHandler a1:{a1} a2:{a2} a3:{a3} t1:{t1} v4:{v4_16}");
            return this.LobbyErrorHandlerHook.Original(a1, a2, a3);
        }

        private void OnLogin()
        {
            if (PluginConfig.DataCenter != 0 && PluginConfig.World != 0)
            {
                hasDoneLogin = true;
                if (ReloggingFromDisconnect)
                {
                    NotifObject.Content = "logging in again - Hold SPACE to cancel.";
                }
                else
                {
                    NotifObject.Content = "Auto logging in - Hold SPACE to cancel.";
                }
                NotifObject.Type = NotificationType.Info;
                NotificationManager.AddNotification(NotifObject);
                actionQueue.Enqueue(OpenDataCenterMenu);
                actionQueue.Enqueue(SelectDataCentre);
                actionQueue.Enqueue(SelectWorld);
                actionQueue.Enqueue(VariableDelay(10));
                actionQueue.Enqueue(SelectCharacter);
                actionQueue.Enqueue(SelectYes);
            }
        }

        private Func<bool> VariableDelay(uint frameDelay)
        {
            return () =>
            {
                Delay = frameDelay;
                return true;
            };
        }

        private void OnToastShown(ref SeString message, ref bool isHandled)
        {
            //Don't log toasts if we're not doing something with the plugin
            if (actionQueue.Count == 0) { return; }
            //Generic error message
            if (message.TextValue.Contains("Character is currently visiting"))
            {
                //NotifObject.Title = "Error";
                NotifObject.Title = "";
                NotifObject.Content = "AutoLogin has been cancelled because your character is visiting another data center.";
                NotifObject.Type = NotificationType.Error;
                NotifObject.InitialDuration = TimeSpan.FromSeconds(15);
                NotificationManager.AddNotification(NotifObject).Minimized = false;
                actionQueue.Clear();
                return;
            }
            if (message.TextValue.Contains("Unable to execute command"))
            {
                NotifObject.Title = "";
                NotifObject.Content = "AutoLogin has been cancelled due to an unknown error.\n\"" + message.TextValue + "\"";
                NotifObject.Type = NotificationType.Error;
                NotifObject.InitialDuration = TimeSpan.FromSeconds(15);
                NotificationManager.AddNotification(NotifObject).Minimized = false;
                actionQueue.Clear();
                return;
            }
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            if (ReloggingFromDisconnect)
            {
                var AddonTest = (AtkUnitBase*)GameGui.GetAddonByName("Dialogue", 1).Address;
                if (AddonTest == null || AddonTest->IsVisible == false)
                {
                    ReloggingFromDisconnect = false;
                    //Do nothing
                }
                else
                {
                    PluginLog.Debug("Found Dialogue addon (Disconnection message hopefully!) - Trying to hit OK!");
                    AddonTest->GetComponentButtonById(4)->ClickAddonButton(AddonTest);
                    PluginLog.Debug("Hit OK!");
                }
            }
            if (!hasDoneLogin && PluginConfig.DataCenter != 0 && PluginConfig.World != 0)
            {
                var addon = (AtkUnitBase*)GameGui.GetAddonByName("_TitleMenu").Address;
                if (addon == null || addon->IsVisible == false)
                    return;

                OnLogin();
            }
            if (actionQueue.Count == 0)
            {
                if (sw.IsRunning) sw.Stop();
                return;
            }
            if (!sw.IsRunning) sw.Restart();

            if (KeyState[VirtualKey.SPACE])
            {
                NotifObject.Content = "AutoLogin cancelled.";
                NotifObject.Type = NotificationType.Warning;
                NotificationManager.AddNotification(NotifObject);
                actionQueue.Clear();
            }

            if (Delay > 0)
            {
                Delay -= 1;
                return;
            }

            if (sw.ElapsedMilliseconds > 30000)
            {
                actionQueue.Clear();
                return;
            }

            try
            {
                var hasNext = actionQueue.TryPeek(out var next);
                if (hasNext)
                {
                    if (next())
                    {
                        actionQueue.Dequeue();
                        sw.Reset();
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Failed: {ex.Message}");
            }
        }

        public static bool OpenDataCenterMenu()
        {
            var addon = (AtkUnitBase*)GameGui.GetAddonByName("_TitleMenu").Address;
            if (addon == null || addon->IsVisible == false) return false;
            PluginLog.Debug("Found Title Screen");
            GenerateCallback(addon, 5);
            var nextAddon = (AtkUnitBase*)GameGui.GetAddonByName("TitleDCWorldMap").Address;
            if (nextAddon == null) return false;
            PluginLog.Debug("Found TitleDCWorldMap");
            return true;
        }

        public static bool SelectDataCentre()
        {
            var addon = (AtkUnitBase*)GameGui.GetAddonByName("TitleDCWorldMap", 1).Address;
            if (addon == null) return false;
            PluginLog.Debug("Found TitleDCWorldMap");
            var dcMenu = (AtkUnitBase*)GameGui.GetAddonByName("TitleDCWorldMap").Address;
            if (dcMenu != null) dcMenu->Hide(true, true, 0);
            var dcMenuBG = (AtkUnitBase*)GameGui.GetAddonByName("TitleDCWorldMapBg").Address;
            if (dcMenuBG != null) dcMenuBG->Hide(true, true, 0);
            GenerateCallback(addon, 17, (int)(tempDc ?? PluginConfig.DataCenter));
            dcMenu->Close(true);
            return true;
        }

        public static bool SelectWorld()
        {
            // Select World
            var dcMenu = (AtkUnitBase*)GameGui.GetAddonByName("TitleDCWorldMap").Address;
            if (dcMenu != null) dcMenu->Hide(true, true, 0);
            var dcMenuBG = (AtkUnitBase*)GameGui.GetAddonByName("TitleDCWorldMapBg").Address;
            if (dcMenuBG != null) dcMenuBG->Hide(true, true, 0);
            var addon = (AtkUnitBase*)GameGui.GetAddonByName("_CharaSelectWorldServer", 1).Address;
            if (addon == null) return false;
            PluginLog.Debug("Found World Server");
            var stringArray = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->UIModule->GetRaptureAtkModule()->AtkModule.AtkArrayDataHolder.StringArrays[1];
            if (stringArray == null) return false;

            var world = Data.Excel.GetSheet<World>()?.GetRow(tempWorld ?? PluginConfig.World);
            if (world is not { IsPublic: true }) return false;

            var checkedWorldCount = 0;

            for (var i = 0; i < 16; i++)
            {
                var n = stringArray->StringArray[i];
                if (n == null) continue;
                var s = MemoryHelper.ReadStringNullTerminated(new IntPtr(n));
                if (s.Trim().Length == 0) continue;
                checkedWorldCount++;
                if (s != world.Value.Name.ToString()) continue;
                PluginLog.Debug("Found world [" + world.Value.ToString() + "] at integer i[" + i + "]");
                GenerateCallback(addon, 24, 0, i);
                return true;
            }

            if (checkedWorldCount > 0) actionQueue.Clear();
            return false;
        }

        public static bool SelectCharacter()
        {
            // Select Character
            var addon = (AtkUnitBase*)GameGui.GetAddonByName("_CharaSelectListMenu", 1).Address;
            if (addon == null) return false;
            PluginLog.Debug("Found _CharaSelectListMenu");
            GenerateCallback(addon, 29, 0, tempCharacter ?? PluginConfig.CharacterSlot);
            var nextAddon = (AtkUnitBase*)GameGui.GetAddonByName("SelectYesno", 1).Address;
            return nextAddon != null;
        }

        public static bool SelectYes()
        {
            var addon = (AtkUnitBase*)GameGui.GetAddonByName("SelectYesno", 1).Address;
            if (addon == null) return false;
            GenerateCallback(addon, 0);
            addon->Hide(true, false, 0);
            return true;
        }

        public static bool Delay5s()
        {
            Delay = 300;
            return true;
        }

        public static bool Delay1s()
        {
            Delay = 60;
            return true;
        }

        public static bool Logout()
        {
            var isLoggedIn = Conditions.Any();
            if (!isLoggedIn) return true;

            FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUIModule()->ExecuteMainCommand(23);
            return true;
        }

        public static bool ClearTemp()
        {
            tempWorld = null;
            tempDc = null;
            tempCharacter = null;
            return true;
        }

        public static void GenerateCallback(AtkUnitBase* unitBase, params object[] values)
        {
            if (unitBase == null) throw new Exception("Null UnitBase");
            var atkValues = (AtkValue*)Marshal.AllocHGlobal(values.Length * sizeof(AtkValue));
            if (atkValues == null) return;
            try
            {
                for (var i = 0; i < values.Length; i++)
                {
                    var v = values[i];
                    switch (v)
                    {
                        case uint uintValue:
                            atkValues[i].Type = ValueType.UInt;
                            atkValues[i].UInt = uintValue;
                            break;

                        case int intValue:
                            atkValues[i].Type = ValueType.Int;
                            atkValues[i].Int = intValue;
                            break;

                        case float floatValue:
                            atkValues[i].Type = ValueType.Float;
                            atkValues[i].Float = floatValue;
                            break;

                        case bool boolValue:
                            atkValues[i].Type = ValueType.Bool;
                            atkValues[i].Byte = (byte)(boolValue ? 1 : 0);
                            break;

                        case string stringValue:
                            {
                                atkValues[i].Type = ValueType.String;
                                var stringBytes = Encoding.UTF8.GetBytes(stringValue);
                                var stringAlloc = Marshal.AllocHGlobal(stringBytes.Length + 1);
                                Marshal.Copy(stringBytes, 0, stringAlloc, stringBytes.Length);
                                Marshal.WriteByte(stringAlloc, stringBytes.Length, 0);
                                atkValues[i].String = (byte*)stringAlloc;
                                break;
                            }
                        default:
                            throw new ArgumentException($"Unable to convert type {v.GetType()} to AtkValue");
                    }
                }

                unitBase->FireCallback((uint)values.Length, atkValues);
            }
            finally
            {
                for (var i = 0; i < values.Length; i++)
                {
                    if (atkValues[i].Type == ValueType.String)
                    {
                        Marshal.FreeHGlobal(new IntPtr(atkValues[i].String));
                    }
                }
                Marshal.FreeHGlobal(new IntPtr(atkValues));
            }
        }

        public void Dispose()
        {
            PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
            PluginInterface.UiBuilder.OpenConfigUi -= ConfigWindow.Toggle;

            WindowSystem.RemoveAllWindows();

            ConfigWindow.Dispose();
            MessageBoxWindow.Dispose();

            Commands.RemoveHandler("/autologin");
            Commands.RemoveHandler("/swapcharacter");
        }
    }
}