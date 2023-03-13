using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Hooking;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using Veda;
using Veda.Attributes;

namespace FoodCheck
{
    public class Plugin : IDalamudPlugin
    {
        private readonly DalamudPluginInterface pluginInterface;
        private readonly PartyList partyList;
        private readonly ChatGui chat;
        private readonly SigScanner _sig;

        private readonly PluginCommandManager<Plugin> commandManager;
        private readonly WindowSystem windowSystem;
        private IntPtr _countdownPtr;
        private readonly CountdownTimer _countdownTimer;
        private Hook<CountdownTimer> _countdownTimerHook;
        private PluginUI ui;

        public static bool FirstRun = true;

        [PluginService] public static ClientState ClientState { get; private set; }

        [PluginService] public static PartyList PartyList { get; private set; }

        [PluginService] public static DalamudPluginInterface PluginInterface { get; set; }

        public Configuration config { get; private set; }

        [PluginService] public static Dalamud.Game.ClientState.Conditions.Condition Condition { get; private set; }

        private delegate void ProcessChatBoxDelegate(IntPtr uiModule, IntPtr message, IntPtr unused, byte a4);

        private delegate IntPtr GetUIModuleDelegate(IntPtr basePtr);

        private ProcessChatBoxDelegate? ProcessChatBox;
        private IntPtr uiModule = IntPtr.Zero;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        private delegate IntPtr CountdownTimer(ulong p1);

        public string Name => "Food Check";

        public Plugin(DalamudPluginInterface pi, CommandManager commands, PartyList partyList, ChatGui chat, SigScanner sig)
        {
            this.pluginInterface = pi;
            this.partyList = partyList;
            this.chat = chat;
            this._sig = sig;
            this._countdownTimer = CountdownTimerFunc;
            HookCountdownPointer();

            // Get or create a configuration object
            config = (Configuration)PluginInterface.GetPluginConfig() ?? new Configuration();
            config.Initialize(PluginInterface);
           
            ui = new PluginUI(this);
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
            //this.config.FateAutoSyncEnabled = !this.config.FateAutoSyncEnabled;
            //chat.Print($"Toggled auto fate syncing {(this.config.FateAutoSyncEnabled ? "on" : "off")}.");
        }

        private float _start;

        private IntPtr CountdownTimerFunc(ulong value)
        {
            float countDownPointerValue = Marshal.PtrToStructure<float>((IntPtr)value + 0x2c);
            if (Math.Floor(countDownPointerValue) - 2 <= _start)
            {
                _start = countDownPointerValue;
                return _countdownTimerHook.Original(value);
            }
            //if (FirstRun == true)
            //{
            if (Condition[ConditionFlag.BoundByDuty] || Condition[ConditionFlag.BoundByDuty56] || Condition[ConditionFlag.BoundByDuty95])
            {
                string PlayersWhoNeedToEat = "";
                foreach (var partyMember in PartyList)
                {
                    //chat.Print("Found party member " + partyMember.Name);
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
                    string FinalMessage = this.config.CustomizableMessage.Replace("<names>", PlayersWhoNeedToEat.Remove(PlayersWhoNeedToEat.Length - 1, 1));
                    if (config.PostToParty)
                    {
                        ExecuteCommand("/p " + FinalMessage);
                    }
                    if (config.PostToEcho) 
                    {
                        chat.Print(Functions.BuildSeString("[FoodCheck]", FinalMessage, LogType.Normal));
                    }
                }
                ///FirstRun = false;
                //}
            }
            _start = countDownPointerValue;
            return _countdownTimerHook.Original(value);
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

        private unsafe void HookCountdownPointer()
        {
            _countdownPtr = _sig.ScanText("48 89 5C 24 ?? 57 48 83 EC 40 8B 41");
            try
            {
                _countdownTimerHook = new Hook<CountdownTimer>(_countdownPtr, _countdownTimer);
                _countdownTimerHook.Enable();
            }
            catch (Exception e)
            {
                PluginLog.Error("Could not hook to timer\n" + e);
            }

            var getUIModulePtr = _sig.ScanText("E8 ?? ?? ?? ?? 48 83 7F ?? 00 48 8B F0");
            var easierProcessChatBoxPtr = _sig.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 45 84 C9");
            var uiModulePtr = _sig.GetStaticAddressFromSig("48 8B 0D ?? ?? ?? ?? 48 8D 54 24 ?? 48 83 C1 10 E8");

            var GetUIModule = Marshal.GetDelegateForFunctionPointer<GetUIModuleDelegate>(getUIModulePtr);

            uiModule = GetUIModule(*(IntPtr*)uiModulePtr);
            ProcessChatBox = Marshal.GetDelegateForFunctionPointer<ProcessChatBoxDelegate>(easierProcessChatBoxPtr);
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            this.commandManager.Dispose();

            PluginInterface.SavePluginConfig(config);

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