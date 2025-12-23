//using Dalamud.Bindings.ImGui;
//using Dalamud.Interface.Windowing;
//using System;
//using System.Collections.Generic;
//using System.Text;
//using static FFXIVClientStructs.FFXIV.Client.Game.WKS.WKSContentInventoryContainer.Delegates;

//namespace AutoLogin.Windows
//{
//    internal class DebugMenu : Window, IDisposable
//    {

//        public DebugMenu(Plugin plugin) : base("DebugMenu###Debug")
//        {
//            Flags = ImGuiWindowFlags.AlwaysAutoResize;
//            SizeCondition = ImGuiCond.Always;
//        }

//        public void Dispose()
//        { }

//        public override void PreDraw()
//        {
//        }

//        public override void Draw()
//        {


//            //drawConfigWindow = drawConfigWindow && PluginConfig.DrawConfigUI();

//            //if (!drawDebugWindow) return;
//            if (ImGui.Begin("AutoLogin Debugging"))
//            {
//                if (ImGui.Button("Clear Queue"))
//                {
//                    Plugin.actionQueue.Clear();
//                }

//                if (ImGui.Button("Test Step: Open Data Centre Menu")) actionQueue.Enqueue(OpenDataCenterMenu);
//                if (ImGui.Button($"Test Step: Select Data Center [{PluginConfig.DataCenter}]")) actionQueue.Enqueue(SelectDataCentre);

//                if (ImGui.Button($"Test Step: SELECT WORLD [{PluginConfig.World}]"))
//                {
//                    Plugin.actionQueue.Clear();
//                    Plugin.actionQueue.Enqueue(SelectWorld);
//                }

//                if (ImGui.Button($"Test Step: SELECT CHARACTER [{PluginConfig.CharacterSlot}]"))
//                {
//                    Plugin.actionQueue.Clear();
//                    Plugin.actionQueue.Enqueue(Plugin.SelectCharacter);
//                }

//                if (ImGui.Button("Test Step: SELECT YES"))
//                {
//                    actionQueue.Clear();
//                    actionQueue.Enqueue(SelectYes);
//                }

//                if (ImGui.Button("Logout"))
//                {
//                    actionQueue.Clear();
//                    actionQueue.Enqueue(Logout);
//                    actionQueue.Enqueue(SelectYes);
//                    actionQueue.Enqueue(Delay5s);
//                }

//                if (ImGui.Button("Swap Character"))
//                {
//                    tempDc = 9;
//                    tempWorld = 87;
//                    tempCharacter = 0;

//                    actionQueue.Enqueue(Logout);
//                    actionQueue.Enqueue(SelectYes);
//                    actionQueue.Enqueue(OpenDataCenterMenu);
//                    actionQueue.Enqueue(SelectDataCentre);
//                    actionQueue.Enqueue(SelectWorld);
//                    actionQueue.Enqueue(SelectCharacter);
//                    actionQueue.Enqueue(SelectYes);
//                    actionQueue.Enqueue(Delay5s);
//                    actionQueue.Enqueue(ClearTemp);
//                }

//                if (ImGui.Button("Full Run"))
//                {
//                    actionQueue.Clear();
//                    actionQueue.Enqueue(OpenDataCenterMenu);
//                    actionQueue.Enqueue(SelectDataCentre);
//                    actionQueue.Enqueue(SelectWorld);
//                    actionQueue.Enqueue(SelectCharacter);
//                    actionQueue.Enqueue(SelectYes);
//                }

//                ImGui.Text("Current Queue:");
//                foreach (var l in actionQueue.ToList())
//                {
//                    ImGui.Text($"{l.Method.Name}");
//                }
//            }
//            ImGui.End();
//        }
//    }
//}
