using Dalamud.Utility;
using ImGuiNET;
using System;
using Veda;

namespace LootMaster
{
    public class PluginUI
    {
        public bool IsVisible;
        private bool ShowSupport;

        public void Draw()
        {
            if (!IsVisible || !ImGui.Begin("LootMaster Config", ref IsVisible, (ImGuiWindowFlags)96))
                return;
            var OriginalStyle = ImGui.GetStyle();
            //ImGui.PushStyleColor(ImGuiCol.Button, 0xFF000000 | 0x00FF0000);
            //ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xDD000000 | 0x000000FF);
            //ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xAA000000 | 0x000000FF);
            ImGui.TextUnformatted("Commands:");
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
            if (!Plugin.PluginConfig.AutoRoll) ImGui.Checkbox("Automatically roll on loot", ref Plugin.PluginConfig.AutoRoll);
            if (Plugin.PluginConfig.AutoRoll)
            {
                ImGui.Checkbox("Automatically roll the following on all loot:", ref Plugin.PluginConfig.AutoRoll);
                if (ImGui.BeginCombo("", Plugin.PluginConfig.AutoRollOption.ToString()))
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
                if (ImGui.IsItemHovered()) { ImGui.SetTooltip("For things like minions/green items you can't get more than one of/already have in your inventory."); }
                ImGui.Checkbox("Do not auto-roll in high-end duties", ref Plugin.PluginConfig.DoNotRollInHighEndDuties);
                if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Disable auto-rolling for any duty that is in the \"High-end Duty\" tab of duty finder."); }
            }
            //ImGui.Spacing();
            ImGui.Separator();
            ImGui.Checkbox("Display roll information in system chat", ref Plugin.PluginConfig.EnableChatLogMessage);
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Show how many items were needed, greeded, or passed."); }
            ImGui.Checkbox("Display a message if your inventory has less than 5 empty slots", ref Plugin.PluginConfig.InventoryCheck);
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Recommended so that you don't miss loot!"); }
            //ImGui.Spacing();
            ImGui.Separator();
            ImGui.Checkbox("Enable delay between rolls", ref Plugin.PluginConfig.EnableDelay);
            if (Plugin.PluginConfig.EnableDelay)
            {
                ImGui.Spacing();
                ImGui.SliderInt("Minimum delay (in milliseconds)", ref Plugin.PluginConfig.LowNum, 250, 750);
                ImGui.Spacing();
                ImGui.SliderInt("Maximum delay (in milliseconds)", ref Plugin.PluginConfig.HighNum, 500, 1000);
                if (Plugin.PluginConfig.LowNum > Plugin.PluginConfig.HighNum)
                {
                    Plugin.PluginConfig.LowNum = Plugin.PluginConfig.HighNum;
                }
            }
            ImGui.Spacing();
            if (ImGui.Button("Want to help support my work?"))
            {
                ShowSupport = !ShowSupport;
            }
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Click me!"); }
            if (ShowSupport)
            {
                ImGui.Text("Here are the current ways you can support the work I do.\nEvery bit helps, thank you! Have a great day!");
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
            ImGui.End();
        }
    }
}