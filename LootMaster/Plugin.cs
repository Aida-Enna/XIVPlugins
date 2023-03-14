using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Veda;

namespace LootMaster
{
    public class Plugin : IDalamudPlugin, IDisposable
    {
        public string Name => "LootMaster";

        [PluginService] public static CommandManager Commands { get; set; }
        [PluginService] public static DalamudPluginInterface PluginInterface { get; set; }
        [PluginService] public static SigScanner SigScanner { get; set; }
        [PluginService] public static ChatGui Chat { get; set; }
        public static Configuration PluginConfig { get; set; }
        private PluginCommandManager<Plugin> commandManager;
        private static IntPtr lootsAddr;
        internal static RollItemRaw rollItemRaw;
        private readonly PluginUI ui;
        internal delegate void RollItemRaw(IntPtr lootIntPtr, RollOption option, uint lootItemIndex);

        public static List<LootItem> LootItems => ReadArray<LootItem>(lootsAddr + 16, 16).Where(i => i.Valid).ToList();

        public Plugin(CommandManager commands)
        {
            lootsAddr = SigScanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 89 44 24 60", 0);
            rollItemRaw = Marshal.GetDelegateForFunctionPointer<RollItemRaw>(SigScanner.ScanText("41 83 F8 ?? 0F 83 ?? ?? ?? ?? 48 89 5C 24 08"));
            PluginConfig = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            PluginConfig.Initialize(PluginInterface);
            ui = new PluginUI();
            PluginInterface.UiBuilder.Draw += new Action(ui.Draw);
            PluginInterface.UiBuilder.OpenConfigUi += () =>
           {
               PluginUI ui = this.ui;
               ui.IsVisible = !ui.IsVisible;
           };
            commandManager = new PluginCommandManager<Plugin>(this, commands);

        }

        private void RollItem(RollOption option, int index)
        {
            LootItem lootItem = LootItems[index];
            PluginLog.Information(string.Format("{0} [{1}] {2} Id: {3:X} rollState: {4} rollOption: {5}", option, index, lootItem.ItemId, lootItem.ObjectId, lootItem.RollState, lootItem.RolledState), Array.Empty<object>());
            rollItemRaw(lootsAddr, option, (uint)index);
        }

        [Command("/need")]
        [HelpMessage("Roll need for everything. If impossible, roll greed. Else, roll pass")]
        public async void NeedCommand(string command, string args)
        {
            Random random = new Random();
            int randomDelay = random.Next(PluginConfig.LowNum, (PluginConfig.HighNum + 1));

            int num1 = 0;
            int num2 = 0;
            int num3 = 0;
            for (int index = 0; index < LootItems.Count; ++index)
            {
                if (!LootItems[index].Rolled)
                {
                    if (LootItems[index].RollState == RollState.UpToNeed)
                    {
                        RollItem(RollOption.Need, index);
                        ++num1;
                        if (PluginConfig.EnableDelay == true)
                        {
                            await Task.Delay(randomDelay);
                        }
                    }
                    else if (!LootItems[index].Rolled)
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
            
            List<Payload> payloadList = new()
            {
                new TextPayload("Needed "),
                new UIForegroundPayload(575),
                new TextPayload(num1.ToString()),
                new UIForegroundPayload(0),
                new TextPayload(" item(s)" + ", greeded "),
                new UIForegroundPayload(575),
                new TextPayload(num2.ToString()),
                new UIForegroundPayload(0),
                new TextPayload(" item(s)" + ", passed "),
                new UIForegroundPayload(575),
                new TextPayload(num3.ToString()),
                new UIForegroundPayload(0),
                new TextPayload(" item(s)" + ".")
            };
            SeString seString = new(payloadList);
            Chat.Print(seString);
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
                if (!LootItems[index].Rolled)
                {
                    if (LootItems[index].RollState == RollState.UpToNeed)
                    {
                        RollItem(RollOption.Need, index);
                        ++num1;
                        if(PluginConfig.EnableDelay == true)
                        {
                            await Task.Delay(randomDelay);
                        }
                    }
                    else
                    {
                        RollItem(RollOption.Pass, index);
                        ++num2;
                        if(PluginConfig.EnableDelay == true)
                        {
                            await Task.Delay(randomDelay);
                        }
                    }
                }
            }
            if (!PluginConfig.EnableChatLogMessage)
                return;
            
            List<Payload> payloadList = new()
            {
                new TextPayload("Needed only "),
                new UIForegroundPayload(575),
                new TextPayload(num1.ToString()),
                new UIForegroundPayload(0),
                new TextPayload(" item(s)" + ", passed "),
                new UIForegroundPayload(575),
                new TextPayload(num2.ToString()),
                new UIForegroundPayload(0),
                new TextPayload(" item(s)" + ".")
            };
            SeString seString = new(payloadList);
            Chat.Print(seString);
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
                if (!LootItems[index].Rolled)
                {
                    RollItem(RollOption.Greed, index);
                    ++num;
                    if(PluginConfig.EnableDelay == true)
                    {
                        await Task.Delay(randomDelay);
                    }
                }
                //else if (LootItems[index].RolledState != RollOption.Greed)
                //{
                //    RollItem(RollOption.Pass, index);
                //    ++num1;
                //}
            }
            if (!PluginConfig.EnableChatLogMessage)
                return;
            
            List<Payload> payloadList = null;
            payloadList = new()
            {
                new TextPayload("Greeded "),
                new UIForegroundPayload(575),
                new TextPayload(num.ToString()),
                new UIForegroundPayload(0),
                new TextPayload(" item(s)."),
            };
            SeString seString = new(payloadList);
            Chat.Print(seString);
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
                if (!LootItems[index].Rolled)
                {
                    RollItem(RollOption.Pass, index);
                    ++num;
                    if(PluginConfig.EnableDelay == true)
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
                payloadList = new()
                {
                    new TextPayload("Passed on 0 items."),
                };
            }
            else
            {
                payloadList = new()
                {
                    new TextPayload("Passed "),
                    new UIForegroundPayload(575),
                    new TextPayload(num.ToString()),
                    new UIForegroundPayload(0),
                    new TextPayload(" item(s)" + ".")
                };
            }
            SeString seString = new(payloadList);
            Chat.Print(seString);
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
                if (LootItems[index].RolledState != RollOption.Pass)
                {
                    RollItem(RollOption.Pass, index);
                    ++num;
                    if(PluginConfig.EnableDelay == true)
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
                payloadList = new()
                {
                    new TextPayload("Passed on 0 items."),
                };
            }
            else
            {
                payloadList = new()
                {
                    new TextPayload("Passed all "),
                    new UIForegroundPayload(575),
                    new TextPayload(num.ToString()),
                    new UIForegroundPayload(0),
                    new TextPayload(" item(s)" + ".")
                };
            }
            SeString seString = new(payloadList);
            Chat.Print(seString);
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
            PluginInterface.UiBuilder.Draw -= new Action(ui.Draw);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
