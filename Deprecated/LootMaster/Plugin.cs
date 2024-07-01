using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;
using Veda;

namespace LootMaster
{
    public class Plugin : IDalamudPlugin, IDisposable
    {
        public string Name => "LootMaster";

        [PluginService] public static ICommandManager Commands { get; set; }
        [PluginService] public static IDalamudPluginInterface PluginInterface { get; set; }
        [PluginService] public static ISigScanner SigScanner { get; set; }
        [PluginService] public static IChatGui Chat { get; set; }
        [PluginService] public static IClientState Client { get; set; }
        [PluginService] public static IDataManager Data { get; set; }
        [PluginService] public static IClientState ClientState { get; set; }
        [PluginService] public static IPluginLog  PluginLog { get; set; }
        public static Configuration PluginConfig { get; set; }
        private PluginCommandManager<Plugin> commandManager;
        private static IntPtr lootsAddr;
        internal static RollItemRaw rollItemRaw;
        private readonly PluginUI ui;
        private bool InHighEndDuty = false;

        internal delegate void RollItemRaw(IntPtr lootIntPtr, RollOption option, uint lootItemIndex);

        public static List<LootItem> LootItems => ReadArray<LootItem>(lootsAddr + 16, 16).Where(i => i.ItemId != 3758096384U && i.ItemId > 0U).ToList();
        public Timer LootTimer = new System.Timers.Timer();
        public static List<LootItem> BlacklistedItems = new List<LootItem>();

        public Plugin(ICommandManager commands, IClientState client)
        {
            lootsAddr = SigScanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 89 44 24 60", 0);
            rollItemRaw = Marshal.GetDelegateForFunctionPointer<RollItemRaw>(SigScanner.ScanText("41 83 F8 ?? 0F 83 ?? ?? ?? ?? 48 89 5C 24 08"));
            PluginConfig = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            PluginConfig.Initialize(PluginInterface);
            ui = new PluginUI();
            PluginInterface.UiBuilder.Draw += new System.Action(ui.Draw);
            PluginInterface.UiBuilder.OpenConfigUi += () =>
           {
               PluginUI ui = this.ui;
               ui.IsVisible = !ui.IsVisible;
           };
            commandManager = new PluginCommandManager<Plugin>(this, commands);

            Client = client;

            Client.CfPop += CFPop;
            Client.TerritoryChanged += TerritoryChanged;

            LootTimer.Interval = 10000; //600000
            LootTimer.Elapsed += CheckLoot;
            LootTimer.Start();
        }

        private unsafe void CFPop(ContentFinderCondition queuedDuty)
        {
            if (PluginConfig.InventoryCheck && GetInventoryRemainingSpace() < 5)
            {
                Chat.Print(Functions.BuildSeString(this.Name, "<c518>Warning: <c518>You <c518>have <g518>" + GetInventoryRemainingSpace() + " <c518>slot(s) <c518>remaining <c518>in <c518>your <c518>inventory."));
            }
            if (PluginConfig.NotifyOnCFPop && PluginConfig.AutoRoll && queuedDuty.PvP == false)
            {
                if (queuedDuty.HighEndDuty && PluginConfig.DoNotRollInHighEndDuties)
                {
                    //Do nothing!
                }
                else
                {
                    Chat.Print(Functions.BuildSeString("LootMaster", "Your current <c17>auto-roll setting is <c573>" + PluginConfig.AutoRollOption.GetAttribute<Display>().Value.Replace(" ", " <c573>") + "</c>."));
                }
            }
        }

        private unsafe void TerritoryChanged(ushort Territory)
        {
            //Chat.Print("Moving territory to " + territoryTypeInfo.PlaceName.Value.Name);
            if (PluginConfig.DoNotRollInHighEndDuties)
            {
                var territoryTypeInfo = Data.GetExcelSheet<TerritoryType>()!.GetRow(ClientState.TerritoryType);
                if (territoryTypeInfo.ContentFinderCondition.Value.HighEndDuty)
                {
                    LootTimer.Enabled = false;
                    InHighEndDuty = true;
                }
                else
                {
                    LootTimer.Enabled = true;
                    InHighEndDuty = false;
                }
            }
        }

