#undef DEBUG

using Dalamud.ContextMenu;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Veda;
using static PotatoFamine2.Plugin;

namespace PotatoFamine2
{
    public class Plugin : IDalamudPlugin
    {
        private const uint CHARA_WINDOW_ACTOR_ID = 0xE0000000;

        private static readonly short[,] RACE_STARTER_GEAR_ID_MAP =
        {
            {84, 85}, // Hyur
            {86, 87}, // Elezen
            {92, 93}, // Lalafell
            {88, 89}, // Miqo
            {90, 91}, // Roe
            {257, 258}, // Au Ra
            {597, -1}, // Hrothgar
            {-1, 581}, // Viera
        };

        private static readonly short[] RACE_STARTER_GEAR_IDS;

        public string Name => "Potato Famine 2";

        [PluginService] public static DalamudPluginInterface PluginInterface { get; set; }
        [PluginService] public static ObjectTable ObjectTable { get; set; }
        [PluginService] public static CommandManager Commands { get; set; }
        [PluginService] public static SigScanner SigScanner { get; set; }
        [PluginService] public static ClientState ClientState { get; set; }
        [PluginService] public static ObjectTable Objects { get; private set; } = null!;
        [PluginService] public static ChatGui Chat { get; set; }

        [Flags]
        public enum DrawState : uint
        {
            Invisibility = 0x00_00_00_02,
            IsLoading = 0x00_00_08_00,
            SomeNpcFlag = 0x00_00_01_00,
            MaybeCulled = 0x00_00_04_00,
            MaybeHiddenMinion = 0x00_00_80_00,
            MaybeHiddenSummon = 0x00_80_00_00,
        }

        public static Configuration PluginConfig { get; set; }

        public static bool unsavedConfigChanges = false;

        private PluginUI ui;
        public static bool SettingsVisible = false;

        private delegate IntPtr CharacterIsMount(IntPtr actor);

        private delegate IntPtr CharacterInitialize(IntPtr actorPtr, IntPtr customizeDataPtr);

        private delegate IntPtr FlagSlotUpdate(IntPtr actorPtr, uint slot, IntPtr equipData);

        private Hook<CharacterIsMount> charaMountedHook;
        private Hook<CharacterInitialize> charaInitHook;
        private Hook<FlagSlotUpdate> flagSlotUpdateHook;

        private IntPtr lastActor;
        private bool lastWasPlayer;
        private bool lastWasSelf;
        private bool lastWasModified;

        private Race lastPlayerRace;
        private byte lastPlayerGender;

        private DalamudContextMenu contextMenu = new DalamudContextMenu();

        //This sucks, but here we are
        static Plugin()
        {
            var list = new List<short>();
            foreach (short id in RACE_STARTER_GEAR_ID_MAP)
            {
                if (id != -1)
                {
                    list.Add(id);
                }
            }

            RACE_STARTER_GEAR_IDS = list.ToArray();
        }

