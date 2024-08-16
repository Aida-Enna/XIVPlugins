using ImGuiNET;
using System.Linq;
using Veda;

namespace AutoPillion
{
    public class PluginUI
    {
        public bool IsVisible;
        public void Draw()
        {
            if (!IsVisible || !ImGui.Begin("AutoPillion Config", ref IsVisible, ImGuiWindowFlags.AlwaysAutoResize))
                return;
            ImGui.Text("Seconds to wait before trying to\nauto-pillion after dismounting:");
            ImGui.SetNextItemWidth(190);
            ImGui.DragInt("###Cooldown", ref Plugin.PluginConfig.CooldownInSeconds, 1, 1, 60);
            if (ImGui.Button("Save"))
            {
                Plugin.PluginConfig.Save();
                this.IsVisible = false;
            }
            ImGui.SameLine();
            ImGui.Checkbox("Only mount friends", ref Plugin.PluginConfig.OnlyMountFriends);
            ImGui.End();
        }
    }
}
