using Dalamud.Configuration;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Utility;
using ImGuiNET;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;
using Veda;

namespace PotatoFamine2
{
    public class PluginUI
    {
        private static Vector4 WHAT_THE_HELL_ARE_YOU_DOING = new Vector4(1, 0, 0, 1);
        private static Vector4 GreenMaybe = new Vector4(0, 1, 0, 1);
        private readonly Plugin plugin;
        //private bool enableExperimental;
        private bool ShowWhy;
        private bool ShowSupport;

        private ITargetManager _targetManager;

        public PluginUI(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void Draw()
        {
            if (!Plugin.SettingsVisible)
            {
                return;
            }

            bool settingsVisible = Plugin.SettingsVisible;
            if (ImGui.Begin("Potato Famine 2", ref settingsVisible, ImGuiWindowFlags.AlwaysAutoResize))
            {

                bool shouldChangeOthers = Plugin.PluginConfig.ShouldChangeOthers;
                ImGui.Checkbox("Change other players", ref shouldChangeOthers);
                if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Change all other players to the specified race (unless overriden by another option)"); }
                Race othersTargetRace = Plugin.PluginConfig.ChangeOthersTargetRace;

                if (shouldChangeOthers)
                {
                    bool onlyChangeLalafells = Plugin.PluginConfig.OnlyChangeLalafells;
                    ImGui.SameLine();
                    ImGui.Checkbox("Only change lalafells", ref onlyChangeLalafells);
                    if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Only change lalafell players (unless overriden by another option)"); }

                    this.plugin.OnlyChangeLalafells(onlyChangeLalafells);
                }

                if (shouldChangeOthers)
                {
                    if (ImGui.BeginCombo("Race", othersTargetRace.GetAttribute<Display>().Value))
                    {
                        foreach (Race race in Enum.GetValues(typeof(Race)))
                        {
                            ImGui.PushID((byte) race);
                            if (ImGui.Selectable(race.GetAttribute<Display>().Value, race == othersTargetRace))
                            {
                                othersTargetRace = race;
                            }

                            if (race == othersTargetRace)
                            {
                                ImGui.SetItemDefaultFocus();
                            }

                            ImGui.PopID();
                        }
                        if (ImGui.IsItemHovered()) { ImGui.SetTooltip("The race to change all players to"); }
                        ImGui.EndCombo();
                    }
                }

                this.plugin.UpdateOtherRace(othersTargetRace);

                this.plugin.ToggleOtherRace(shouldChangeOthers);

                //------------------------------------------------

                bool ForciblyChangePeople = Plugin.PluginConfig.ForciblyChangePeople;
                ImGui.Checkbox("Change players in Forcibly Change list", ref ForciblyChangePeople);
                if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Change players in the Forcibly Change list regardless of other options"); }
                this.plugin.ToggleForciblyChangeOption(ForciblyChangePeople);

                Race ForciblyChangePeopleTargetRace = Plugin.PluginConfig.ForciblyChangePeopleTargetRace;

                if (ForciblyChangePeople)
                {
                    if (ImGui.BeginCombo("Race ", ForciblyChangePeopleTargetRace.GetAttribute<Display>().Value))
                    {
                        foreach (Race race in Enum.GetValues(typeof(Race)))
                        {
                            ImGui.PushID((byte)race);
                            if (ImGui.Selectable(race.GetAttribute<Display>().Value, race == ForciblyChangePeopleTargetRace))
                            {
                                ForciblyChangePeopleTargetRace = race;
                            }

                            if (race == ForciblyChangePeopleTargetRace)
                            {
                                ImGui.SetItemDefaultFocus();
                            }
                            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("The race to forcibly change players in the list to"); }
                            ImGui.PopID();
                        }

                        ImGui.EndCombo();
                    }
                }

                this.plugin.UpdateForciblyChangeRace(ForciblyChangePeopleTargetRace);

                bool UseTrustedList = Plugin.PluginConfig.UseTrustedList;
                ImGui.Checkbox("Don't change trusted players", ref UseTrustedList);
                if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Do not change players in the Trusted list"); }
                this.plugin.ToggleTrustedOption(UseTrustedList);

                bool shouldChangeSelf = Plugin.PluginConfig.ChangeSelf;
                ImGui.Checkbox("Change self", ref shouldChangeSelf);
                if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Change your own race to the specified race"); }
                this.plugin.ToggleChangeSelf(shouldChangeSelf);

                Race selfTargetRace = Plugin.PluginConfig.ChangeSelfTargetRace;

                if (shouldChangeSelf)
                {
                    if (ImGui.BeginCombo("Race Self", selfTargetRace.GetAttribute<Display>().Value))
                    {
                        foreach (Race race in Enum.GetValues(typeof(Race)))
                        {
                            ImGui.PushID((byte)race);
                            if (ImGui.Selectable(race.GetAttribute<Display>().Value, race == selfTargetRace))
                            {
                                selfTargetRace = race;
                            }

                            if (race == selfTargetRace)
                            {
                                ImGui.SetItemDefaultFocus();
                            }
                            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("The race to set yourself to"); }
                            ImGui.PopID();
                        }

                        ImGui.EndCombo();
                    }
                }

                this.plugin.UpdateSelfRace(selfTargetRace);

                //if (enableExperimental)
                //{
                    bool immersiveMode = Plugin.PluginConfig.ImmersiveMode;
                    ImGui.Checkbox("Immersive Mode", ref immersiveMode);
                    ImGui.Text("If Immersive Mode is enabled, \"Examine\" windows will also be modified.");
                    //ImGui.TextColored(WHAT_THE_HELL_ARE_YOU_DOING,"Experimental features may crash your game, uncat your boy,\nor cause the Eighth Umbral Calamity. YOU HAVE BEEN WARNED!");

                    this.plugin.UpdateImmersiveMode(immersiveMode);
                //}

                ImGui.Separator();
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

                if (!ShowWhy)
                {
                    if (ImGui.SmallButton("Why did you make this plugin?"))
                    {
                        ShowWhy = true;
                    }
                    if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Click me!"); }
                }
                if (ShowWhy)
                {
                    ImGui.Text("The previous version of this plugin was made a long time ago by another person. I revived it as a favor to someone else who asked me to.\n" +
                               "They have some lalafell friends who wear lewd/nsfw things on their lalafell characters and it made them feel uncomfortable. Using this\n" +
                               "plugin, they can now solve that. I understand some people may also have issues with other races/people - That's why there is an option\n" +
                               "to change all other players, along with allowing certain players to stay as they are and forcing just other certain players to change.\n" +
                               "\nI am a lalafell player myself, this is not a \"I hate lalas and wish I didn't have to ever see them\" thing.\n" +
                               "You also have the option of changing -all players- to Lalafell as well, if you wish.\n");
                    ImGui.TextColored(GreenMaybe, "Thanks for reading, you're awesome!");
                    if (ShowWhy)
                    {
                        if (ImGui.SmallButton("OK, I understand! ♥"))
                        {
                            ShowWhy = false;
                        }
                    }
                }
                ImGui.End();
            }

            Plugin.SettingsVisible = settingsVisible;
            this.plugin.SaveConfig();
        }
    }
}