        public Plugin(ChatGui chat)
        {
            PluginConfig = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            PluginConfig.Initialize(PluginInterface);

            this.ui = new PluginUI(this);

            PluginInterface.UiBuilder.Draw += this.ui.Draw;
            PluginInterface.UiBuilder.OpenConfigUi += OpenSettingsMenu;

            Chat = chat;

            // add event handler
            contextMenu.OnOpenGameObjectContextMenu += OpenGameObjectContextMenu;

            Commands.AddHandler(
                "/potato",
                new CommandInfo(this.OpenSettingsMenuCommand)
                {
                    HelpMessage = "Opens the Potato Famine 2 settings menu.",
                    ShowInHelp = true
                }
            );

            var charaIsMountAddr =
                SigScanner.ScanText("40 53 48 83 EC 20 48 8B 01 48 8B D9 FF 50 10 83 F8 08 75 08");
            PluginLog.Log($"Found IsMount address: {charaIsMountAddr.ToInt64():X}");
            this.charaMountedHook ??=
                new Hook<CharacterIsMount>(charaIsMountAddr, CharacterIsMountDetour);
            this.charaMountedHook.Enable();

            var charaInitAddr = SigScanner.ScanText(
                "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 48 8B F9 48 8B EA 48 81 C1 ?? ?? ?? ?? E8 ?? ?? ?? ??");
            PluginLog.Log($"Found Initialize address: {charaInitAddr.ToInt64():X}");
            this.charaInitHook ??=
                new Hook<CharacterInitialize>(charaInitAddr, CharacterInitializeDetour);
            this.charaInitHook.Enable();

            var flagSlotUpdateAddr =
                SigScanner.ScanText(
                    "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 8B DA 49 8B F0 48 8B F9 83 FA 0A");
            PluginLog.Log($"Found FlagSlotUpdate address: {flagSlotUpdateAddr.ToInt64():X}");
            this.flagSlotUpdateHook ??=
                new Hook<FlagSlotUpdate>(flagSlotUpdateAddr, FlagSlotUpdateDetour);
            this.flagSlotUpdateHook.Enable();

            // Trigger an initial refresh of all players
            RefreshAllPlayers();

            //chat.Print(Functions.BuildSeString(this.Name, "Successfully loaded the first part!", LogType.Success));
        }


        //private void OpenSettingsMenu(string command, string args)
        //    {
        //    this.SettingsVisible = true;
        //}

        private void OpenGameObjectContextMenu(GameObjectContextMenuOpenArgs args)
        {
            //Chat.Print(args.Text.TextValue);
            //Chat.Print(args.ParentAddonName);
            if (args.ObjectWorld == 0 || args.ObjectWorld == 65535 || args.Text.TextValue.Contains(" ") == false)
            {
                //Not a player
                return;
            }
            args.AddCustomItem(
                !Functions.ListContainsPlayer(PluginConfig.TrustedList, args.Text.TextValue, args.ObjectWorld)
                    ? new GameObjectContextMenuItem(
                        new SeString(
                            new UIForegroundPayload(539),
                            new TextPayload($"{SeIconChar.BoxedLetterP.ToIconString()} "),
                            new UIForegroundPayload(0),
                            new TextPayload("Add to Trusted")),
                        ToggleTrusted)
                    : new GameObjectContextMenuItem(
                        new SeString(
                            new UIForegroundPayload(539),
                            new TextPayload($"{SeIconChar.BoxedLetterP.ToIconString()} "),
                            new UIForegroundPayload(0),
                            new TextPayload("Remove from Trusted")),
                        ToggleTrusted));
            args.AddCustomItem(
                !Functions.ListContainsPlayer(PluginConfig.ForciblyChangeList, args.Text.TextValue, args.ObjectWorld)
                    ? new GameObjectContextMenuItem(
                        new SeString(
                            new UIForegroundPayload(539),
                            new TextPayload($"{SeIconChar.BoxedLetterP.ToIconString()} "),
                            new UIForegroundPayload(0),
                            new TextPayload("Add to Forcibly Change")),
                        ToggleForciblyChange)
                    : new GameObjectContextMenuItem(
                        new SeString(
                            new UIForegroundPayload(539),
                            new TextPayload($"{SeIconChar.BoxedLetterP.ToIconString()} "),
                            new UIForegroundPayload(0),
                            new TextPayload("Remove from Forcibly Change")),
                        ToggleForciblyChange));
        }