        private async void CheckLoot(object source, ElapsedEventArgs e)
        {
            try
            {
                if (PluginConfig.DoNotRollInHighEndDuties && InHighEndDuty)
                {
                    LootTimer.Enabled = false;
                    return;
                }
                if (LootItems.Count == 0 && BlacklistedItems.Count() > 0) { BlacklistedItems.Clear(); }
                if (LootItems.Any(x => (LootMaster.RollState)x.RollState == RollState.LootMasterNotDecided)) { return; }
                if (LootItems.Where(x => x.RollResult == 0).Count() > 0 && PluginConfig.AutoRoll)
                {
                    if (PluginConfig.DoNotRollInHighEndDuties)
                    {
                        var territoryTypeInfo = Data.GetExcelSheet<TerritoryType>()!.GetRow(ClientState.TerritoryType);
                        if (territoryTypeInfo.ContentFinderCondition.Value.HighEndDuty)
                        {
                            InHighEndDuty = true;
                            return;
                        }
                    }
                    //List<LootItem> CurrentLoot = LootItems;
                    LootTimer.Enabled = false;
                    //Chat.Print("There's some loot waiting, " + LootItems.Where(x => x.RollResult == 0).Count() + " pieces. We're gonna roll " + PluginConfig.AutoRollOption.GetAttribute<Display>().Value);
                    Random random = new Random();
                    int randomDelay = random.Next(PluginConfig.LowNum, (PluginConfig.HighNum + 1));

                    int RolledCount = 0;
                    int PassedCount = 0;
                    int BlacklistedCount = 0;
                    LootItem StupidShitCheck = new LootItem();
                    foreach (LootItem Item in LootItems.Where(x => x.RollResult == 0))
                    {
                        //Chat.Print("Item ID: " + Item.ItemId + "\nChest ID: " + Item.ChestObjectId);
                        //if (BlacklistedItems.Count > 0)
                        //{
                        //    Chat.Print("Blacklisted item ID: " + BlacklistedItems.First().ItemId + "\nBlacklisted chest ID: " + BlacklistedItems.First().ChestObjectId);
                        //    Chat.Print("Is item in blacklist? " + BlacklistedItems.FindIndex(x => x.ItemId == Item.ItemId && x.ChestObjectId == Item.ChestObjectId));
                        //}
                        if (BlacklistedItems.FindIndex(x => x.ItemId == Item.ItemId && x.ChestObjectId == Item.ChestObjectId) != -1)
                        {
                            BlacklistedCount++;
                            continue;
                        }
                        int ItemIndex = LootItems.FindIndex(x => x.ItemId == Item.ItemId && x.RollResult == 0);
                        try
                        {
                            StupidShitCheck = LootItems[ItemIndex];
                        }
                        catch (Exception f)
                        {
                            PluginLog.Information("Error getting item, already rolled?");
                            continue;
                        }
                        //This item is no longer in the Loot list?
                        try
                        {
                            StupidShitCheck = LootItems[ItemIndex];
                        }
                        catch (Exception f)
                        {
                            PluginLog.Information("Error getting item, already rolled?");
                            continue;
                        }
                        if (LootItems[ItemIndex].RollResult == 0 && (LootMaster.RollState)Item.RollState == RollState.UpToNeed && (PluginConfig.AutoRollOption == AutoRollOption.Need || PluginConfig.AutoRollOption == AutoRollOption.NeedThenGreed))
                        {
                            RollItem(RollOption.Need, ItemIndex);
                            await Task.Delay(500);
                            if (LootItems[ItemIndex].RollResult > 0)
                            {
                                RolledCount++;
                            }
                            else
                            {
                                if (!PluginConfig.PassOnFail)
                                {
                                    //Item failed to be rolled on and we're not gonna pass on it
                                    //Add it to the blacklist
                                    BlacklistedItems.Add(Item);
                                    Chat.Print(Functions.BuildSeString("LootMaster", "Couldn't autoroll " + PluginConfig.AutoRollOption.GetAttribute<Display>().Value + " on the <c575>" + Data.GameData.GetExcelSheet<Item>().GetRow(Item.ItemId)?.Name.RawString.Replace(" ", " <c575>") + " " + Data.GameData.GetExcelSheet<Item>().GetRow(Item.ItemId)?.ItemUICategory.Value.Name + "."));
                                }
                            }
                            if (PluginConfig.EnableDelay == true)
                            {
                                await Task.Delay(randomDelay);
                            }
                        }
                        //This item is no longer in the Loot list?
                        try
                        {
                            StupidShitCheck = LootItems[ItemIndex];
                        }
                        catch (Exception f)
                        {
                            PluginLog.Information("Error getting item, already rolled?");
                            continue;
                        }
                        if (LootItems[ItemIndex].RollResult == 0 && (LootMaster.RollState)Item.RollState == RollState.UpToGreed && (PluginConfig.AutoRollOption == AutoRollOption.Greed || PluginConfig.AutoRollOption == AutoRollOption.NeedThenGreed))
                        {
                            RollItem(RollOption.Greed, ItemIndex);
                            await Task.Delay(500);
                            if (LootItems[ItemIndex].RollResult > 0)
                            {
                                RolledCount++;
                            }
                            else
                            {
                                if (!PluginConfig.PassOnFail)
                                {
                                    //Item failed to be rolled on and we're not gonna pass on it
                                    //Add it to the blacklist
                                    BlacklistedItems.Add(Item);
                                    Chat.Print(Functions.BuildSeString("LootMaster", "Couldn't autoroll " + PluginConfig.AutoRollOption.GetAttribute<Display>().Value + " on the <c575>" + Data.GameData.GetExcelSheet<Item>().GetRow(Item.ItemId)?.Name.RawString.Replace(" ", " <c575>") + " " + Data.GameData.GetExcelSheet<Item>().GetRow(Item.ItemId)?.ItemUICategory.Value.Name + "."));
                                }
                            }
                            if (PluginConfig.EnableDelay == true)
                            {
                                await Task.Delay(randomDelay);
                            }
                        }
                        //This item is no longer in the Loot list?
                        try
                        {
                            StupidShitCheck = LootItems[ItemIndex];
                        }
                        catch (Exception f)
                        {
                            PluginLog.Information("Error getting item, already rolled?");
                            continue;
                        }
                        if (LootItems[ItemIndex].RollResult == 0 && (PluginConfig.AutoRollOption == AutoRollOption.Pass || PluginConfig.PassOnFail))
                        {
                            RollItem(RollOption.Pass, ItemIndex);
                            if (PluginConfig.PassOnFail)
                            {
                                PassedCount++;
                            }
                            else
                            {
                                RolledCount++;
                            }
                            if (PluginConfig.EnableDelay == true)
                            {
                                await Task.Delay(randomDelay);
                            }
                        }
                    }
                    LootTimer.Enabled = true;
                    if (!PluginConfig.EnableChatLogMessage) { return; }
                    if (RolledCount == 0 && PassedCount > 0)
                    {
                        Chat.Print(Functions.BuildSeString("LootMaster", "Couldn't auto " + PluginConfig.AutoRollOption.GetAttribute<Display>().Value + " on any items, passed on <c575>" + PassedCount + " item(s)."));
                    }
                    if (RolledCount == 0 && PassedCount == 0 && LootItems.Count() != BlacklistedItems.Count())
                    {
                        Chat.Print(Functions.BuildSeString("LootMaster", "Couldn't auto " + PluginConfig.AutoRollOption.GetAttribute<Display>().Value + " on any items."));
                    }
                    if (RolledCount > 0 && PassedCount == 0)
                    {
                        Chat.Print(Functions.BuildSeString("LootMaster", "Auto " + PluginConfig.AutoRollOption.GetAttribute<Display>().Value + "ed <c575>" + RolledCount.ToString() + " items(s)."));
                    }
                    if (RolledCount > 0 && PassedCount > 0)
                    {
                        Chat.Print(Functions.BuildSeString("LootMaster", "Auto " + PluginConfig.AutoRollOption.GetAttribute<Display>().Value + "ed <c575>" + RolledCount.ToString() + " items(s), passed on <c575>" + PassedCount + " item(s)."));
                    }
                }
            }
            catch (Exception f)
            {
                LootTimer.Enabled = true;

                Chat.PrintError("Error with checking for loot: " + Environment.NewLine + f.ToString());
            }
        }

