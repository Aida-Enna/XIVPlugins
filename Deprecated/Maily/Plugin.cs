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
using static FFXIVClientStructs.FFXIV.Component.GUI.AtkResNode.Delegates;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace Maily
{
    public unsafe class Plugin : IDalamudPlugin
    {
        public string Name => "Maily";

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

        public static Configuration PluginConfig { get; set; }
        private PluginCommandManager<Plugin> CommandManager;
        private bool drawConfigWindow;

        private readonly Queue<Func<bool>> actionQueue = new();
        private readonly Stopwatch sw = new();
        private uint Delay = 0;
        private int CurrentLetter = 0;
        private bool UIVisible = false;
        private bool ShowSupport = false;

        public Plugin(IDalamudPluginInterface pluginInterface, IChatGui chat, IPartyList partyList, ICommandManager commands, ISigScanner sigScanner)
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

            // Load all of our commands
            CommandManager = new PluginCommandManager<Plugin>(this, commands);
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
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

        public bool OpenFirstLetter()
        {
            var addon = (AtkUnitBase*)GameGui.GetAddonByName("LetterList");
            if (addon == null || addon->IsVisible == false) return false;
            PluginLog.Debug("Found LetterList");
            GenerateCallback(addon, 0,0,0,0);
            var nextAddon = (AtkUnitBase*)GameGui.GetAddonByName("LetterViewer");
            if (nextAddon == null) return false;
            PluginLog.Debug("Found LetterViewer");
            return true;
        }
        public bool CloseLetter()
        {
            var addon = (AtkUnitBase*)GameGui.GetAddonByName("LetterViewer");
            if (addon == null || addon->IsVisible == false) return false;
            PluginLog.Debug("Found LetterViewer");
            addon->Close(true);
            return true;
        }
        public bool OpenSpecificLetter()
        {
            var addon = (AtkUnitBase*)GameGui.GetAddonByName("LetterList");
            if (addon == null || addon->IsVisible == false) return false;
            PluginLog.Debug("Found LetterList");
            GenerateCallback(addon, 0, CurrentLetter, 0, 0);
            var nextAddon = (AtkUnitBase*)GameGui.GetAddonByName("LetterViewer");
            if (nextAddon == null) return false;
            PluginLog.Debug("Found LetterViewer");
            return true;
        }

        public bool TakeAllItemsFromLetter()
        {
            var addon = (AtkUnitBase*)GameGui.GetAddonByName("LetterViewer");
            if (addon == null || addon->IsVisible == false) return false;
            PluginLog.Debug("Found LetterViewer");
            GenerateCallback(addon, 1);
            //var nextAddon = (AtkUnitBase*)GameGui.GetAddonByName("LetterViewer");
            //if (nextAddon == null) return false;
            //PluginLog.Debug("Found LetterViewer");
            return true;
        }
        public bool DeleteLetter()
        {
            var addon = (AtkUnitBase*)GameGui.GetAddonByName("LetterViewer");
            if (addon == null || addon->IsVisible == false) return false;
            PluginLog.Debug("Found LetterViewer");
            GenerateCallback(addon, 2);
            addon = (AtkUnitBase*)GameGui.GetAddonByName("SelectYesno", 1);
            if (addon == null) return false;
            GenerateCallback(addon, 0);
            addon->Close(true);
            //var nextAddon = (AtkUnitBase*)GameGui.GetAddonByName("LetterViewer");
            //if (nextAddon == null) return false;
            //PluginLog.Debug("Found LetterViewer");
            return true;
        }

        public bool IncrementLetter()
        {
            CurrentLetter++;
            return true;
        }

        private bool drawDebugWindow = true;
        private void DrawUI()
        {
            if (!UIVisible) { return; }
            if (ImGui.Begin($"{this.Name} Menu", ref drawDebugWindow, ImGuiWindowFlags.AlwaysAutoResize))
            {
                if (ImGui.Button("Open all letters"))
                {
                    CurrentLetter = 0;
                    do
                    {
                        if (PluginConfig.DeleteLetters)
                        {
                            actionQueue.Enqueue(OpenFirstLetter);
                            actionQueue.Enqueue(TakeAllItemsFromLetter);
                            actionQueue.Enqueue(Delay5Seconds);
                            actionQueue.Enqueue(DeleteLetter);
                        }
                        else
                        {
                            actionQueue.Enqueue(OpenSpecificLetter);
                            actionQueue.Enqueue(TakeAllItemsFromLetter);
                            actionQueue.Enqueue(CloseLetter);
                        }
                        actionQueue.Enqueue(Delay5Seconds);
                        actionQueue.Enqueue(IncrementLetter);
                    } while (CurrentLetter != PluginConfig.MaxLettersToOpen);
                }
                ImGui.SameLine();
                ImGui.Checkbox("Delete letters", ref PluginConfig.DeleteLetters);
                ImGui.Text("Number of letters to open:");
                ImGui.SetNextItemWidth(190);
                ImGui.SliderInt("###MaxLetter", ref PluginConfig.MaxLettersToOpen, 1, 20);
                ImGui.Text("Current Status:");
                if (actionQueue.Count > 0)
                {
                    ImGui.Text($"{actionQueue.ToList().First().Method.Name}".Replace("OpenFirstLetter", "Opening letter " + CurrentLetter + "...").Replace("OpenSpecificLetter", "Opening letter" + CurrentLetter + "...").Replace("TakeAllItemsFromLetter", "Taking item(s)...").Replace("Delay5Seconds", "Waiting for server to catch up...").Replace("DeleteLetter", "Deleting letter...").Replace("CloseLetter", "Closing letter..."));
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
            if (!drawDebugWindow) return;
            if (ImGui.Begin($"{this.Name} Debugging", ref drawDebugWindow))
            {
                if (ImGui.Button("Open Config")) drawConfigWindow = true;
                if (ImGui.Button("Clear Queue"))
                {
                    actionQueue.Clear();
                }

                if (ImGui.Button("Test Step: Open Letter")) actionQueue.Enqueue(OpenFirstLetter);
                if (ImGui.Button("Test Step: Take All Items")) actionQueue.Enqueue(TakeAllItemsFromLetter);
                if (ImGui.Button("Test Step: Delete Letter")) actionQueue.Enqueue(DeleteLetter);

                if (ImGui.Button("Test Run"))
                {
                    actionQueue.Enqueue(OpenFirstLetter);
                    actionQueue.Enqueue(TakeAllItemsFromLetter);
                    actionQueue.Enqueue(Delay5Seconds);
                    actionQueue.Enqueue(DeleteLetter);
                }

                if (ImGui.Button("Open all letters"))
                {
                    CurrentLetter = 0;
                    do
                    {
                        actionQueue.Enqueue(OpenFirstLetter);
                        actionQueue.Enqueue(TakeAllItemsFromLetter);
                        actionQueue.Enqueue(Delay5Seconds);
                        actionQueue.Enqueue(DeleteLetter);
                        actionQueue.Enqueue(Delay5Seconds);
                        if (PluginConfig.DeleteLetters) { CurrentLetter++; }
                    } while (CurrentLetter != 20);
                }
            }
            ImGui.End();
        }

        public bool Delay5Seconds()
        {
            //Math is hard bro
            Delay = 5*60;
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

        [Command("/maily")]
        [HelpMessage("Shows Maily Menu")]
        public void ToggleMailyWindow(string command, string args)
        {
            UIVisible = !UIVisible;
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            CommandManager.Dispose();

            PluginInterface.SavePluginConfig(PluginConfig);

            PluginInterface.UiBuilder.Draw -= DrawUI;
            Framework.Update -= OnFrameworkUpdate;

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}