        public void ToggleTrusted(GameObjectContextMenuItemSelectedArgs args)
        {
            try
            {
                string PlayerName = args.Text.TextValue;
                ushort WorldID = args.ObjectWorld;
                if (!Functions.ListContainsPlayer(PluginConfig.TrustedList, PlayerName, WorldID))
                {
                    PlayerData TempPlayerData = new PlayerData
                    {
                        Firstname = PlayerName.Split(' ')[0],
                        Lastname = PlayerName.Split(' ')[1],
                        Name = PlayerName,
                        HomeworldId = WorldID,
                        HomeworldName = "WIP",
                        Time = DateTime.Now.ToString("G")

                    };
                    PluginConfig.TrustedList.Add(TempPlayerData);
                    PluginConfig.Save();
                    Chat.Print(Functions.BuildSeString("PF2", PlayerName + " was added to the trusted list."));
                }
                else
                {
                    PluginConfig.TrustedList.RemoveAt(PluginConfig.TrustedList.FindIndex(x => x.Name == PlayerName));
                    PluginConfig.Save();
                    Chat.Print(Functions.BuildSeString("PF2", PlayerName + " was removed from the trusted list."));
                }
                RerenderActor(ObjectTable.SearchById(args.ObjectId));
            }
            catch (Exception f)
            {
                Chat.Print(Functions.BuildSeString("PF2", "Something went wrong - " + f.ToString(), ColorType.Error));
            }
        }

        public void ToggleForciblyChange(GameObjectContextMenuItemSelectedArgs args)
        {
            try
            {
                string PlayerName = args.Text.TextValue;
                ushort WorldID = args.ObjectWorld;
                if (!Functions.ListContainsPlayer(PluginConfig.ForciblyChangeList, PlayerName, WorldID))
                {
                    PlayerData TempPlayerData = new PlayerData
                    {
                        Firstname = PlayerName.Split(' ')[0],
                        Lastname = PlayerName.Split(' ')[1],
                        Name = PlayerName,
                        HomeworldId = WorldID,
                        HomeworldName = "WIP",
                        Time = DateTime.Now.ToString("G")

                    };
                    PluginConfig.ForciblyChangeList.Add(TempPlayerData);
                    unsavedConfigChanges = true;
                    SaveConfig();
                    Chat.Print(Functions.BuildSeString("PF2", PlayerName + " was added to the \"Forcibly Change\" list."));
                }
                else
                {
                    PluginConfig.ForciblyChangeList.RemoveAt(PluginConfig.ForciblyChangeList.FindIndex(x => x.Name == PlayerName));
                    unsavedConfigChanges = true;
                    SaveConfig();
                    Chat.Print(Functions.BuildSeString("PF2", PlayerName + " was removed from the \"Forcibly Change\" list."));
                }
                RerenderActor(ObjectTable.SearchById(args.ObjectId));
            }
            catch (Exception f)
            {
                Chat.Print(Functions.BuildSeString("PF2", "Something went wrong - " + f.ToString(), ColorType.Error));
            }
        }

        private IntPtr CharacterIsMountDetour(IntPtr actorPtr)
        {
            // TODO: use native FFXIVClientStructs unsafe methods?
            if (Marshal.ReadByte(actorPtr + 0x8C) == (byte)ObjectKind.Player)
            {
                lastActor = actorPtr;
                lastWasPlayer = true;
            }
            else
            {
                lastWasPlayer = false;
            }

            return charaMountedHook.Original(actorPtr);
        }

