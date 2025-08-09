using Dalamud.Bindings.ImGui;
using System.Linq;
using Veda;

namespace PortraitFixer
{
    public class PluginUI
    {
        public bool IsVisible;
        private bool ShowSupport;
        public void Draw()
        {
            if (!IsVisible || !ImGui.Begin("PortraitFixer Config", ref IsVisible, ImGuiWindowFlags.AlwaysAutoResize))
                return;

            //ImGui.Begin($"PortraitFixer Debugging");
            //if (ImGui.Button("Clear Queue"))
            //{
            //    Plugin.actionQueue.Clear();
            //}

            //if (ImGui.Button("Test Step: Open GearSetMenu")) Plugin.actionQueue.Enqueue(Plugin.OpenGearSetMenu);
            //if (ImGui.Button("Test Step: RightClickOnGearSet")) Plugin.actionQueue.Enqueue(Plugin.RightClickOnGearSet);
            //if (ImGui.Button("Test Step: OpenPortraitMenu")) Plugin.actionQueue.Enqueue(Plugin.OpenPortraitMenu);
            //if (ImGui.Button("Test Step: CheckForPortraitEditor"))
            //{
            //    Plugin.actionQueue.Enqueue(Plugin.VariableDelay(100));
            //    Plugin.actionQueue.Enqueue(Plugin.CheckForPortraitEditor);
            //}
            //if (ImGui.Button("Test Step: PressSaveOnPortraitMenu"))
            //{
            //    Plugin.actionQueue.Enqueue(Plugin.VariableDelay(100));
            //    Plugin.actionQueue.Enqueue(Plugin.PressSaveOnPortraitMenu);
            //}
            //if (ImGui.Button("Test Step: ClosePortraitMenu")) Plugin.actionQueue.Enqueue(Plugin.ClosePortraitMenu);
            
            //if (ImGui.Button("JOHN FUCKING MADDEN"))
            //{
            //    //V2
            //    //Plugin.actionQueue.Enqueue(Plugin.OpenPortraitMenuV2);
            //    //Plugin.actionQueue.Enqueue(Plugin.CheckForPortraitListV2);
            //    //Plugin.actionQueue.Enqueue(Plugin.SelectPortraitV2);
            //    //Plugin.actionQueue.Enqueue(Plugin.CheckForPortraitEditor);
            //    //Plugin.actionQueue.Enqueue(Plugin.PressSaveOnPortraitMenuAndClose);
            //    //Plugin.actionQueue.Enqueue(Plugin.OpenPortraitMenuV2);
            //    //V1
            //    //Plugin.actionQueue.Enqueue(Plugin.OpenGearSetMenu);
            //    //Plugin.actionQueue.Enqueue(Plugin.RightClickOnGearSet);
            //    //Plugin.actionQueue.Enqueue(Plugin.OpenPortraitMenu);
            //    //Plugin.actionQueue.Enqueue(Plugin.CheckForPortraitEditor);
            //    //Plugin.actionQueue.Enqueue(Plugin.VariableDelay(100));
            //    //Plugin.actionQueue.Enqueue(Plugin.PressSaveOnPortraitMenu);
            //    //Plugin.actionQueue.Enqueue(Plugin.VariableDelay(100));
            //    //Plugin.actionQueue.Enqueue(Plugin.ClosePortraitMenu);
            //    //Plugin.Chat.Print("[PortraitFixer] Portrait Saved!");
            //    Plugin.SavePortrait("DEBUG");
            //}
            ////if (ImGui.Button("Test Step: OpenPortraitMenuV2")) Plugin.actionQueue.Enqueue(Plugin.OpenPortraitMenuV2);
            ////if (ImGui.Button("Test Step: CheckForPortraitListV2")) Plugin.actionQueue.Enqueue(Plugin.CheckForPortraitListV2);
            ////if (ImGui.Button("Test Step: SelectPortraitV2")) Plugin.actionQueue.Enqueue(Plugin.SelectPortraitV2);
            ////if (ImGui.Button("Test Step: CheckForPortraitEditor")) Plugin.actionQueue.Enqueue(Plugin.CheckForPortraitEditor);
            ////if (ImGui.Button("Test Step: PressSaveOnPortraitMenuAndClose")) Plugin.actionQueue.Enqueue(Plugin.PressSaveOnPortraitMenuAndClose);
            ////if (ImGui.Button("Test Step: OpenPortraitMenuV2")) Plugin.actionQueue.Enqueue(Plugin.OpenPortraitMenuV2);
            ////if (ImGui.Button("Test Step: CloseGearSetMenu")) Plugin.actionQueue.Enqueue(Plugin.CloseGearSetMenu);

            //ImGui.Text("Current Queue:");
            //foreach (var l in Plugin.actionQueue.ToList())
            //{
            //    ImGui.Text($"{l.Method.Name}");
            //}

            //ImGui.End();

            ImGui.Checkbox("Automatically update portrait when gearset is updated", ref Plugin.PluginConfig.AutoUpdatePortraitFromGearsetUpdate);
            if (Plugin.PluginConfig.AutoUpdatePortraitFromGearsetUpdate) { ImGui.Checkbox("Display message when portrait is autosaved via gearset update", ref Plugin.PluginConfig.ShowMessageInChatWhenAutoUpdatingPortraitFromGearsetUpdate); }
            if (ImGui.Button("Save"))
            {
                Plugin.PluginConfig.Save();
                this.IsVisible = false;
            }
            ImGui.SameLine();
            ImGui.Indent(200);
            if (ImGui.Button("Want to help support my work?"))
            {
                ShowSupport = !ShowSupport;
            }
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Click me!"); }
            if (ShowSupport)
            {
                ImGui.Indent(-200);
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
