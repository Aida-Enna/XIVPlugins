using Dalamud.Game.Gui;
using Dalamud.Utility;
using ImGuiNET;
using System;
using System.Linq;

namespace LootMaster
{
    public class PluginUI
    {
        public bool IsVisible;

        public void Draw()
        {
            if (!IsVisible || !ImGui.Begin("LootMaster Config", ref IsVisible, (ImGuiWindowFlags)96))
                return;
            ImGui.TextUnformatted("Features");
            if (ImGui.BeginTable("lootlootlootlootloot", 2))
            {
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("/need");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Roll need for everything. If impossible, roll greed. Else, roll pass.");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("/needonly");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Roll need for everything. If impossible, roll pass.");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("/greed");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Greed on all items. Else, roll pass.");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("/pass");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Pass on things you haven't rolled for yet.");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("/passall");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Passes on all, even if you rolled on them previously.");
                ImGui.EndTable();
            }
            ImGui.Checkbox("Automatically roll on loot", ref Plugin.PluginConfig.AutoRoll);
            if (Plugin.PluginConfig.AutoRoll)
            {
                if (ImGui.BeginCombo("on all loot", Plugin.PluginConfig.AutoRollOption.ToString()))
                {
                    foreach (AutoRollOption RollSelection in Enum.GetValues(typeof(AutoRollOption)))
                    {
                        ImGui.PushID((byte)RollSelection);
                        if (ImGui.Selectable(RollSelection.GetAttribute<Display>().Value))
                        {
                            //Plugin.Chat.Print("AutoRollOption is " + RollSelection);
                            Plugin.PluginConfig.AutoRollOption = RollSelection;
                            Plugin.PluginConfig.Save();
                            //Plugin.Chat.Print("AutoRollOption is " + Plugin.PluginConfig.AutoRollOption);
                        }

                        if (RollSelection == Plugin.PluginConfig.AutoRollOption)
                        {
                            ImGui.SetItemDefaultFocus();
                        }

                        ImGui.PopID();
                    }
                    ImGui.EndCombo();
                }
                ImGui.Checkbox("Display auto-loot status on Duty Finder pop", ref Plugin.PluginConfig.NotifyOnCFPop);
                if (ImGui.IsItemHovered()) { ImGui.SetTooltip("HIGHLY RECOMMENDED so that you don't forget you have it set to something and lose loot you care about!"); }
                ImGui.Checkbox("Automatically pass on items that fail need/greed", ref Plugin.PluginConfig.PassOnFail);
                if (ImGui.IsItemHovered()) { ImGui.SetTooltip("For things like minions/green items you can't get more than one of/already have in your inventory"); }
            }
            /*
             * if (ImGui.BeginCombo("Race", othersTargetRace.GetAttribute<Display>().Value))
                    {
                        foreach (Race race in Enum.GetValues(typeof(Race)))
                        {
                            ImGui.PushID((byte) race);
                            if (ImGui.Selectable(race.GetAttribute<Display>().Value, race == othersTargetRace))
                            {
                                othersTargetRace = race;
                            }

                            if (race == othersTargetRace)
                            {
                                ImGui.SetItemDefaultFocus();
                            }

                            ImGui.PopID();
                        }
                        if (ImGui.IsItemHovered()) { ImGui.SetTooltip("The race to change all players to"); }
                        ImGui.EndCombo();
                    }
            */
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Checkbox("Display roll information in system chat", ref Plugin.PluginConfig.EnableChatLogMessage);
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Checkbox("Enable Delay", ref Plugin.PluginConfig.EnableDelay);
            ImGui.Spacing();
            ImGui.Text("Sets the delay between rolls (in milliseconds)");
            ImGui.Spacing();
            ImGui.SliderInt("Min Delay", ref Plugin.PluginConfig.LowNum, 250, 750);
            ImGui.Spacing();
            ImGui.SliderInt("Max Delay", ref Plugin.PluginConfig.HighNum, 500, 1000);
            if (Plugin.PluginConfig.LowNum > Plugin.PluginConfig.HighNum)
            {
                Plugin.PluginConfig.LowNum = Plugin.PluginConfig.HighNum; 
            }
            ImGui.End();
        }
    }
}
