using FoodCheck;
using ImGuiNET;

namespace FoodCheck
{
    public class PluginUI
    {

        private readonly Plugin plugin;

        public PluginUI(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public bool IsVisible;

        public void Draw()
        {
            if (!IsVisible || !ImGui.Begin("FoodCheck Config", ref IsVisible, (ImGuiWindowFlags)96))
                return;

            ImGui.Checkbox("Post in echo chat", ref this.plugin.config.PostToEcho);
            ImGui.Checkbox("Post in party chat", ref this.plugin.config.PostToParty);
            ImGui.InputText("Please format the message as you would like:", ref this.plugin.config.CustomizableMessage, 40);
            ImGui.End();
        }
    }
}
