using ImGuiNET;

namespace DalamudPluginProjectTemplate
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
                ImGui.TextUnformatted("Roll need for everything. If impossible, roll greed. Else, roll pass");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("/needonly");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Roll need for everything. If impossible, roll pass.");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("/greed");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Greed on all items. Else, roll pass");
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
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Checkbox("Display roll information in chat.", ref Plugin.config.EnableChatLogMessage);
            ImGui.End();
        }
    }
}
