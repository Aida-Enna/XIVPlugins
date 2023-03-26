using ImGuiNET;
using Veda;

namespace RightClickExtender
{
    public class PluginUI
    {
        public bool IsVisible;
        public void Draw()
        {
            if (!IsVisible || !ImGui.Begin("RightClickExtender Config", ref IsVisible, ImGuiWindowFlags.AlwaysAutoResize))
                return;
            //ImGui.Text("Enter your twitch username here:");
            //ImGui.SetNextItemWidth(310);
            //ImGui.InputText("Username", ref Plugin.PluginConfig.Username, 25);
            //ImGui.Text("Enter the initial channel name to join here:");
            //ImGui.SetNextItemWidth(310);
            //ImGui.InputText("Channel", ref Plugin.PluginConfig.ChannelToSend, 25);
            //ImGui.Text("The last channel you join will be remembered and\nautomatically joined at plugin start.");
            //ImGui.Text("Enter your oath code here (including the \"oath:\" part):");
            //ImGui.SetNextItemWidth(310);
            //ImGui.InputText("OAuth", ref Plugin.PluginConfig.OAuthCode, 36);
            if (ImGui.Button("Save"))
            {
                Plugin.PluginConfig.Save();
                this.IsVisible = false;
                //Plugin.Chat.Print(Functions.BuildSeString("Twitch XIV","<c17>DO <c25>NOT <c37>SHARE <c45>YOUR <c48>OAUTH <c52>CODE <c500>WITH <c579>ANYONE!"));
            }
            //ImGui.SameLine();
            //if (ImGui.Checkbox("Relay twitch chat to chatbox", ref Plugin.PluginConfig.TwitchEnabled))
            //{
            //    Plugin.Chat.Print(Functions.BuildSeString("TwitchXIV",$"Toggled twitch chat {(Plugin.PluginConfig.TwitchEnabled ? "on" : "off")}."));
            //}
            //ImGui.SameLine();
            //ImGui.Indent(280);
            //if (ImGui.Button("Get OAuth code"))
            //{
            //    Functions.OpenWebsite("https://twitchapps.com/tmi/");
            //}
            ImGui.End();
        }
    }
}
