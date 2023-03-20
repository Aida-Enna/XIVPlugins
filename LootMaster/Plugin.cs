using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;
using Veda;
using static Dalamud.Game.Text.SeStringHandling.Payloads.ItemPayload;
using static FFXIVClientStructs.FFXIV.Component.GUI.AtkComponentList;

namespace LootMaster
{
    public class Plugin : IDalamudPlugin, IDisposable
    {
        public string Name => "LootMaster";

        [PluginService] public static CommandManager Commands { get; set; }
        [PluginService] public static DalamudPluginInterface PluginInterface { get; set; }
        [PluginService] public static SigScanner SigScanner { get; set; }
        [PluginService] public static ChatGui Chat { get; set; }
        [PluginService] public static ClientState Client { get; set; }
        public static Configuration PluginConfig { get; set; }
        private PluginCommandManager<Plugin> commandManager;
        private static IntPtr lootsAddr;
        internal static RollItemRaw rollItemRaw;
        private readonly PluginUI ui;

        internal delegate void RollItemRaw(IntPtr lootIntPtr, RollOption option, uint lootItemIndex);

        public static List<LootItem> LootItems => ReadArray<LootItem>(lootsAddr + 16, 16).Where(i => i.ItemId != 3758096384U && i.ItemId > 0U).ToList();
        public Timer LootTimer = new System.Timers.Timer();
        public static List<LootItem> FailedItems = new List<LootItem>();

        public Plugin(CommandManager commands, ClientState client)
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

            LootTimer.Interval = 10000; //600000
            LootTimer.Elapsed += CheckLoot;
            LootTimer.Start();
        }

        private unsafe void CFPop(object? sender, ContentFinderCondition queuedDuty)
        {
            if (PluginConfig.NotifyOnCFPop && PluginConfig.AutoRoll) { Chat.Print(Functions.BuildSeString("LootMaster", "Your current <c17>auto-roll setting is <c573>" + PluginConfig.AutoRollOption + "</c>.")); }
        }

        private async void CheckLoot(object source, ElapsedEventArgs e)
        {
            try
            {
                if (LootItems.Any(x => (LootMaster.RollState)x.RollState == RollState.LootMasterNotDecided)) { return; }
                if (LootItems.Count() > 0 && PluginConfig.AutoRoll)
                {
                    //save the number and don't repeat if the lootcount and failed count are the same
                    List<LootItem> ItemsToRollOn = LootItems;
                    ItemsToRollOn.RemoveAll(y => FailedItems.Any(z => y.RollResult == 0 && z.ItemId == y.ItemId));
                    if (ItemsToRollOn.Count == 0)
                    {
                        return; // PluginLog.Information("No non-failed items left, aborting...");
                    }
                    int OriginaLootcount = ItemsToRollOn.Count();
                    if (ItemsToRollOn.All(x => x.RollState == FFXIVClientStructs.FFXIV.Client.Game.UI.RollState.Rolled)) { return; }
                    LootTimer.Enabled = false;
                    Chat.Print("There's some loot waiting, " + ItemsToRollOn.Count() + " pieces. We're gonna roll " + PluginConfig.AutoRollOption);
                    Random random = new Random();
                    int randomDelay = random.Next(PluginConfig.LowNum, (PluginConfig.HighNum + 1));

                    int num1 = 0;
                    for (int index = 0; index < ItemsToRollOn.Count; ++index)
                    {
                        if (ItemsToRollOn[index].RollResult == 0)
                        {
                            if ((LootMaster.RollState)ItemsToRollOn[index].RollState == RollState.UpToNeed && (PluginConfig.AutoRollOption == AutoRollOption.Need || PluginConfig.AutoRollOption == AutoRollOption.NeedThenGreed))
                            {
                                RollItem(RollOption.Need, index);
                                num1++;
                                if (PluginConfig.EnableDelay == true)
                                {
                                    await Task.Delay(randomDelay);
                                }
                            }
                            if (PluginConfig.AutoRollOption == AutoRollOption.Greed || PluginConfig.AutoRollOption == AutoRollOption.NeedThenGreed)
                            {
                                RollItem(RollOption.Greed, index);
                                num1++;
                                if (PluginConfig.EnableDelay == true)
                                {
                                    await Task.Delay(randomDelay);
                                }
                            }
                            if (PluginConfig.AutoRollOption == AutoRollOption.Pass || (LootItems[index].RollResult == 0 && PluginConfig.PassOnFail))
                            {
                                RollItem(RollOption.Pass, index);
                                num1++;
                                if (PluginConfig.EnableDelay == true)
                                {
                                    await Task.Delay(randomDelay);
                                }
                            }
                        }
                    }
                    LootTimer.Enabled = true;
                    if (!PluginConfig.EnableChatLogMessage)
                        return;

                    if (LootItems.Where(x => x.RollResult == 0).Count() > 0)
                    {
                        if (num1 != 0)
                        {
                            Chat.Print(Functions.BuildSeString("LootMaster", "Auto " + PluginConfig.AutoRollOption.GetAttribute<Display>().Value + "ed <c575>" + num1.ToString() + " items(s), but couldn't roll some items."));
                        }
                        else
                        {
                            Chat.Print(Functions.BuildSeString("LootMaster", "Couldn't auto " + PluginConfig.AutoRollOption.GetAttribute<Display>().Value + "on any items."));
                        }
                        FailedItems = LootItems;
                    }
                    else
                    {
                        Chat.Print(Functions.BuildSeString("LootMaster", "Auto " + PluginConfig.AutoRollOption.GetAttribute<Display>().Value + "ed <c575>" + num1.ToString() + " items(s)."));
                    }
                }
            }
            catch (Exception f)
            {
                Chat.PrintError("Error with checking for loot: " + Environment.NewLine + f.ToString());
            }
        }

