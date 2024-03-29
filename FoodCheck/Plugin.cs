﻿using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Hooking;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using Veda;

namespace FoodCheck
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "FoodCheck";

        [PluginService] public static DalamudPluginInterface PluginInterface { get; set; }
        [PluginService] public static ICommandManager Commands { get; set; }
        [PluginService] public static ICondition Conditions { get; set; }
        [PluginService] public static IDataManager Data { get; set; }
        [PluginService] public static IFramework Framework { get; set; }
        [PluginService] public static IGameGui GameGui { get; set; }
        [PluginService] public static ISigScanner SigScanner { get; set; }
        [PluginService] public static IKeyState KeyState { get; set; }
        [PluginService] public static IChatGui Chat { get; set; }
        [PluginService] public static IClientState ClientState { get; set; }
        [PluginService] public static IPartyList PartyList { get; set; }
        [PluginService] public static IGameInteropProvider Hook { get; set; }

        public static Configuration PluginConfig { get; set; }
        private PluginCommandManager<Plugin> commandManager;
        private readonly WindowSystem windowSystem;
        private IntPtr _countdownPtr;
        private readonly CountdownTimer _countdownTimer;
        private Hook<CountdownTimer> _countdownTimerHook;
        private PluginUI ui;

        public static bool FirstRun = true;

        private delegate void ProcessChatBoxDelegate(IntPtr uiModule, IntPtr message, IntPtr unused, byte a4);

        private delegate IntPtr GetUIModuleDelegate(IntPtr basePtr);

        private ProcessChatBoxDelegate? ProcessChatBox;
        private IntPtr uiModule = IntPtr.Zero;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        private delegate IntPtr CountdownTimer(ulong p1);

        public Plugin(DalamudPluginInterface pluginInterface, IChatGui chat, IPartyList partyList, ICommandManager commands, ISigScanner sigScanner)
        {
            PluginInterface = pluginInterface;
            PartyList = partyList;
            Chat = chat;
            SigScanner = sigScanner;
            this._countdownTimer = CountdownTimerFunc;
            HookCountdownPointer();

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

            // Load all of our commands
            this.commandManager = new PluginCommandManager<Plugin>(this, commands);

        }

        [Command("/foodcheck")]
        [HelpMessage("Toggles auto fate syncing on/off.")]
        public void ToggleAutoFate(string command, string args)
        {
            ui.IsVisible = !ui.IsVisible;
        }

        private float _start;

        private IntPtr CountdownTimerFunc(ulong value)
        {
            try
            {
                float countDownPointerValue = Marshal.PtrToStructure<float>((IntPtr)value + 0x2c);
                if (Math.Floor(countDownPointerValue) - 2 <= _start)
                {
                    _start = countDownPointerValue;
                    return _countdownTimerHook.Original(value);
                }
                //if (FirstRun == true)
                //{
                bool Debug = true;
                if (Conditions[ConditionFlag.BoundByDuty] || Conditions[ConditionFlag.BoundByDuty56] || Conditions[ConditionFlag.BoundByDuty95] || Debug)
                {
                    var currentZone = ClientState.TerritoryType;
                    var territoryTypeInfo = Data.GetExcelSheet<TerritoryType>()!.GetRow(currentZone);
                    if (!territoryTypeInfo.ContentFinderCondition.Value.HighEndDuty && PluginConfig.OnlyDoHighEndDuties)
                    {
                        _start = countDownPointerValue;
                        return _countdownTimerHook.Original(value);
                    }
                    string PlayersWhoNeedToEat = "";
                    if (PartyList.Count() == 0) Chat.Print("(There are no other party members)");
                    foreach (var partyMember in PartyList)
                    {
                        //Chat.Print("Found party member " + partyMember.Name);
                        //chat.Print("Fed? " + partyMember.Statuses.FirstOrDefault(status => status.GameData.Name == "Well Fed"));
                        //Check for food
                        if (partyMember.Statuses.FirstOrDefault(status => status.GameData.Name == "Well Fed") == null)
                        {
                            //if (first)
                            //{
                            //this.chat.Print($"FOOD CHECK!");
                            //    first = false;
                            //}
                            PlayersWhoNeedToEat += partyMember.Name.TextValue + " ";
                            //this.chat.Print($"{partyMember.Name}");
                        }
                    }
                    if (PlayersWhoNeedToEat != "")
                    {
                        string FinalMessage = PluginConfig.CustomizableMessage.Replace("<names>", PlayersWhoNeedToEat.Remove(PlayersWhoNeedToEat.Length - 1, 1));
                        if (PluginConfig.PostToParty)
                        {
                            ExecuteCommand("/p " + FinalMessage);
                        }
                        if (PluginConfig.PostToEcho)
                        {
                            Chat.Print(Functions.BuildSeString("FoodCheck", FinalMessage));
                        }
                    }
                    ///FirstRun = false;
                    //}
                }
                _start = countDownPointerValue;
                return _countdownTimerHook.Original(value);
            }
            catch(Exception f)
            {
                Chat.PrintError("Something went wrong - " + f.ToString());
                return _countdownTimerHook.Original(value);
            }
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
            _countdownPtr = SigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 40 8B 41");
            try
            {
                _countdownTimerHook = Hook.HookFromSignature<CountdownTimer>("48 89 5C 24 ?? 57 48 83 EC 40 8B 41", _countdownTimer);
                _countdownTimerHook.Enable();
                PluginLog.Error("Timer hooked!\n");
            }
            catch (Exception e)
            {
                PluginLog.Error("Could not hook to timer\n" + e);
            }
            try
            {
                var getUIModulePtr = SigScanner.ScanText("E8 ?? ?? ?? ?? 48 83 7F ?? 00 48 8B F0");
                var easierProcessChatBoxPtr = SigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 45 84 C9");
                var uiModulePtr = SigScanner.GetStaticAddressFromSig("48 8B 0D ?? ?? ?? ?? 48 8D 54 24 ?? 48 83 C1 10 E8");

                var GetUIModule = Marshal.GetDelegateForFunctionPointer<GetUIModuleDelegate>(getUIModulePtr);

                uiModule = GetUIModule(*(IntPtr*)uiModulePtr);
                ProcessChatBox = Marshal.GetDelegateForFunctionPointer<ProcessChatBoxDelegate>(easierProcessChatBoxPtr);
                PluginLog.Error("Chatbox hooked!\n");
            }
            catch (Exception e)
            {
                PluginLog.Error("Could not hook to chatbox\n" + e);
            }
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
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}