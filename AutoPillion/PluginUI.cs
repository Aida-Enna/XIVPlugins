using ImGuiNET;

namespace AutoPillion
{
    public class PluginUI
    {
        public bool IsVisible;
        public void Draw()
        {
            if (!IsVisible || !ImGui.Begin("AutoPillion Config", ref IsVisible, ImGuiWindowFlags.AlwaysAutoResize))
                return;
            ImGui.Text("Seconds to wait before trying\nto auto-pillion after getting off:");
            ImGui.SetNextItemWidth(190);
            ImGui.DragInt("###Cooldown", ref Plugin.PluginConfig.CooldownInSeconds, 1, 1, 60);
            if (ImGui.Button("Save"))
            {
                Plugin.PluginConfig.Save();
                this.IsVisible = false;
            }
            ImGui.End();
        }
    }
}
