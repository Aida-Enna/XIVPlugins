using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Memory;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Veda;

namespace PortraitFixer
{
    public class Plugin : IDalamudPlugin
    {
        public unsafe string Name => "PortraitFixer";

        [PluginService] public static IDalamudPluginInterface PluginInterface { get; set; }
        [PluginService] public static IFramework Framework { get; set; }
        [PluginService] public static IGameGui GameGui { get; set; }
        [PluginService] public static ISigScanner SigScanner { get; set; }
        [PluginService] public static IChatGui Chat { get; set; }
        [PluginService] public static IPluginLog PluginLog { get; set; }
        [PluginService] public static IGameInteropProvider HookProvider { get; private set; } = null!;

        private PluginCommandManager<Plugin> commandManager;
        private PluginUI ui;
        public static Configuration PluginConfig { get; set; }
        private bool drawConfigWindow;

        public static readonly Queue<Func<bool>> actionQueue = new();
        private readonly Stopwatch sw = new();
        private static uint Delay = 0;

        private Hook<RaptureGearsetDelegate> onUpdateGearsetHook;

        private unsafe delegate void RaptureGearsetDelegate(RaptureShellModule* RaptureShellModule, RaptureGearsetModule* GearsetStuff);

        public static bool HideWindows = true;

        public unsafe Plugin(IDalamudPluginInterface pluginInterface, IChatGui chat, ICommandManager commands, IFramework framework, ISigScanner sigScanner)
        {
            PluginInterface = pluginInterface;
            Chat = chat;
            Framework = framework;
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

            // Load all of our commands
            this.commandManager = new PluginCommandManager<Plugin>(this, commands);

            Framework.Update += OnFrameworkUpdate;

            onUpdateGearsetHook = HookProvider.HookFromAddress<RaptureGearsetDelegate>(new nint(RaptureGearsetModule.MemberFunctionPointers.UpdateGearset), OnUpdateGearset);
            onUpdateGearsetHook.Enable();

            Functions.GetChatSignatures(sigScanner);
        }

        private unsafe void OnUpdateGearset(RaptureShellModule* RaptureShellModule, RaptureGearsetModule* GearsetStuff)
        {
            onUpdateGearsetHook?.Original(RaptureShellModule, GearsetStuff);
            if (PluginConfig.AutoUpdatePortaitFromGearsetUpdate)
            {
                if (PluginConfig.ShowMessageInChatWhenAutoUpdatingPortaitFromGearsetUpdate)
                {
                    SavePortait("Gearset updated");
                }
                else
                {
                    SavePortait("", true);
                }
            }
        }

        [Command("/pfixsave")]
        [HelpMessage("Updates the portait for the currently selected gearset/equipment")]
        public unsafe void PortraitFixSave(string command, string args)
        {
            var addon = (AtkUnitBase*)GameGui.GetAddonByName("Character");
            if (addon == null || addon->IsVisible == false)
            {
                Chat.PrintError("You can only use this command while the character menu is open.");
                return;
            }
            SavePortait();
        }

        [Command("/pfixconfig")]
        [HelpMessage("Show the Portait Fixer settings")]
        public void ToggleConfig(string command, string args)
        {
            ui.IsVisible = !ui.IsVisible;
        }

        public static void SavePortait(string ExtraInfo = "", bool Silent = false)
        {
            actionQueue.Enqueue(OpenGearSetMenu);
            actionQueue.Enqueue(RightClickOnGearSet);
            actionQueue.Enqueue(OpenPortaitMenu);
            actionQueue.Enqueue(CheckForPortraitEditor);
            actionQueue.Enqueue(VariableDelay(100));
            actionQueue.Enqueue(PressSaveOnPortaitMenu);
            actionQueue.Enqueue(VariableDelay(100));
            actionQueue.Enqueue(ClosePortraitMenu);
            Chat.Print("[PortraitFixer] Portait Saved!");
            if (!Silent)
            {
                if (ExtraInfo == "")
                {
                    Chat.Print("[PortraitFixer] Portait saved!");
                }
                else
                {
                    Chat.Print("[PortraitFixer] " + ExtraInfo + " - portait saved!");
                }
            }
        }

        public static Func<bool> VariableDelay(uint frameDelay)
        {
            return () =>
            {
                Delay = frameDelay;
                return true;
            };
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

            if (sw.ElapsedMilliseconds > 3000)
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
                PluginLog.Error($"Failed: {ex.ToString()}");
            }
        }

        public static unsafe bool OpenGearSetMenu()
        {
            var addon = (AtkUnitBase*)GameGui.GetAddonByName("Character");
            if (addon == null || addon->IsVisible == false) return false;
            PluginLog.Debug("Found Character Menu");
            //var gearsetName = MemoryHelper.ReadStringNullTerminated((nint)gearset->Name);
            GenerateCallback(addon, 14, 2297, 454);
            var nextAddon = (AtkUnitBase*)GameGui.GetAddonByName("GearSetList");
            if (nextAddon == null) return false;
            if (HideWindows) nextAddon->Hide(true, true, 0);
            PluginLog.Debug("Found GearSetList");
            return true;
        }

