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
            ImGui.InputText("Please format the message as you would like:", ref Plugin.PluginConfig.CustomizableMessage, 40);
            ImGui.End();
        }
    }
}
