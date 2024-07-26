using Veda;
using ImGuiNET;

namespace FoodCheck
{
    public class PluginUI
    {
        public bool IsVisible;
        private bool ShowSupport;

        public void Draw()
        {
            if (!IsVisible || !ImGui.Begin("FoodCheck Config", ref IsVisible, (ImGuiWindowFlags)96))
                return;

            ImGui.Checkbox("Post in echo chat", ref Plugin.PluginConfig.PostToEcho);
            ImGui.Checkbox("Post in party chat", ref Plugin.PluginConfig.PostToParty);
            ImGui.Checkbox("Only post in high-end duties", ref Plugin.PluginConfig.OnlyDoHighEndDuties);
            ImGui.Checkbox("Only use first names", ref Plugin.PluginConfig.OnlyUseFirstNames);
            ImGui.Text("Please format the message as you would like it to show:");
            ImGui.InputText("", ref Plugin.PluginConfig.CustomizableMessage, 100);
            ImGui.Text("<names> will be replaced with the names of the people\nwho need to eat food.");
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