        private void RollItem(RollOption option, int index)
        {
            try
            {
                //Chat.Print("Rolling " + option + " on item " + index);
                LootItem lootItem = LootItems[index];
                //PluginLog.Information(string.Format("{0} [{1}] {2} Id: {3:X} rollState: {4} rollOption: {5}", option, index, lootItem.ItemId, lootItem.ObjectId, lootItem.RollState, lootItem.RolledState), Array.Empty<object>());
                //Chat.Print(string.Format("{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}\n{7}\n{8}\n{9} ", lootItem.ChestItemIndex, lootItem.ChestObjectId, lootItem.ItemCount, lootItem.ItemId, lootItem.LootMode, lootItem.MaxTime, lootItem.RollResult, lootItem.RollState, lootItem.RollValue, lootItem.Time));
                rollItemRaw(lootsAddr, option, (uint)index);
            }
            catch (Exception f)
            {
                Chat.PrintError("Error with rolling: " + Environment.NewLine + f.ToString());
            }
        }

        private unsafe int GetInventoryRemainingSpace()
        {
            var empty = 0;
            foreach (var i in new[] { InventoryType.Inventory1, InventoryType.Inventory2, InventoryType.Inventory3, InventoryType.Inventory4 })
            {
                var c = InventoryManager.Instance()->GetInventoryContainer(i);
                if (c == null) continue;
                if (c->Loaded == 0) continue;
                for (var s = 0; s < c->Size; s++)
                {
                    var slot = c->GetInventorySlot(s);
                    if (slot->ItemID == 0) empty++;
                }
            }
            return empty;
        }

