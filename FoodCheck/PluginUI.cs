using FoodCheck;
using ImGuiNET;

namespace DalamudPluginProjectTemplate
{
    public class PluginUI
    {
        public bool IsVisible;

        public void Draw()
        {
            if (!IsVisible || !ImGui.Begin("FoodCheck Config", ref IsVisible, (ImGuiWindowFlags)96))
                return;

            ImGui.Checkbox("Post in echo chat", ref Plugin.config.PostToEcho);
            ImGui.Checkbox("Post in party chat", ref Plugin.config.PostToParty);
            ImGui.End();
        }
    }
}