        private IntPtr CharacterInitializeDetour(IntPtr drawObjectBase, IntPtr customizeDataPtr)
        {
            if (lastWasPlayer)
            {
                var actor = ObjectTable.CreateObjectReference(lastActor);
                lastWasModified = false;
                lastWasSelf = false;

                if (actor != null &&
                    (actor.ObjectId != CHARA_WINDOW_ACTOR_ID || PluginConfig.ImmersiveMode)
                    && ClientState.LocalPlayer != null
                    && actor.ObjectId != ClientState.LocalPlayer.ObjectId
                    && PluginConfig.ShouldChangeOthers)
                {
                    if (PluginConfig.UseTrustedList) //If they're in the trusted list, don't change them
                    {
                        if (!Functions.ListContainsPlayer(PluginConfig.TrustedList, actor.Name.TextValue))
                        {
                            this.ChangeRace(customizeDataPtr, PluginConfig.ChangeOthersTargetRace);
                        }
                        
                    }
                    else //Change them
                    {
                        this.ChangeRace(customizeDataPtr, PluginConfig.ChangeOthersTargetRace);
                    }
                }

                if (actor != null &&
                    (actor.ObjectId != CHARA_WINDOW_ACTOR_ID || PluginConfig.ImmersiveMode)
                    && ClientState.LocalPlayer != null
                    && actor.ObjectId != ClientState.LocalPlayer.ObjectId
                    && PluginConfig.ForciblyChangePeople && Functions.ListContainsPlayer(PluginConfig.ForciblyChangeList, actor.Name.TextValue))
                {
                    //They're in the forcibly change list so we're changing them
                    this.ChangeRace(customizeDataPtr, PluginConfig.ForciblyChangePeopleTargetRace, true);
                }

                if (actor != null &&
                    (actor.ObjectId != CHARA_WINDOW_ACTOR_ID || PluginConfig.ImmersiveMode)
                    && ClientState.LocalPlayer != null
                    && actor.ObjectId == ClientState.LocalPlayer.ObjectId
                    && PluginConfig.ChangeSelf)
                {
                    lastWasSelf = true;
                    this.ChangeRace(customizeDataPtr, PluginConfig.ChangeSelfTargetRace);
                }
            }
            return charaInitHook.Original(drawObjectBase, customizeDataPtr);
        }

        private void ChangeRace(IntPtr customizeDataPtr, Race targetRace, bool ForceChange = false)
        {
            var customData = Marshal.PtrToStructure<CharaCustomizeData>(customizeDataPtr);
            
            bool lalaBool;

            if (PluginConfig.OnlyChangeLalafells && lastWasSelf == false && ForceChange == false)
            {
                lalaBool = customData.Race == Race.LALAFELL;
            }
            else
            {
                lalaBool = customData.Race != targetRace;
            }
            lastWasSelf = false;

            if (lalaBool)
            {
                // Modify the race/tribe accordingly
                customData.Race = targetRace;
                customData.Tribe = (byte)((byte)customData.Race * 2 - customData.Tribe % 2);

                // Special-case Hrothgar gender to prevent fuckery
                customData.Gender = targetRace switch
                {
                    Race.HROTHGAR => 0, // Force male for Hrothgar
                    _ => customData.Gender
                };

                // TODO: Re-evaluate these for valid race-specific values? (These are Lalafell values)
                // Constrain face type to 0-3 so we don't decapitate the character
                customData.FaceType %= 4;

                // Constrain body type to 0-1 so we don't crash the game
                customData.ModelType %= 2;

                // Hrothgar have a limited number of lip colors?
                customData.LipColor = targetRace switch
                {
                    Race.HROTHGAR => (byte)(customData.LipColor % 5 + 1),
                    _ => customData.LipColor
                };

                customData.HairStyle = (byte)(customData.HairStyle % RaceMappings.RaceHairs[targetRace] + 1);

                Marshal.StructureToPtr(customData, customizeDataPtr, true);

                // Record the new race/gender for equip model mapping, and mark the equip as dirty
                lastPlayerRace = customData.Race;
                lastPlayerGender = customData.Gender;
                lastWasModified = true;
            }
        }

        private IntPtr FlagSlotUpdateDetour(IntPtr actorPtr, uint slot, IntPtr equipDataPtr)
        {
            if (lastWasPlayer && lastWasModified)
            {
                var equipData = Marshal.PtrToStructure<EquipData>(equipDataPtr);
                // TODO: Handle gender-locked gear for Viera/Hrothgar
                equipData = MapRacialEquipModels(lastPlayerRace, lastPlayerGender, equipData);
                Marshal.StructureToPtr(equipData, equipDataPtr, true);
            }

            return flagSlotUpdateHook.Original(actorPtr, slot, equipDataPtr);
        }