        public static unsafe bool RightClickOnGearSet()
        {
            var addon = (AtkUnitBase*)GameGui.GetAddonByName("GearSetList");
            if (addon == null) return false;
            PluginLog.Debug("Found GearSetList");
            var module = RaptureGearsetModule.Instance();
            var currentGearsetIndex = module->CurrentGearsetIndex;
            //Chat.Print("CurrentGearsetIndex: " + currentGearsetIndex);
            if (!module->IsValidGearset(currentGearsetIndex)) { Chat.Print("This isn't a valid gearset? Aborting..."); actionQueue.Clear(); return true; }
            //var gearset = module->GetGearset(currentGearsetIndex);
            //if (gearset == null) { Chat.Print("This isn't a valid gearset? Aborting..."); actionQueue.Clear(); return true; }
            GenerateCallback(addon, 5, currentGearsetIndex);
            var nextAddon = (AtkUnitBase*)GameGui.GetAddonByName("ContextMenu");
            if (nextAddon == null) return false;
            if (HideWindows) nextAddon->Hide(true, true, 0);
            PluginLog.Debug("Found ContextMenu");
            return true;
        }

        public static unsafe bool OpenPortaitMenu()
        {
            var previousAddon = (AtkUnitBase*)GameGui.GetAddonByName("BannerEditor");
            if (previousAddon != null)
            {
                PluginLog.Debug("Found BannerEditor, not trying to open again");
                return true;
            }
            var addon = (AtkUnitBase*)GameGui.GetAddonByName("ContextMenu");
            if (addon == null) return false;
            PluginLog.Debug("Found ContextMenu");
            if (HideWindows) addon->Hide(true, true, 0);
            GenerateCallback(addon, 0, 9, 0);
            var ChangeGearSetNameAddon = (AtkUnitBase*)GameGui.GetAddonByName("InputString");
            if (ChangeGearSetNameAddon != null)
            {
                PluginLog.Debug("Uh oh, rename opened instead!");
                ChangeGearSetNameAddon->Close(false);
                GenerateCallback(addon, 0, 8, 0);
            }
            //If we open the rename thing then close it and then try 0,9,0

            actionQueue.Dequeue();
            return true;
        }

        //public static unsafe bool GetStrings()
        //{
        //    //var addon = (AtkUnitBase*)GameGui.GetAddonByName("ContextMenu");
        //    //if (addon == null) return false;
        //    //PluginLog.Debug("Found ContextMenu");
        //    ////GenerateCallback(addon->GetTextNodeById(31009)->unitb(),1);

        //    for (var x = 0; x < 50; x++)
        //    {
        //        var stringArray = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->UIModule->GetRaptureTextModule()->GetAddonText;

        //        if (stringArray == null) continue;

        //        for (var i = 0; i < 16; i++)
        //        {
        //            var n = stringArray->StringArray[i];
        //            if (n == null) continue;
        //            var s = MemoryHelper.ReadStringNullTerminated(new IntPtr(n));
        //            if (s.Contains("Edit Portrait"))
        //            {
        //                PluginLog.Debug("Edit Portrait found at " + x);
        //            }
        //            if (s.Trim().Length == 0) continue;
        //            //PluginLog.Debug("s " + s);

        //        }
        //    }
        //    return true;
        //}

        //public static unsafe bool OpenPortaitMenuV2()
        //{
        //    Functions.Send("/portrait");
        //    return true;
        //}


        //public static unsafe bool CheckForPortaitListV2()
        //{
        //    try
        //    {
        //        var nextAddon = (AtkUnitBase*)GameGui.GetAddonByName("BannerList");
        //        PluginLog.Debug("where plogon");
        //        if (nextAddon != null)
        //        {
        //            if (nextAddon->IsFullyLoaded())
        //            {
        //                PluginLog.Debug("Found Portrait List!");
        //                //nextAddon->Hide(true, true, 0);
        //                return true;
        //            }
        //            else
        //            {
        //                PluginLog.Debug("Didn't find Portrait List :c");
        //                return false;
        //            }
        //        }
        //        else
        //        {
        //            PluginLog.Debug("Didn't find Portrait List :c");
        //            return false;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        PluginLog.Error(e.ToString());
        //        return false;
        //    }
        //}

