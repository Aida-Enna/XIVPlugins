using Dalamud.Game.ClientState.Conditions;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Veda;

namespace FoodCheck
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "FoodCheck";

        [PluginService] public static IDalamudPluginInterface PluginInterface { get; set; }
        [PluginService] public static ICondition Conditions { get; set; }
        [PluginService] public static IDataManager Data { get; set; }
        [PluginService] public static ISigScanner SigScanner { get; set; }
        [PluginService] public static IChatGui Chat { get; set; }
        [PluginService] public static IClientState ClientState { get; set; }
        [PluginService] public static IPartyList PartyList { get; set; }
        [PluginService] public static IPluginLog PluginLog { get; set; }
        [PluginService] public static IGameInteropProvider Hook { get; set; }
        [PluginService] public static IGameInteropProvider GameInterop { get; set; }

        public static Configuration PluginConfig { get; set; }
        private PluginCommandManager<Plugin> commandManager;
        private PluginUI ui;

        public static bool FirstRun = true;
        public static bool Debug = false;

        private delegate nint CountdownTimerHookDelegate(ulong a1);

        [Signature("40 53 48 83 EC 40 80 79 38 00", DetourName = nameof(OnCountdownTimer))]
        private readonly Hook<CountdownTimerHookDelegate>? _countdownTimerHook = null;

        private static Hook<AgentReadyCheck.Delegates.InitiateReadyCheck> ReadyCheckHook;

        private readonly CountdownEvent _countdownEvent;

        public unsafe Plugin(IDalamudPluginInterface pluginInterface, IChatGui chat, IPartyList partyList, ICommandManager commands, ISigScanner sigScanner)
        {
            PluginInterface = pluginInterface;
            PartyList = partyList;
            Chat = chat;
            SigScanner = sigScanner;

            Plugin.GameInterop.InitializeFromAttributes(this);
            _countdownTimerHook?.Enable();
            Functions.GetChatSignatures(sigScanner);

            // Get or create a configuration object
            PluginConfig = (Configuration)PluginInterface.GetPluginConfig() ?? new Configuration();
            PluginConfig.Initialize(PluginInterface);

            ReadyCheckHook = Plugin.Hook.HookFromAddress<AgentReadyCheck.Delegates.InitiateReadyCheck>(AgentReadyCheck.MemberFunctionPointers.InitiateReadyCheck, ReadyCheckInitiatedDetour);
            ReadyCheckHook.Enable();

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
        [HelpMessage("Opens the Food Check config menu")]
        public void OpenSettings(string command, string args)
        {
            ui.IsVisible = !ui.IsVisible;
        }

        [Command("/checkfood")]
        [HelpMessage("Manually check for food")]
        public void ManuallyCheckFood(string command, string args)
        {
            CheckWhoNeedsToEat();
        }

        private float _start;

        private nint OnCountdownTimer(ulong value)
        {
            try
            {
                float countDownPointerValue = Marshal.PtrToStructure<float>((IntPtr)value + 0x2c);
                if (Math.Floor(countDownPointerValue) - 2 <= _start)
                {
                    _start = countDownPointerValue;
                    return _countdownTimerHook.Original(value);
                }

                if (Conditions[ConditionFlag.BoundByDuty] || Conditions[ConditionFlag.BoundByDuty56] || Conditions[ConditionFlag.BoundByDuty95] || Debug)
                {
                    if (!PlayerIsInHighEndDuty() & PluginConfig.OnlyDoHighEndDuties)
                    {
                        _start = countDownPointerValue;
                        return _countdownTimerHook.Original(value);
                    }
                    if (PartyList.Count() == 0)
                    {
                        //Chat.Print("(There are no other party members)");
                        _start = countDownPointerValue;
                        return _countdownTimerHook.Original(value);
                    }
                    CheckWhoNeedsToEat();
                }

                _start = countDownPointerValue;
                return _countdownTimerHook.Original(value);
            }
            catch (Exception f)
            {
                Chat.PrintError("Something went wrong - " + f.ToString());
                return _countdownTimerHook.Original(value);
            }
        }

        private static unsafe void ReadyCheckInitiatedDetour(AgentReadyCheck* ptr)
        {
            ReadyCheckHook.Original(ptr);
            if (Conditions[ConditionFlag.BoundByDuty] || Conditions[ConditionFlag.BoundByDuty56] || Conditions[ConditionFlag.BoundByDuty95] || Debug)
            {
                if (!PlayerIsInHighEndDuty() & PluginConfig.OnlyDoHighEndDuties)
                {
                    return;
                }
                if (PartyList.Count() == 0)
                {
                    //Chat.Print("(There are no other party members)");
                    return;
                }
                CheckWhoNeedsToEat();
            }
        }

        public static unsafe void CheckWhoNeedsToEat()
        {
            string PlayersWhoNeedToEat = "";
            foreach (var partyMember in PartyList)
            {
                if (partyMember == null) { continue; }
                //Chat.Print("Found party member " + partyMember.Name);
                //Chat.Print("Fed? " + partyMember.Statuses.FirstOrDefault(status => status.GameData.Name == "Well Fed"));
                //Check for food
                if (partyMember.Statuses.FirstOrDefault(status => status.GameData.Value.Name == "Well Fed") == null)
                {
                    //if (first)
                    //{
                    //this.chat.Print($"FOOD CHECK!");
                    //    first = false;
                    //}
                    if (Plugin.PluginConfig.OnlyUseFirstNames)
                    {
                        PlayersWhoNeedToEat += partyMember.Name.TextValue.Split(' ')[0] + ", ";
                    }
                    else
                    {
                        PlayersWhoNeedToEat += partyMember.Name.TextValue + ", ";
                    }
                    //this.chat.Print($"{partyMember.Name}");
                }
                else
                {
                    var statusManager = ((Character*)partyMember.GameObject.Address)->GetStatusManager();
                    var statusIndex = statusManager->GetStatusIndex(48);
                    var RemainingTime = statusManager->GetRemainingTime(statusIndex)/60;

                    //Chat.Print(partyMember.Name.TextValue + " has " + RemainingTime + " minutes left on their food buff.");
                    if (PluginConfig.CheckForFoodUnderXMinutes && RemainingTime < PluginConfig.MinutesToCheck)
                    {
                        if (Plugin.PluginConfig.OnlyUseFirstNames)
                        {
                            PlayersWhoNeedToEat += partyMember.Name.TextValue.Split(' ')[0] + ", ";
                        }
                        else
                        {
                            PlayersWhoNeedToEat += partyMember.Name.TextValue + ", ";
                        }
                    }
                }
            }
            if (PlayersWhoNeedToEat != "" && PlayersWhoNeedToEat.Length > 3)
            {
                string FinalMessage = PluginConfig.CustomizableMessage.Replace("<names>", PlayersWhoNeedToEat.Remove(PlayersWhoNeedToEat.Length - 2, 2));
                if (PluginConfig.PostToParty)
                {
                    Functions.Send("/p " + FinalMessage);
                }
                if (PluginConfig.ChatType.ToString().ToLower() != "none")
                {
                    //Chat.Print(Functions.BuildSeString("FoodCheck", FinalMessage));
                    Chat.Print(new Dalamud.Game.Text.XivChatEntry
                    {
                        Message = Functions.BuildSeString("FoodCheck", FinalMessage),
                        Type = PluginConfig.ChatType,
                    });
                }
            }
        }

        //Taken from the Stanley Parable plugin, https://github.com/rekyuu/StanleyParableXiv/blob/main/StanleyParableXiv/Utility/XivUtility.cs
        //Based and hilarious plugin, you should check it out
        /// <summary>
        /// Checks if the territory is Unreal, Extreme, Savage, or Ultimate difficulty.
        /// </summary>
        /// <param name="territoryType">The territory to check against.</param>
        /// <returns>True if high-end, false otherwise.</returns>
        public static bool TerritoryIsHighEndDuty(ushort territoryType)
        {
            string name = Plugin.Data.Excel
                .GetSheet<TerritoryType>()!
                .GetRow(territoryType)!
                .ContentFinderCondition.Value!
                .Name
                .ToString();

            bool isHighEndDuty = name.StartsWith("the Minstrel's Ballad")
                || name.EndsWith("(Unreal)")
                || name.EndsWith("(Extreme)")
                || name.EndsWith("(Savage)")
                || name.EndsWith("(Ultimate)");

            PluginLog.Debug("{DutyName} is high end: {IsHighEnd}", name, isHighEndDuty);

            return isHighEndDuty;
        }

        /// <summary>
        /// Checks if player's current territory is Unreal, Extreme, Savage, or Ultimate difficulty.
        /// </summary>
        /// <returns>True if high-end, false otherwise.</returns>
        public static bool PlayerIsInHighEndDuty()
        {
            return TerritoryIsHighEndDuty(Plugin.ClientState.TerritoryType);
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

            //if (_countdownTimerHook == null) return;
            _countdownTimerHook.Disable();
            _countdownTimerHook.Dispose();
            ReadyCheckHook.Disable();
            ReadyCheckHook.Dispose();
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}