        //[Command("/lootdebug")]
        //[Aliases("/ld")]
        //[HelpMessage("Display debug loot info")]
        //public async void ShowDebugLootInfo(string command, string args)
        //{
        //    string FinalMessage = "";

        //    foreach (LootItem Item in LootItems)
        //    {
        //        FinalMessage += "Item ID: " + Item.ItemId + Environment.NewLine;
        //        FinalMessage += "Loot index: " + Item.ChestItemIndex + Environment.NewLine;
        //        FinalMessage += "Number of item: " + Item.ItemCount + Environment.NewLine;
        //        FinalMessage += "Loot Mode: " + Item.LootMode + Environment.NewLine;
        //        FinalMessage += "Roll Result: " + Item.RollResult + Environment.NewLine;
        //        FinalMessage += "Roll State: " + Item.RollState + Environment.NewLine;
        //        FinalMessage += "Roll Value: " + Item.RollValue + Environment.NewLine;
        //    }
        //    Chat.Print(FinalMessage);
        //}

        [Command("/lootmaster")]
        [Aliases("/loot")]
        [HelpMessage("Opens the lootmaster configuration menu.")]
        public async void OpenConfig(string command, string args)
        {
            PluginUI ui = this.ui;
            ui.IsVisible = !ui.IsVisible;
        }

        [Command("/need")]
        [HelpMessage("Roll need for everything. If impossible, roll greed. Else, roll pass.")]
        public async void NeedCommand(string command, string args)
        {
            Random random = new Random();
            int randomDelay = random.Next(PluginConfig.LowNum, (PluginConfig.HighNum + 1));
            int num1 = 0;
            int num2 = 0;
            int num3 = 0;
            for (int index = 0; index < LootItems.Count; ++index)
            {
                if (LootItems[index].RollResult == 0)
                {
                    if ((LootMaster.RollState)LootItems[index].RollState == RollState.UpToNeed)
                    {
                        RollItem(RollOption.Need, index);
                        ++num1;
                        if (PluginConfig.EnableDelay == true)
                        {
                            await Task.Delay(randomDelay);
                        }
                    }
                    else if (LootItems[index].RollResult == 0 && (LootMaster.RollState)LootItems[index].RollState == RollState.UpToGreed)
                    {
                        RollItem(RollOption.Greed, index);
                        ++num2;
                        if (PluginConfig.EnableDelay == true)
                        {
                            await Task.Delay(randomDelay);
                        }
                    }
                    else
                    {
                        RollItem(RollOption.Pass, index);
                        ++num3;
                        if (PluginConfig.EnableDelay == true)
                        {
                            await Task.Delay(randomDelay);
                        }
                    }
                }
            }
            if (!PluginConfig.EnableChatLogMessage)
                return;

            Chat.Print(Functions.BuildSeString(this.Name, "Needed <c575>" + num1.ToString() + " item(s), greeded <c575>" + num2.ToString() + " item(s), passed <c575>" + num3.ToString() + " item(s)."));
        }