        //public static unsafe bool SelectPortraitV2()
        //{
        //    try
        //    {
        //        var addon = (AtkUnitBase*)GameGui.GetAddonByName("BannerList");
        //        if (addon == null)
        //        {
        //            PluginLog.Debug("Couldn't find BannerList?");
        //            return false;
        //        }
        //        PluginLog.Debug("Found BannerList");
        //        var module = RaptureGearsetModule.Instance();
        //        var currentGearsetIndex = module->CurrentGearsetIndex;
        //        //Chat.Print("CurrentGearsetIndex: " + currentGearsetIndex);
        //        if (!module->IsValidGearset(currentGearsetIndex)) { Chat.Print("This isn't a valid gearset? Aborting..."); actionQueue.Clear(); return true; }
        //        //var gearset = module->GetGearset(currentGearsetIndex);
        //        //if (gearset == null) { Chat.Print("This isn't a valid gearset? Aborting..."); actionQueue.Clear(); return true; }
        //        GenerateCallback(addon, 3, currentGearsetIndex);
        //        Delay = 500;
        //        GenerateCallback(addon, 2, currentGearsetIndex);
        //        addon = (AtkUnitBase*)GameGui.GetAddonByName("ContextMenu");
        //        if (addon == null) return false;
        //        PluginLog.Debug("Found ContextMenu");
        //        //addon->Hide(true, true, 0);
        //        GenerateCallback(addon, 0, 0);
        //        return true;
        //    }
        //    catch (Exception e)
        //    {
        //        PluginLog.Error(e.ToString());
        //        return false;
        //    }
        //}

        public static unsafe bool CheckForPortraitEditor()
        {
            try
            {
                var nextAddon = (AtkUnitBase*)GameGui.GetAddonByName("BannerEditor");
                PluginLog.Debug("where plogon");
                if (nextAddon != null)
                {
                    if (nextAddon->IsFullyLoaded())
                    {
                        PluginLog.Debug("Found Portrait Editor!");
                        if (HideWindows) nextAddon->Hide(true, true, 0);
                        return true;
                    }
                    else
                    {
                        PluginLog.Debug("Didn't find Portrait Editor :c");
                        return false;
                    }
                }
                else
                {
                    PluginLog.Debug("Didn't find Portrait Editor :c");
                    return false;
                }
            }
            catch (Exception e)
            {
                PluginLog.Error(e.ToString());
                return false;
            }
        }

        public unsafe static bool PressSaveOnPortaitMenu()
        {
            var addon = (AtkUnitBase*)GameGui.GetAddonByName("BannerEditor");
            if (addon == null) return false;
            PluginLog.Debug("Found BannerEditor");
            addon->Hide(true, true, 0);
            GenerateCallback(addon, 0, 9, -1, -1);
            PluginLog.Debug("Clicked Save");
            actionQueue.Dequeue();
            addon->Close(false);
            var nextAddon = (AtkUnitBase*)GameGui.GetAddonByName("BannerEditor");
            if (nextAddon != null) return false;
            PluginLog.Debug("BannerEditor was closed!");
            return true;
        }


        public unsafe static bool ClosePortraitMenu()
        {
            var addon = (AtkUnitBase*)GameGui.GetAddonByName("BannerEditor");
            if (addon == null) return false;
            PluginLog.Debug("Found BannerEditor");
            addon->Close(false);
            var nextAddon = (AtkUnitBase*)GameGui.GetAddonByName("BannerEditor");
            if (nextAddon != null) return false;
            PluginLog.Debug("BannerEditor was closed!");
            return true;
        }

        public static unsafe bool CloseGearSetMenu()
        {
            var addon = (AtkUnitBase*)GameGui.GetAddonByName("GearSetList");
            if (addon == null || addon->IsVisible == false) return false;
            PluginLog.Debug("Found GearSetList");
            addon->Close(true);
            var nextAddon = (AtkUnitBase*)GameGui.GetAddonByName("GearSetList");
            if (nextAddon == null) return false;
            PluginLog.Debug("Found GearSetList");
            return true;
        }

        public static unsafe void GenerateCallback(AtkUnitBase* unitBase, params object[] values)
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
                            atkValues[i].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt;
                            atkValues[i].UInt = uintValue;
                            break;

                        case int intValue:
                            atkValues[i].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int;
                            atkValues[i].Int = intValue;
                            break;

                        case float floatValue:
                            atkValues[i].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Float;
                            atkValues[i].Float = floatValue;
                            break;

                        case bool boolValue:
                            atkValues[i].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Bool;
                            atkValues[i].Byte = (byte)(boolValue ? 1 : 0);
                            break;

                        case string stringValue:
                            {
                                atkValues[i].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String;
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
                    if (atkValues[i].Type == FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String)
                    {
                        Marshal.FreeHGlobal(new IntPtr(atkValues[i].String));
                    }
                }
                Marshal.FreeHGlobal(new IntPtr(atkValues));
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

            Framework.Update -= OnFrameworkUpdate;
            onUpdateGearsetHook.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);

            //Marshal.FreeHGlobal(textPtr);
        }

        #endregion IDisposable Support
    }
}