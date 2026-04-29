using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using Veda;

//using ECommons;
//using ECommons.DalamudServices;
//using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;

using FFXIVClientStructs.FFXIV.Client.Game.UI;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

using FFXIVClientStructs.FFXIV.Component.GUI;

using static FFXIVClientStructs.FFXIV.Component.GUI.AtkResNode.Delegates;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;
using TwitchLib.PubSub.Models.Responses.Messages.AutomodCaughtMessage;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Lumina.Excel.Sheets;
using Dalamud.Game.ClientState.Conditions;

namespace AutoQueue
{
    public unsafe class Plugin : IDalamudPlugin
    {
        public string Name => "AutoQueue";

        [PluginService] public static IDalamudPluginInterface PluginInterface { get; set; }
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
        [PluginService] public static IPluginLog PluginLog { get; set; }
        [PluginService] public static ICondition Condition { get; set; }

        public static Configuration PluginConfig { get; set; }
        private PluginCommandManager<Plugin> CommandManager;

        public static bool FirstRun = true;
        public string PreviousWorkingChannel;
        public bool SuccessfullyJoined;
        private Random RNGenerator = new Random();
        private readonly Queue<Func<bool>> actionQueue = new();
        private readonly Stopwatch sw = new();
        private uint Delay = 0;
        private bool ShowSupport = false;
        private bool UIVisible = true;
        private AtkUnitBase* ContentsFinderAddon;
        private AtkUnitBase* DFConfirmAddon;

        public Plugin(IDalamudPluginInterface pluginInterface, IChatGui chat, IPartyList partyList, ICommandManager commands, ISigScanner sigScanner, ICondition conditions)
        {
            PluginInterface = pluginInterface;
            PartyList = partyList;
            Chat = chat;
            SigScanner = sigScanner;

            Framework.Update += OnFrameworkUpdate;

            // Get or create a configuration object
            PluginConfig = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            PluginConfig.Initialize(PluginInterface);

            PluginInterface.UiBuilder.Draw += new System.Action(DrawUI);
            PluginInterface.UiBuilder.OpenConfigUi += () =>
            {
                UIVisible = !UIVisible;
            };
            
            conditions.ConditionChange += ConditionChanged;

            // Load all of our commands
            CommandManager = new PluginCommandManager<Plugin>(this, commands);

            Functions.GetChatSignatures(sigScanner);
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            if (!ClientState.IsLoggedIn) { return; }
            if (actionQueue.Count == 0)
            {
                if (sw.IsRunning) sw.Stop();
                return;
            }
            if (!sw.IsRunning) sw.Restart();

            if (Delay > 0)
            {
                Delay -= 1;
                return;
            }

            //if (sw.ElapsedMilliseconds > 30000)
            //{
            //    actionQueue.Clear();
            //    return;
            //}

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

            var CurrentCharacterPtr = (Character*)null;
            try
            {
                CurrentCharacterPtr = (Character*)ClientState.LocalPlayer.Address;
            }
            catch (Exception f)
            {
                return;
            }
            
        }

        public bool OpenDF()
        {
            AgentContentsFinder.Instance()->OpenRegularDuty(1);
            ContentsFinderAddon = (AtkUnitBase*)GameGui.GetAddonByName("ContentsFinder");
            if (ContentsFinderAddon == null || ContentsFinderAddon->IsVisible == false) return false;
            PluginLog.Debug("Found ContentsFinderAddon");
            return true;
        }
        public bool CloseDF()
        {
            ContentsFinderAddon = (AtkUnitBase*)GameGui.GetAddonByName("ContentsFinder");
            if (ContentsFinderAddon == null) return false;
            PluginLog.Debug("Found ContentsFinderAddon");
            ContentsFinderAddon->Close(false);
            return true;
        }

        public bool QueueForDuty()
        {
            //AgentContentsFinder.Instance()->OpenRegularDuty(1);
            if (ContentsFinderAddon == null || ContentsFinderAddon->IsVisible == false) return false;
            PluginLog.Debug("Found ContentsFinderAddon");
            GenerateCallback(ContentsFinderAddon, 12, 0);
            return true;
        }

        private bool ConfirmDFPop()
        {
            if (PluginConfig.AutoAcceptEnabled == false) { return true; }
            DFConfirmAddon = (AtkUnitBase*)GameGui.GetAddonByName("ContentsFinderConfirm");
            if (DFConfirmAddon != null || DFConfirmAddon->IsVisible == true)
            {
                GenerateCallback(DFConfirmAddon, 8);
                return true;
            }
            return false;
        }