        public bool SaveConfig()
        {
            if (Plugin.unsavedConfigChanges)
            {
                PluginConfig.Save();
                Plugin.unsavedConfigChanges = false;
                this.RefreshAllPlayers();
                return true;
            }

            return false;
        }

        public void OnlyChangeLalafells(bool onlyChangeLalafells)
        {
            if (PluginConfig.OnlyChangeLalafells == onlyChangeLalafells)
            {
                return;
            }

            PluginLog.Log($"Only change lalafells players toggled to {onlyChangeLalafells}, refreshing players");
            PluginConfig.OnlyChangeLalafells = onlyChangeLalafells;
            unsavedConfigChanges = true;
        }

        public void ToggleOtherRace(bool changeRace)
        {
            if (PluginConfig.ShouldChangeOthers == changeRace)
            {
                return;
            }

            PluginLog.Log($"Target race for other players toggled to {changeRace}, refreshing players");
            PluginConfig.ShouldChangeOthers = changeRace;
            unsavedConfigChanges = true;
        }

        public void ToggleChangeSelf(bool changeSelf)
        {
            if (PluginConfig.ChangeSelf == changeSelf)
            {
                return;
            }

            PluginLog.Log($"Target race for player toggled to {changeSelf}, refreshing players");
            PluginConfig.ChangeSelf = changeSelf;
            unsavedConfigChanges = true;
        }

        public void ToggleForciblyChangeOption(bool ForciblyChangePeople)
        {
            if (PluginConfig.ForciblyChangePeople == ForciblyChangePeople)
            {
                return;
            }

            PluginLog.Log($"Player toggled Forcibly Changed list, refreshing player");
            PluginConfig.ForciblyChangePeople = ForciblyChangePeople;
            unsavedConfigChanges = true;
        }

        public void ToggleTrustedOption(bool UseTrustedList)
        {
            if (PluginConfig.UseTrustedList == UseTrustedList)
            {
                return;
            }

            PluginLog.Log($"Player toggled Trusted players, refreshing player");
            PluginConfig.UseTrustedList = UseTrustedList;
            unsavedConfigChanges = true;
        }

        public void UpdateOtherRace(Race race)
        {
            if (PluginConfig.ChangeOthersTargetRace == race)
            {
                return;
            }

            PluginLog.Log($"Target race for other players changed to {race}, refreshing players");
            PluginConfig.ChangeOthersTargetRace = race;
            unsavedConfigChanges = true;
        }

        public void UpdateForciblyChangeRace(Race race)
        {
            if (PluginConfig.ForciblyChangePeopleTargetRace == race)
            {
                return;
            }

            PluginLog.Log($"Target race for forcibly changed players changed to {race}, refreshing players");
            PluginConfig.ForciblyChangePeopleTargetRace = race;
            unsavedConfigChanges = true;
        }

        public void UpdateSelfRace(Race race)
        {
            if (PluginConfig.ChangeSelfTargetRace == race)
            {
                return;
            }

            PluginLog.Log($"Target race for player changed to {race}, refreshing players");
            PluginConfig.ChangeSelfTargetRace = race;
            unsavedConfigChanges = true;
        }

        public void UpdateImmersiveMode(bool immersiveMode)
        {
            if (PluginConfig.ImmersiveMode == immersiveMode)
            {
                return;
            }

            PluginLog.Log($"Immersive mode set to {immersiveMode}, refreshing players");
            PluginConfig.ImmersiveMode = immersiveMode;
            unsavedConfigChanges = true;
        }

