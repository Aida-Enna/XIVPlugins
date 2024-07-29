using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Veda;

namespace AutoPillion
{
    public unsafe class Plugin : IDalamudPlugin
    {
        public string Name => "AutoPillion";

        [PluginService] public static IDalamudPluginInterface PluginInterface { get; set; }
        [PluginService] public static IDataManager Data { get; set; }
        [PluginService] public static IFramework Framework { get; set; }
        [PluginService] public static IGameGui GameGui { get; set; }
        [PluginService] public static IChatGui Chat { get; set; }
        [PluginService] public static IClientState ClientState { get; set; }
        [PluginService] public static IPartyList PartyList { get; set; }
        [PluginService] public static ITargetManager TargetManager { get; set; }

        private PluginUI ui;
        private bool TryingToMount = false;
        public static readonly Stopwatch AutoPillionCooldownTimer = new();
        public static Configuration PluginConfig { get; set; }

        public Plugin(IDalamudPluginInterface pluginInterface, IFramework framework)
        {
            PluginInterface = pluginInterface;
            Framework = framework;

            // Get or create a configuration object
            PluginConfig = (Configuration)PluginInterface.GetPluginConfig() ?? new Configuration();
            PluginConfig.Initialize(PluginInterface);

            Framework.Update += Framework_Update;

            ui = new PluginUI();
            PluginInterface.UiBuilder.Draw += new System.Action(ui.Draw);
            PluginInterface.UiBuilder.OpenConfigUi += () =>
            {
                PluginUI ui = this.ui;
                ui.IsVisible = !ui.IsVisible;
            };
        }

        private unsafe void Framework_Update(IFramework framework)
        {
            try
            {
                if (!ClientState.IsLoggedIn) { return; }
                var CurrentCharacterPtr = (Character*)null;
                try
                {
                    CurrentCharacterPtr = (Character*)ClientState.LocalPlayer.Address;
                }
                catch (Exception f)
                {
                    return;
                }
                if (CurrentCharacterPtr->IsMounted() || !ClientState.LocalPlayer.IsTargetable)
                {
                    if (AutoPillionCooldownTimer.IsRunning) AutoPillionCooldownTimer.Stop();
                    return;
                }
                else
                {
                    if (!AutoPillionCooldownTimer.IsRunning) { AutoPillionCooldownTimer.Restart(); }
                }
                if (AutoPillionCooldownTimer.ElapsedMilliseconds > Plugin.PluginConfig.CooldownInSeconds * 1000)
                {
                    TryingToMount = false;
                    AutoPillionCooldownTimer.Restart();
                }
                if (PartyList.Count() > 0 & !TryingToMount)
                {
                    int count = 0;
                    foreach (var partyMember in PartyList)
                    {
                        count++;
                        if (count == 1) { continue; }
                        try
                        {
                            if (partyMember.GameObject.YalmDistanceX > 3 || partyMember.GameObject.YalmDistanceZ > 3) { continue; }
                        }
                        catch (Exception f)
                        {
                            continue;
                        }
                        var characterPtr = (Character*)partyMember.GameObject.Address;
                        if (characterPtr->IsNotMounted()) { continue; }
                        if (characterPtr == null) continue;
                        var mountContainer = characterPtr->Mount;

                        var mountObjectID = mountContainer.MountId;
                        if (mountObjectID == 0) continue;
                        var mountRow = Data.GetExcelSheet<Mount>()?.GetRow(mountObjectID);
                        if (mountRow.ExtraSeats > 0)
                        {
                            TryingToMount = true;
                            TryToMount(partyMember.GameObject);
                        }
                        else
                        {
                            //Seatless behavior!
                        }
                    }
                }
            }
            catch (Exception f)
            {
                Chat.Print(f.ToString());
            }
        }

        public void TryToMount(IGameObject PartyMemberObject)
        {
            AutoPillionCooldownTimer.Restart();
            //Chat.Print("You can ride the mount " + partyMember.Name + " is on!");
            TargetManager.Target = PartyMemberObject;
            var ContextMenu = (AtkUnitBase*)GameGui.GetAddonByName("ContextMenu");
            //Chat.Print("Found ContextMenu");
            GenerateCallback(ContextMenu, 1);
            GenerateCallback(ContextMenu, 0, Functions.GetAddonEntries("ContextMenu").IndexOf("Ride Pillion"), 0);
            //var AddonContextSub = (AtkUnitBase*)GameGui.GetAddonByName("AddonContextSub");
            //if (AddonContextSub != null)
            //{
            //    Chat.Print("Found AddonContextSub");
            //    GenerateCallback(AddonContextSub, 0, 3, 0);
            //}
            TargetManager.Target = null;
            return;
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

        public static unsafe uint GetMountID(IPlayerCharacter playerCharacter)
        {
            var characterPtr = (Character*)playerCharacter.Address;
            if (characterPtr == null) return 0;
            var mountContainer = characterPtr->Mount;

            var mountObjectID = mountContainer.MountId;
            if (mountObjectID == 0) return 0;

            return mountObjectID;
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            Framework.Update -= Framework_Update;

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