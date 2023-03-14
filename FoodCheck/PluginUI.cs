using FoodCheck;
using ImGuiNET;

namespace FoodCheck
{
    public class PluginUI
    {
        public bool IsVisible;

        public void Draw()
        {
            if (!IsVisible || !ImGui.Begin("FoodCheck Config", ref IsVisible, (ImGuiWindowFlags)96))
                return;

            ImGui.Checkbox("Post in echo chat", ref Plugin.PluginConfig.PostToEcho);
            ImGui.Checkbox("Post in party chat", ref Plugin.PluginConfig.PostToParty);
            ImGui.Checkbox("Only post in high-end duties", ref Plugin.PluginConfig.OnlyDoHighEndDuties);
            ImGui.Text("Please format the message as you would like it to show:");
            ImGui.InputText("", ref Plugin.PluginConfig.CustomizableMessage, 40);
            ImGui.Text("<names> will be replaced with the names of the people\nwho need to eat food.");
            ImGui.End();
        }
    }
}
