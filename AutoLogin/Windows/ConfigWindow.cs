using Dalamud.Bindings.ImGui;
using Dalamud.Configuration;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;
using System;
using System.Linq;
using Veda;

namespace AutoLogin.Windows
{
    public class ConfigWindow : Window, IDisposable
    {
        // We give this window a constant ID using ###.
        // This allows for labels to be dynamic, like "{FPS Counter}fps###XYZ counter window",
        // and the window ID will always be "###XYZ counter window" for ImGui
        public ConfigWindow(Plugin plugin) : base("AutoLogin Config###ALConfig")
        {
            Flags = ImGuiWindowFlags.AlwaysAutoResize;

            SizeCondition = ImGuiCond.Always;
        }

        public void Dispose()
        { }

        private bool ShowSupport;

        public override void Draw()
        {
            const ImGuiWindowFlags windowFlags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse;

            var dcSheet = Plugin.Data.Excel.GetSheet<WorldDCGroupType>();
            //if (dcSheet == null) return false;
            var worldSheet = Plugin.Data.Excel.GetSheet<World>();
            //if (worldSheet == null) return false;

            var currentDc = dcSheet.GetRow(Plugin.PluginConfig.DataCenter);
            if (currentDc.Region == 0)
            {
                Plugin.PluginConfig.DataCenter = 0;
                //return true;
            }

            if (ImGui.BeginCombo("Data Center", Plugin.PluginConfig.DataCenter == 0 ? "Not Selected" : currentDc.Name.ToString()))
            {
                foreach (var dc in dcSheet.Where(w => w.Region > 0 && w.Name.ToString().Trim().Length > 0))
                {
                    if (ImGui.Selectable(dc.Name.ToString(), dc.RowId == Plugin.PluginConfig.DataCenter))
                    {
                        Plugin.PluginConfig.DataCenter = dc.RowId;
                        Plugin.PluginConfig.Save();
                    }
                }
                ImGui.EndCombo();
            }

            if (currentDc.Region != 0)
            {
                var currentWorld = worldSheet.GetRow(Plugin.PluginConfig.World);
                if (/*currentWorld.RowId == 0 || */Plugin.PluginConfig.World != 0 && currentWorld.DataCenter.RowId != Plugin.PluginConfig.DataCenter)
                {
                    Plugin.PluginConfig.World = 0;
                }

                if (ImGui.BeginCombo("World", Plugin.PluginConfig.World == 0 ? "Not Selected" : currentWorld.Name.ToString()))
                {
                    foreach (var w in worldSheet.Where(w => w.DataCenter.RowId == Plugin.PluginConfig.DataCenter && w.IsPublic))
                    {
                        if (ImGui.Selectable(w.Name.ToString(), w.RowId == Plugin.PluginConfig.World))
                        {
                            Plugin.PluginConfig.World = w.RowId;
                            Plugin.PluginConfig.Save();
                        }
                    }
                    ImGui.EndCombo();
                }

                if (currentWorld.IsPublic)
                {
                    if (ImGui.BeginCombo("Character Slot", $"Slot #{Plugin.PluginConfig.CharacterSlot + 1}"))
                    {
                        for (uint i = 0; i < 8; i++)
                        {
                            if (ImGui.Selectable($"Slot #{i + 1}", Plugin.PluginConfig.CharacterSlot == i))
                            {
                                Plugin.PluginConfig.CharacterSlot = i;
                                Plugin.PluginConfig.Save();
                            }
                        }
                        ImGui.EndCombo();
                    }
                }
            }
            if (ImGui.Checkbox("Relogin to the above after being disconnected", ref Plugin.PluginConfig.RelogAfterDisconnect))
            {
                Plugin.PluginConfig.Save();
            }
            if (ImGui.Checkbox("Send discord notification when disconnected", ref Plugin.PluginConfig.SendNotif))
            {
                Plugin.PluginConfig.Save();
            }
            if (Plugin.PluginConfig.SendNotif)
            {
                ImGui.SetNextItemWidth(400);
                ImGui.InputText("Text to send", ref Plugin.PluginConfig.WebhookMessage, 400);
                ImGui.SetNextItemWidth(400);
                ImGui.InputText("Webhook URL", ref Plugin.PluginConfig.WebhookURL, 200);
                if (string.IsNullOrWhiteSpace(Plugin.PluginConfig.WebhookMessage))
                {
                    ImGui.TextColored(new System.Numerics.Vector4(1.0f, 0.0f, 0.0f, 1.0f), "Text to send invalid!");
                }
                if (!Plugin.PluginConfig.WebhookURL.StartsWith("https://discord.com/api/webhooks/"))
                {
                    ImGui.TextColored(new System.Numerics.Vector4(1.0f, 0.0f, 0.0f, 1.0f), "Webhook invalid. Correct format:\nhttps://discord.com/api/webhooks/1234556789/abcdefghijklmno");
                }
                if (ImGui.Button("Save webhook message/URL"))
                {
                    Plugin.PluginConfig.Save();
                }
                ImGui.SameLine();
                if (ImGui.Button("Send test payload"))
                {
                    Functions.SendDiscordWebhookAsync(Plugin.PluginConfig.WebhookURL, Plugin.PluginConfig.WebhookMessage);
                }
                ImGui.Text("Do not share this webhook URL with other people,\nit will allow them to send messages to your server.");
            }
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
            if (ImGui.Checkbox("Debug Mode", ref Plugin.PluginConfig.DebugMode))
            {
                Plugin.PluginConfig.Save();
            }
            if (Plugin.PluginConfig.DebugMode)
            {
                ImGui.Text("Last login error code: " + Plugin.PluginConfig.LastErrorCode);
                ImGui.Separator();
                if (ImGui.Button("-> Clear Queue"))
                {
                    Plugin.actionQueue.Clear();
                }

                if (ImGui.Button("-> Test Step: Open Data Centre Menu")) Plugin.actionQueue.Enqueue(Plugin.OpenDataCenterMenu);
                if (ImGui.Button($"-> Test Step: Select Data Center [{Plugin.PluginConfig.DataCenter}]")) Plugin.actionQueue.Enqueue(Plugin.SelectDataCentre);

                if (ImGui.Button($"-> Test Step: SELECT WORLD [{Plugin.PluginConfig.World}]"))
                {
                    Plugin.actionQueue.Clear();
                    Plugin.actionQueue.Enqueue(Plugin.SelectWorld);
                }

                if (ImGui.Button($"-> Test Step: SELECT CHARACTER [{Plugin.PluginConfig.CharacterSlot}]"))
                {
                    Plugin.actionQueue.Clear();
                    Plugin.actionQueue.Enqueue(Plugin.SelectCharacter);
                }

                if (ImGui.Button("-> Test Step: SELECT YES"))
                {
                    Plugin.actionQueue.Clear();
                    Plugin.actionQueue.Enqueue(Plugin.SelectYes);
                }

                if (ImGui.Button("-> Logout"))
                {
                    Plugin.actionQueue.Clear();
                    Plugin.actionQueue.Enqueue(Plugin.Logout);
                    Plugin.actionQueue.Enqueue(Plugin.SelectYes);
                    Plugin.actionQueue.Enqueue(Plugin.Delay5s);
                }

                if (ImGui.Button("-> Swap Character"))
                {
                    uint? tempDc = 9;
                    uint? tempWorld = 87;
                    uint? tempCharacter = 0;

                    Plugin.actionQueue.Enqueue(Plugin.Logout);
                    Plugin.actionQueue.Enqueue(Plugin.SelectYes);
                    Plugin.actionQueue.Enqueue(Plugin.OpenDataCenterMenu);
                    Plugin.actionQueue.Enqueue(Plugin.SelectDataCentre);
                    Plugin.actionQueue.Enqueue(Plugin.SelectWorld);
                    Plugin.actionQueue.Enqueue(Plugin.SelectCharacter);
                    Plugin.actionQueue.Enqueue(Plugin.SelectYes);
                    Plugin.actionQueue.Enqueue(Plugin.Delay5s);
                    Plugin.actionQueue.Enqueue(Plugin.ClearTemp);
                }

                if (ImGui.Button("-> Full Run"))
                {
                    Plugin.actionQueue.Clear();
                    Plugin.actionQueue.Enqueue(Plugin.OpenDataCenterMenu);
                    Plugin.actionQueue.Enqueue(Plugin.SelectDataCentre);
                    Plugin.actionQueue.Enqueue(Plugin.SelectWorld);
                    Plugin.actionQueue.Enqueue(Plugin.SelectCharacter);
                    Plugin.actionQueue.Enqueue(Plugin.SelectYes);
                }

                ImGui.Text("Current Queue:");
                foreach (var l in Plugin.actionQueue.ToList())
                {
                    ImGui.Text($"{l.Method.Name}");
                }
            }
        }
    }
}