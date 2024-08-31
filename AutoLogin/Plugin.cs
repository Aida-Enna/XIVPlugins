using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Command;
using Dalamud.Memory;
using Dalamud.Plugin;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;
using Dalamud.IoC;
using Dalamud.Interface;
using ImGuiNET;
using System.Linq;
using Dalamud.Plugin.Services;
using Dalamud.Interface.ImGuiNotification;
using System.Threading;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Gui.Toast;

namespace AutoLogin {
    public unsafe class Plugin : IDalamudPlugin {
        public string Name => "AutoLogin";
        
        [PluginService] public static IDalamudPluginInterface PluginInterface { get; set; }
        [PluginService] public static ICommandManager Commands { get; set; }
        [PluginService] public static ICondition Conditions { get; set; }
        [PluginService] public static IDataManager Data { get; set; }
        [PluginService] public static IFramework Framework { get; set; }
        [PluginService] public static IGameGui GameGui { get; set; }
        [PluginService] public static IKeyState KeyState { get; set; }
        [PluginService] public static IChatGui Chat { get; set; }
        [PluginService] public static INotificationManager NotificationManager{ get; set; }
        [PluginService] public static IPluginLog PluginLog { get; set; }
        [PluginService] public static IToastGui ToastyGoodness { get; set; }

        public static Configuration PluginConfig { get; set; }
        private bool drawConfigWindow;
        private readonly Queue<Func<bool>> actionQueue = new();
        private readonly Stopwatch sw = new();
        private uint Delay = 0;
        public static Notification NotifObject = new Notification();
        