        //private bool ClearQueue()
        //{
        //    actionQueue.Clear();
        //    return true;
        //}

        public bool Delay5Seconds()
        {
            //Math is hard bro
            Delay = 5 * 60;
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

        public void ConditionChanged(ConditionFlag flag, bool value)
        {
            bool IsDebug = false;
            //Have a string option that stores the name of the preset to use in each case (or "none" for don't change it)
            //Have a dropbox next to each condition that lists the current presets
            switch (flag)
            {
                case ConditionFlag.BetweenAreas: //the fade to black loading screen
                    if (value)
                    {
                        if (IsDebug) Chat.Print("BetweenAreas: true");
                        //ApplyConfig("min");
                    }
                    else
                    {
                        if (IsDebug) Chat.Print("BetweenAreas: false");
                        //ApplyConfig("max");
                    }
                    break;
                case ConditionFlag.BoundByDuty:
                    if (value)
                    {
                        if (IsDebug) Chat.Print("BoundByDuty: true");
                        //if (PluginConfig.InDutyPreset != "None")
                        //{
                        //    ApplyConfig(PluginConfig.InDutyPreset, true);
                        //}
                    }
                    else
                    {
                        if (IsDebug) Chat.Print("BoundByDuty: false");
                        //if (PluginConfig.DefaultPreset != "None" & PluginConfig.InDutyPreset != "None")
                        //{
                        //    ApplyConfig(PluginConfig.DefaultPreset, true);
                        //}
                    }
                    break;
                case ConditionFlag.BoundByDuty56:
                    if (value)
                    {
                        if (IsDebug) Chat.Print("BoundByDuty56: true");
                        //if (PluginConfig.InDutyPreset != "None")
                        //{
                        //    ApplyConfig(PluginConfig.InDutyPreset, true);
                        //}
                    }
                    else
                    {
                        if (IsDebug) Chat.Print("BoundByDuty56: false");
                        if (PluginConfig.AutoQueueEnabled)
                        {
                            actionQueue.Enqueue(Delay5Seconds);
                            actionQueue.Enqueue(OpenDF);
                            actionQueue.Enqueue(QueueForDuty);
                            actionQueue.Enqueue(CloseDF);
                        }
                        //if (PluginConfig.DefaultPreset != "None" & PluginConfig.InDutyPreset != "None")
                        //{
                        //    ApplyConfig(PluginConfig.DefaultPreset, true);
                        //}
                    }
                    break;
                case ConditionFlag.BoundByDuty95:
                    if (value)
                    {
                        if (IsDebug) Chat.Print("BoundByDuty95: true");
                        //if (PluginConfig.InDutyPreset != "None")
                        //{
                        //    ApplyConfig(PluginConfig.InDutyPreset, true);
                        //}
                    }
                    else
                    {
                        if (IsDebug) Chat.Print("BoundByDuty95: false");
                        //if (PluginConfig.DefaultPreset != "None" & PluginConfig.InDutyPreset != "None")
                        //{
                        //    ApplyConfig(PluginConfig.DefaultPreset, true);
                        //}
                    }
                    break;
                case ConditionFlag.InDutyQueue: // DF searching
                    if (value)
                    {
                        if (IsDebug) Chat.Print("InDutyQueue: true");
                    }
                    else
                    {
                        if (IsDebug) Chat.Print("InDutyQueue: false");
                    }
                    break;
                case ConditionFlag.WaitingForDuty: //Accepted
                    if (value)
                    {
                        if (IsDebug) Chat.Print("WaitingForDuty: true");
                    }
                    else
                    {
                        if (IsDebug) Chat.Print("WaitingForDuty: false");
                    }
                    break;
                case ConditionFlag.WaitingForDutyFinder: //DF Popped
                    if (value)
                    {
                        if (IsDebug) Chat.Print("WaitingForDutyFinder: true");
                        if (PluginConfig.AutoAcceptEnabled) { actionQueue.Enqueue(ConfirmDFPop); }
                    }
                    else
                    {
                        if (IsDebug) Chat.Print("WaitingForDutyFinder: false");
                    }
                    break;
            }
        }

        private void DrawUI()
        {
            if (!UIVisible) { return; }
            if (ImGui.Begin($"{this.Name} Menu", ref UIVisible, ImGuiWindowFlags.AlwaysAutoResize))
            {
                if (ImGui.Button("Open DF"))
                {
                    actionQueue.Enqueue(OpenDF);
                }
                if (ImGui.Button("Queue selected duty"))
                {
                    actionQueue.Enqueue(QueueForDuty);
                }
                if (ImGui.Button("Confirm DF pop"))
                {
                    actionQueue.Enqueue(ConfirmDFPop);
                }
                ImGui.Checkbox("Auto queue (repeat)", ref PluginConfig.AutoQueueEnabled);
                ImGui.Checkbox("Auto accept queue", ref PluginConfig.AutoAcceptEnabled);
                if (ImGui.Button("Clear queue"))
                {
                    actionQueue.Clear();
                }
                //if (ImGui.Button("Open all letters"))
                //{
                //            actionQueue.Enqueue(OpenFirstLetter);
                //            actionQueue.Enqueue(TakeAllItemsFromLetter);
                //            actionQueue.Enqueue(Delay5Seconds);
                //            actionQueue.Enqueue(DeleteLetter);
                //}
                //ImGui.Text("Number of letters to open:");
                //ImGui.SetNextItemWidth(190);
                //ImGui.SliderInt("###MaxLetter", ref PluginConfig.MaxLettersToOpen, 1, 20);
                ImGui.Text("Current Status:");
                if (actionQueue.Count > 0)
                {
                    ImGui.Text($"{actionQueue.ToList().First().Method.Name}");
                }
                else
                {
                    ImGui.Text("Waiting for command...");
                }
            }
            if (ImGui.Button("Save"))
            {
                Plugin.PluginConfig.Save();
            }
            ImGui.SameLine();
            ImGui.Indent(50);
            if (ImGui.Button("Want to help support my work?"))
            {
                ShowSupport = !ShowSupport;
            }
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Click me!"); }
            if (ShowSupport)
            {
                ImGui.Indent(-50);
                ImGui.Text("Here are the current ways you can support the work I do.\nEvery bit helps, thank you! ♥ Have a great day!");
                ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.19f, 0.52f, 0.27f, 1));
                if (ImGui.Button("Donate via Paypal"))
                {
                    Functions.OpenWebsite("https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=QXF8EL4737HWJ");
                }
                ImGui.PopStyleColor();
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.95f, 0.39f, 0.32f, 1));
                if (ImGui.Button("Become a Patron"))
                {
                    Functions.OpenWebsite("https://www.patreon.com/bePatron?u=5597973");
                }
                ImGui.PopStyleColor();
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.25f, 0.67f, 0.87f, 1));
                if (ImGui.Button("Support me on Ko-Fi"))
                {
                    Functions.OpenWebsite("https://ko-fi.com/Y8Y114PMT");
                }
                ImGui.PopStyleColor();
            }

            //if (ImGui.Begin($"{this.Name} Debugging")
            //{
            //    if (ImGui.Button("Open Config")) drawConfigWindow = true;
            //    if (ImGui.Button("Clear Queue"))
            //    {
            //        actionQueue.Clear();
            //    }

            //    if (ImGui.Button("Test Step: Open Letter")) actionQueue.Enqueue(OpenFirstLetter);
            //    if (ImGui.Button("Test Step: Take All Items")) actionQueue.Enqueue(TakeAllItemsFromLetter);
            //    if (ImGui.Button("Test Step: Delete Letter")) actionQueue.Enqueue(DeleteLetter);

            //    if (ImGui.Button("Test Run"))
            //    {
            //        actionQueue.Enqueue(OpenFirstLetter);
            //        actionQueue.Enqueue(TakeAllItemsFromLetter);
            //        actionQueue.Enqueue(Delay5Seconds);
            //        actionQueue.Enqueue(DeleteLetter);
            //    }

            //    if (ImGui.Button("Open all letters"))
            //    {
            //        CurrentLetter = 0;
            //        do
            //        {
            //            actionQueue.Enqueue(OpenFirstLetter);
            //            actionQueue.Enqueue(TakeAllItemsFromLetter);
            //            actionQueue.Enqueue(Delay5Seconds);
            //            actionQueue.Enqueue(DeleteLetter);
            //            actionQueue.Enqueue(Delay5Seconds);
            //            if (PluginConfig.DeleteLetters) { CurrentLetter++; }
            //        } while (CurrentLetter != 20);
            //    }
            //}
            //ImGui.End();
        }

        [Command("/autoqueue")]
        [Aliases("/aq")]
        [HelpMessage("Shows AutoQueue configuration options")]
        public void ShowTwitchOptions(string command, string args)
        {
            UIVisible = !UIVisible;
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            CommandManager.Dispose();

            PluginInterface.SavePluginConfig(PluginConfig);

            Framework.Update -= OnFrameworkUpdate;

            PluginInterface.UiBuilder.Draw -= DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi -= () =>
            {
                UIVisible = !UIVisible;
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