        [Command("/needonly")]
        [HelpMessage("Roll need for everything. If impossible, roll pass.")]
        public async void NeedOnlyCommand(string command, string args)
        {
            Random random = new Random();
            int randomDelay = random.Next(PluginConfig.LowNum, (PluginConfig.HighNum + 1));

            int num1 = 0;
            int num2 = 0;
            for (int index = 0; index < LootItems.Count; ++index)
            {
                if (LootItems[index].RollResult == 0)
                {
                    if ((LootMaster.RollState)LootItems[index].RollState == RollState.UpToNeed)
                    {
                        RollItem(RollOption.Need, index);
                        ++num1;
                        if (PluginConfig.EnableDelay == true)
                        {
                            await Task.Delay(randomDelay);
                        }
                    }
                    else
                    {
                        RollItem(RollOption.Pass, index);
                        ++num2;
                        if (PluginConfig.EnableDelay == true)
                        {
                            await Task.Delay(randomDelay);
                        }
                    }
                }
            }
            if (!PluginConfig.EnableChatLogMessage)
                return;

            Chat.Print(Functions.BuildSeString(this.Name, "Needed only <c575>" + num1.ToString() + " item(s), passed <c575>" + num2.ToString() + " item(s)."));
        }

        [Command("/greed")]
        [HelpMessage("Greed on all items.")]
        public async void GreedCommand(string command, string args)
        {
            Random random = new Random();
            int randomDelay = random.Next(PluginConfig.LowNum, (PluginConfig.HighNum + 1));

            int num = 0;
            int num1 = 0;
            for (int index = 0; index < LootItems.Count; ++index)
            {
                if (LootItems[index].RollResult == 0)
                {
                    RollItem(RollOption.Greed, index);
                    ++num;
                    if (PluginConfig.EnableDelay == true)
                    {
                        await Task.Delay(randomDelay);
                    }
                }
            }
            if (!PluginConfig.EnableChatLogMessage)
                return;

            Chat.Print(Functions.BuildSeString(this.Name, "Greeded <c575>" + num.ToString() + " item(s)."));
        }

        [Command("/pass")]
        [HelpMessage("Pass on things you haven't rolled for yet.")]
        public async void PassCommand(string command, string args)
        {
            Random random = new Random();
            int randomDelay = random.Next(PluginConfig.LowNum, (PluginConfig.HighNum + 1));

            int num = 0;
            for (int index = 0; index < LootItems.Count; ++index)
            {
                if (LootItems[index].RollResult == 0)
                {
                    RollItem(RollOption.Pass, index);
                    ++num;
                    if (PluginConfig.EnableDelay == true)
                    {
                        await Task.Delay(randomDelay);
                    }
                }
            }
            if (!PluginConfig.EnableChatLogMessage)
                return;

            if (num == 0)
            {
                Chat.Print(Functions.BuildSeString(this.Name, "Passed on <c575>0 item(s)."));
            }
            else
            {
                Chat.Print(Functions.BuildSeString(this.Name, "Passed <c575>" + num.ToString() + " item(s)."));
            }
        }

        [Command("/passall")]
        [HelpMessage("Passes on all, even if you rolled on them previously.")]
        public async void PassAllCommand(string command, string args)
        {
            Random random = new Random();
            int randomDelay = random.Next(PluginConfig.LowNum, (PluginConfig.HighNum + 1));

            int num = 0;
            for (int index = 0; index < LootItems.Count; ++index)
            {
                if (LootItems[index].RollResult != RollResult.Passed)
                {
                    RollItem(RollOption.Pass, index);
                    ++num;
                    if (PluginConfig.EnableDelay == true)
                    {
                        await Task.Delay(randomDelay);
                    }
                }
            }
            if (!PluginConfig.EnableChatLogMessage)
                return;

            List<Payload> payloadList = null;
            if (num == 0)
            {
                Chat.Print(Functions.BuildSeString(this.Name, "Passed on <c575>0 item(s)."));
            }
            else
            {
                Chat.Print(Functions.BuildSeString(this.Name, "Passed on <c575>" + num.ToString() + " item(s)."));
            }
        }

        public static T[] ReadArray<T>(IntPtr unmanagedArray, int length) where T : struct
        {
            int num = Marshal.SizeOf(typeof(T));
            T[] objArray = new T[length];
            for (int index = 0; index < length; ++index)
            {
                IntPtr ptr = new(unmanagedArray.ToInt64() + index * num);
                objArray[index] = Marshal.PtrToStructure<T>(ptr);
            }
            return objArray;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            commandManager.Dispose();
            PluginInterface.SavePluginConfig(PluginConfig);
            PluginInterface.UiBuilder.Draw -= new System.Action(ui.Draw);
            LootTimer.Dispose();
            Client.CfPop -= CFPop;
            Client.TerritoryChanged -= TerritoryChanged;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}