        public Plugin(IDalamudPluginInterface pluginInterface, IToastGui ToastGui) {

            PluginConfig = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            PluginConfig.Initialize(PluginInterface);

            pluginInterface.UiBuilder.Draw += DrawUI;
            pluginInterface.UiBuilder.OpenConfigUi += this.OpenConfigUI;

            NotifObject.Title = "Auto Login";

            Commands.AddHandler("/autologinconfig", new Dalamud.Game.Command.CommandInfo(OnConfigCommandHandler) {
                HelpMessage = $"Open config window for {Name}",
                ShowInHelp = true
            });

            if (Commands.Commands.ContainsKey("/swapcharacter")) {
                Commands.RemoveHandler("/swapcharacter");
            }
            Commands.AddHandler("/swapcharacter", new CommandInfo(OnSwapCharacterCommandHandler) {
                HelpMessage = $"Swap character /swapcharacter WorldName CharacterIndex",
                ShowInHelp = true
            });

            Framework.Update += OnFrameworkUpdate;
            ToastyGoodness = ToastGui;
            ToastyGoodness.ErrorToast += OnToastShown;
            if (PluginConfig.DataCenter != 0 && PluginConfig.World != 0)
            {
                NotifObject.Content = "Auto logging in - Hold SPACE to cancel.";
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

        private void OnSwapCharacterCommandHandler(string command, string arguments) {

            var args = arguments.Split(' ');

            void ShowHelp() => Chat.PrintError("/swapcharacter WorldName CharacterIndex");

            if (args.Length != 2) {
                ShowHelp();
                return;
            }

            var world = Data.Excel.GetSheet<World>()?.FirstOrDefault(w => w.Name.ToDalamudString().TextValue.Equals(args[0], StringComparison.InvariantCultureIgnoreCase));

            if (world == null) {
                Chat.PrintError($"'{args[0]}' is not a valid world name.");
                ShowHelp();
                return;
            }

            //Disabled in case they're doing a DC/World visit
            if (!uint.TryParse(args[1], out var characterIndex)) {
                Chat.PrintError("Invalid Character Index. Please make sure to choose a number that matches your character slot with 0 being the first character.");
                ShowHelp();
                return;
            }

            tempDc = world.DataCenter.Row;
            tempWorld = world.RowId;
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

        private Func<bool> VariableDelay(uint frameDelay) {
            return () => {
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

        private void OnFrameworkUpdate(IFramework framework) {
            if (actionQueue.Count == 0) {
                if (sw.IsRunning) sw.Stop();
                return;
            }
            if (!sw.IsRunning) sw.Restart();

            if (KeyState[VirtualKey.SPACE]) {
                NotifObject.Content = "AutoLogin cancelled.";
                NotifObject.Type = NotificationType.Warning;
                NotificationManager.AddNotification(NotifObject);
                actionQueue.Clear();
            }

            if (Delay > 0) {
                Delay -= 1;
                return;
            }
            
            

            if (sw.ElapsedMilliseconds > 30000) {
                actionQueue.Clear();
                return;
            }

            try {
                var hasNext = actionQueue.TryPeek(out var next);
                if (hasNext) {
                    if (next()) {
                        actionQueue.Dequeue();
                        sw.Reset();
                    }
                }
            } catch (Exception ex){
                PluginLog.Error($"Failed: {ex.Message}");
            }
        }

        public void OnConfigCommandHandler(string command, string args) {
            #if DEBUG
            if (args.ToLowerInvariant() == "debug") {
                drawDebugWindow = true;
                return;
            }
            #endif
            OpenConfigUI();
        }

        private void OpenConfigUI() {
            drawConfigWindow = !drawConfigWindow;
        }

        public bool OpenDataCenterMenu() {
            var addon = (AtkUnitBase*) GameGui.GetAddonByName("_TitleMenu");
            if (addon == null || addon->IsVisible == false) return false;
            PluginLog.Debug("Found Title Screen");
            GenerateCallback(addon, 5); 
            var nextAddon = (AtkUnitBase*) GameGui.GetAddonByName("TitleDCWorldMap");
            if (nextAddon == null) return false;
            PluginLog.Debug("Found TitleDCWorldMap");
            return true;
        }

        public bool SelectDataCentre() {
            var addon = (AtkUnitBase*) GameGui.GetAddonByName("TitleDCWorldMap", 1);
            if (addon == null) return false;
            PluginLog.Debug("Found TitleDCWorldMap");
            var dcMenu = (AtkUnitBase*)GameGui.GetAddonByName("TitleDCWorldMap");
            if (dcMenu != null) dcMenu->Hide(true, true, 0);
            var dcMenuBG = (AtkUnitBase*)GameGui.GetAddonByName("TitleDCWorldMapBg");
            if (dcMenuBG != null) dcMenuBG->Hide(true, true, 0);
            GenerateCallback(addon, 17, (int) (tempDc ?? PluginConfig.DataCenter));
            dcMenu->Close(true);
            return true;
        }

        public bool SelectWorld() {
            // Select World
            var dcMenu = (AtkUnitBase*) GameGui.GetAddonByName("TitleDCWorldMap");
            if (dcMenu != null) dcMenu->Hide(true, true, 0);
            var dcMenuBG = (AtkUnitBase*)GameGui.GetAddonByName("TitleDCWorldMapBg");
            if (dcMenuBG != null) dcMenuBG->Hide(true, true, 0);
            var addon = (AtkUnitBase*) GameGui.GetAddonByName("_CharaSelectWorldServer", 1);
            if (addon == null) return false;
            PluginLog.Debug("Found World Server");
            var stringArray = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->UIModule->GetRaptureAtkModule()->AtkModule.AtkArrayDataHolder.StringArrays[1];
            if (stringArray == null) return false;

            var world = Data.Excel.GetSheet<World>()?.GetRow(tempWorld ?? PluginConfig.World);
            if (world is not { IsPublic: true }) return false;

            var checkedWorldCount = 0;

            for (var i = 0; i < 16; i++) {
                var n = stringArray->StringArray[i];
                if (n == null) continue;
                var s = MemoryHelper.ReadStringNullTerminated(new IntPtr(n));
                if (s.Trim().Length == 0) continue;
                checkedWorldCount++;
                if (s != world.Name.RawString) continue;
                PluginLog.Debug("Found world [" + world.Name.RawString + "] at integer i[" + i + "]");
                GenerateCallback(addon, 24, 0, i);
                return true;
            }

            if (checkedWorldCount > 0) actionQueue.Clear();
            return false;
        }

        public bool SelectCharacter() {
            // Select Character
            var addon = (AtkUnitBase*) GameGui.GetAddonByName("_CharaSelectListMenu", 1);
            if (addon == null) return false;
            PluginLog.Debug("Found _CharaSelectListMenu");
            GenerateCallback(addon, 29, 0, tempCharacter ?? PluginConfig.CharacterSlot);
            var nextAddon = (AtkUnitBase*) GameGui.GetAddonByName("SelectYesno", 1);
            return nextAddon != null;
        }

        public bool SelectYes() {
            var addon = (AtkUnitBase*) GameGui.GetAddonByName("SelectYesno", 1);
            if (addon == null) return false;
            GenerateCallback(addon, 0);
            addon->Hide(true, false, 0);
            return true;
        }


        public bool Delay5s() {
            Delay = 300;
            return true;
        }
        

        public bool Delay1s() {
            Delay = 60;
            return true;
        }
        
        public bool Logout() {
            var isLoggedIn = Conditions.Any();
            if (!isLoggedIn) return true;

            FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUIModule()->ExecuteMainCommand(23);
            return true;
        }

        public bool ClearTemp() {
            tempWorld = null;
            tempDc = null;
            tempCharacter = null;
            return true;
        }
        

        private uint? tempDc = null;
        private uint? tempWorld = null;
        private uint? tempCharacter = null;
        private bool drawDebugWindow = false;
        private void DrawUI() {
            drawConfigWindow = drawConfigWindow && PluginConfig.DrawConfigUI();

            if (!drawDebugWindow) return;
            if (ImGui.Begin($"{this.Name} Debugging", ref drawDebugWindow)) {
                if (ImGui.Button("Open Config")) drawConfigWindow = true;
                if (ImGui.Button("Clear Queue")) {
                    actionQueue.Clear();
                }

                if (ImGui.Button("Test Step: Open Data Centre Menu")) actionQueue.Enqueue(OpenDataCenterMenu);
                if (ImGui.Button($"Test Step: Select Data Center [{PluginConfig.DataCenter}]")) actionQueue.Enqueue(SelectDataCentre);

                if (ImGui.Button($"Test Step: SELECT WORLD [{PluginConfig.World}]")) {
                    actionQueue.Clear();
                    actionQueue.Enqueue(SelectWorld);
                }

                if (ImGui.Button($"Test Step: SELECT CHARACTER [{PluginConfig.CharacterSlot}]")) {
                    actionQueue.Clear();
                    actionQueue.Enqueue(SelectCharacter);
                }

                if (ImGui.Button("Test Step: SELECT YES")) {
                    actionQueue.Clear();
                    actionQueue.Enqueue(SelectYes);
                }

                if (ImGui.Button("Logout")) {
                    actionQueue.Clear();
                    actionQueue.Enqueue(Logout);
                    actionQueue.Enqueue(SelectYes);
                    actionQueue.Enqueue(Delay5s);
                }

                
                
                if (ImGui.Button("Swap Character")) {
                    tempDc = 9;
                    tempWorld = 87;
                    tempCharacter = 0;
                    
                    actionQueue.Enqueue(Logout);
                    actionQueue.Enqueue(SelectYes);
                    actionQueue.Enqueue(OpenDataCenterMenu);
                    actionQueue.Enqueue(SelectDataCentre);
                    actionQueue.Enqueue(SelectWorld);
                    actionQueue.Enqueue(SelectCharacter);
                    actionQueue.Enqueue(SelectYes);
                    actionQueue.Enqueue(Delay5s);
                    actionQueue.Enqueue(ClearTemp);
                }

                if (ImGui.Button("Full Run")) {
                    actionQueue.Clear();
                    actionQueue.Enqueue(OpenDataCenterMenu);
                    actionQueue.Enqueue(SelectDataCentre);
                    actionQueue.Enqueue(SelectWorld);
                    actionQueue.Enqueue(SelectCharacter);
                    actionQueue.Enqueue(SelectYes);
                }

                ImGui.Text("Current Queue:");
                foreach (var l in actionQueue.ToList()) {
                    ImGui.Text($"{l.Method.Name}");
                }
            }
            ImGui.End();
        }


        public static void GenerateCallback(AtkUnitBase* unitBase, params object[] values) {
            if (unitBase == null) throw new Exception("Null UnitBase");
            var atkValues = (AtkValue*) Marshal.AllocHGlobal(values.Length * sizeof(AtkValue));
            if (atkValues == null) return;
            try {
                for (var i = 0; i < values.Length; i++) {
                    var v = values[i];
                    switch (v) {
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
                            atkValues[i].Byte = (byte) (boolValue ? 1 : 0);
                            break;
                        case string stringValue: {
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
            } finally {
                for (var i = 0; i < values.Length; i++) {
                    if (atkValues[i].Type == ValueType.String) {
                        Marshal.FreeHGlobal(new IntPtr(atkValues[i].String));
                    }
                }
                Marshal.FreeHGlobal(new IntPtr(atkValues));
            }
        }
        public void Dispose()
        {
            PluginInterface.UiBuilder.Draw -= DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi -= this.OpenConfigUI;
            Commands.RemoveHandler("/autologinconfig");
            Commands.RemoveHandler("/swapcharacter");
        }
    }
}