        private void RollItem(RollOption option, int index)
        {
            try
            {
                LootItem lootItem = LootItems[index];
                //PluginLog.Information(string.Format("{0} [{1}] {2} Id: {3:X} rollState: {4} rollOption: {5}", option, index, lootItem.ItemId, lootItem.ObjectId, lootItem.RollState, lootItem.RolledState), Array.Empty<object>());
                PluginLog.Information(string.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} ", lootItem.ChestItemIndex, lootItem.ChestObjectId, lootItem.ItemCount, lootItem.ItemId, lootItem.LootMode, lootItem.MaxTime, lootItem.RollResult, lootItem.RollState, lootItem.RollValue, lootItem.Time), Array.Empty<object>());
                rollItemRaw(lootsAddr, option, (uint)index);
            }
            catch (Exception f)
            {
                Chat.PrintError("Error with rolling: " + Environment.NewLine + f.ToString());
            }
        }

        [Command("/lootdebug")]
        [Aliases("/ld")]
        [HelpMessage("Display debug loot info")]
        public async void ShowDebugLootInfo(string command, string args)
        {
            string FinalMessage = "";

            foreach (LootItem Item in LootItems)
            {
                FinalMessage += "Item ID: " + Item.ItemId + Environment.NewLine;
                FinalMessage += "Loot index: " + Item.ChestItemIndex + Environment.NewLine;
                FinalMessage += "Number of item: " + Item.ItemCount + Environment.NewLine;
                FinalMessage += "Loot Mode: " + Item.LootMode + Environment.NewLine;
                FinalMessage += "Roll Result: " + Item.RollResult + Environment.NewLine;
                FinalMessage += "Roll State: " + Item.RollState + Environment.NewLine;
                FinalMessage += "Roll Value: " + Item.RollValue + Environment.NewLine;

                //(string.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} ",
                //lootItem.ChestItemIndex,
                //lootItem.ChestObjectId,
                //lootItem.ItemCount,
                //lootItem.ItemId,
                //lootItem.LootMode,
                //lootItem.MaxTime,
                //lootItem.RollResult,
                //lootItem.RollState,
                //lootItem.RollValue,
                //lootItem.Time), Array.Empty<object>());
            }
            Chat.Print(FinalMessage);
        }
        [Command("/lootmaster")]
        [Aliases("/lm")]
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

            //List<Payload> payloadList = new()
            //{
            //    new TextPayload("Needed "),
            //    new UIForegroundPayload(575),
            //    new TextPayload(num1.ToString()),
            //    new UIForegroundPayload(0),
            //    new TextPayload(" item(s)" + ", greeded "),
            //    new UIForegroundPayload(575),
            //    new TextPayload(num2.ToString()),
            //    new UIForegroundPayload(0),
            //    new TextPayload(" item(s)" + ", passed "),
            //    new UIForegroundPayload(575),
            //    new TextPayload(num3.ToString()),
            //    new UIForegroundPayload(0),
            //    new TextPayload(" item(s)" + ".")
            //};
            //SeString seString = new(payloadList);
            //Chat.Print(seString);
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

            //List<Payload> payloadList = new()
            //{
            //    new TextPayload("Needed only "),
            //    new UIForegroundPayload(575),
            //    new TextPayload(num1.ToString()),
            //    new UIForegroundPayload(0),
            //    new TextPayload(" item(s)" + ", passed "),
            //    new UIForegroundPayload(575),
            //    new TextPayload(num2.ToString()),
            //    new UIForegroundPayload(0),
            //    new TextPayload(" item(s)" + ".")
            //};
            //SeString seString = new(payloadList);
            //Chat.Print(seString);
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

            //List<Payload> payloadList = null;
            //payloadList = new()
            //{
            //    new TextPayload("Greeded "),
            //    new UIForegroundPayload(575),
            //    new TextPayload(num.ToString()),
            //    new UIForegroundPayload(0),
            //    new TextPayload(" item(s)."),
            //};
            //SeString seString = new(payloadList);
            Chat.Print(Functions.BuildSeString(this.Name,"Greeded <c575>" + num.ToString() + " item(s)."));
            //Chat.Print(seString);
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

            //List<Payload> payloadList = null;
            if (num == 0)
            {
                Chat.Print(Functions.BuildSeString(this.Name, "Passed on <c575>0 item(s)."));
                //payloadList = new()
                //{
                //    new TextPayload("Passed on 0 items."),
                //};
            }
            else
            {
                //payloadList = new()
                //{
                //    new TextPayload("Passed "),
                //    new UIForegroundPayload(575),
                //    new TextPayload(num.ToString()),
                //    new UIForegroundPayload(0),
                //    new TextPayload(" item(s)" + ".")
                //};
                Chat.Print(Functions.BuildSeString(this.Name, "Passed <c575>" + num.ToString() + " item(s)."));
            }
            //SeString seString = new(payloadList);
            //Chat.Print(seString);
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
                //payloadList = new()
                //{
                //    new TextPayload("Passed on 0 items."),
                //};
                Chat.Print(Functions.BuildSeString(this.Name, "Passed on <c575>0 item(s)."));
            }
            else
            {
                //payloadList = new()
                //{
                //    new TextPayload("Passed all "),
                //    new UIForegroundPayload(575),
                //    new TextPayload(num.ToString()),
                //    new UIForegroundPayload(0),
                //    new TextPayload(" item(s)" + ".")
                //};
                Chat.Print(Functions.BuildSeString(this.Name, "Passed on <c575>" + num.ToString() + " item(s)."));
            }
            //SeString seString = new(payloadList);
            //Chat.Print(seString);
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
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}