        public async void RefreshAllPlayers()
        {
            // Workaround to prevent literally genociding the actor table if we load at the same time as Dalamud + Dalamud is loading while ingame
            await Task.Delay(100); // LMFAOOOOOOOOOOOOOOOOOOO
            var localPlayer = ClientState.LocalPlayer;
            if (localPlayer == null)
            {
                return;
            }
            for (var i = 0; i < ObjectTable.Length; i++)
            {
                var actor = ObjectTable[i];

                if (actor != null && actor.ObjectKind == ObjectKind.Player)
                {
                    RerenderActor(actor);
                }
            }
        }

        private async void RerenderActor(GameObject actor)
        {
            try
            {
                //VisibilityManager.Redraw(actor);
                VisibilityManager.MakeInvisible(actor);
                await Task.Delay(100);
                VisibilityManager.MakeVisible(actor);
            }
            catch (Exception ex)
            {
                PluginLog.LogError(ex.ToString());
            }
        }

        private EquipData MapRacialEquipModels(Race race, int gender, EquipData eq)
        {
            if (Array.IndexOf(RACE_STARTER_GEAR_IDS, eq.model) > -1)
            {
#if DEBUG
                PluginLog.Log($"Modified {eq.model}, {eq.variant}");
                PluginLog.Log($"Race {race}, index {(byte) (race - 1)}, gender {gender}");
#endif
                eq.model = RACE_STARTER_GEAR_ID_MAP[(byte)race - 1, gender];
                eq.variant = 1;
#if DEBUG
                PluginLog.Log($"New {eq.model}, {eq.variant}");
#endif
            }

            return eq;
        }

        //[Command("/potato")]
        //[HelpMessage("Toggles auto fate syncing on/off.")]
        public void OpenSettingsMenuCommand(string command, string args)
        {
            OpenSettingsMenu();
        }

        private void OpenSettingsMenu()
        {
            SettingsVisible = true;
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            PluginInterface.UiBuilder.OpenConfigUi -= OpenSettingsMenu;
            PluginInterface.UiBuilder.Draw -= this.ui.Draw;
            this.SaveConfig();

            this.charaMountedHook.Disable();
            this.charaInitHook.Disable();
            this.flagSlotUpdateHook.Disable();

            this.charaMountedHook.Dispose();
            this.charaInitHook.Dispose();
            this.flagSlotUpdateHook.Dispose();

            // Refresh all players again
            RefreshAllPlayers();

            Commands.RemoveHandler("/potato");

            contextMenu.OnOpenGameObjectContextMenu -= OpenGameObjectContextMenu;
            contextMenu.Dispose();

            //this.pluginInterface.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }

    public sealed unsafe partial class VisibilityManager
    {
        public static DrawState* ActorDrawState(GameObject actor)
    => (DrawState*)(&((FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)actor.Address)->RenderFlags);

        private static int ObjectTableIndex(GameObject actor)
    => ((FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)actor.Address)->ObjectIndex;

        public static void MakeInvisible(GameObject? actor)
        {
            if (BadRedrawIndices(actor, out var tableIndex))
            {
                return;
            }

            *ActorDrawState(actor!) |= DrawState.Invisibility;

            if (/*actor is PlayerCharacter &&*/ Objects[tableIndex + 1] is { ObjectKind: ObjectKind.MountType } mount)
            {
                *ActorDrawState(mount) |= DrawState.Invisibility;
            }
        }

        public static void MakeVisible(GameObject? actor)
        {
            if (BadRedrawIndices(actor, out var tableIndex))
            {
                return;
            }

            *ActorDrawState(actor!) &= ~DrawState.Invisibility;

            if (actor is PlayerCharacter && Objects[tableIndex + 1] is { ObjectKind: ObjectKind.MountType } mount)
            {
                *ActorDrawState(mount) &= ~DrawState.Invisibility;
            }
        }

        private static bool BadRedrawIndices(GameObject? actor, out int tableIndex)
        {
            if (actor == null)
            {
                tableIndex = -1;
                return true;
            }

            tableIndex = ObjectTableIndex(actor);
            return tableIndex is >= 240 and < 245;
        }
    }
}