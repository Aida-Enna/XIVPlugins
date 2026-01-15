using Dalamud.Bindings.ImGui;
using Dalamud.Configuration;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;

namespace AutoLogin.Windows
{
    public unsafe class EmergencyExitWindow : Window, IDisposable
    {
        private string EECodeExplanation = "";
        private string ExitText = "If you get stuck in an endless loop of errors,\nyou can close the game by clicking below.";
        public int StartingPositionX = 0;
        public int StartingPositionY = 0;

        public EmergencyExitWindow(Plugin plugin) : base("Auto Login Emergency Exit###EEWindow")
        {
            Flags = ImGuiWindowFlags.AlwaysAutoResize;
            SizeCondition = ImGuiCond.Always;            
        }

        public void Dispose()
        { }

        public override void PreDraw()
        {
            PositionCondition = ImGuiCond.Appearing;
            //Position = new((Device.Instance()->Width / 2) + (Device.Instance()->Width / 10), Device.Instance()->Height / 2);
            var DialogueAddon = (AtkUnitBase*)Plugin.GameGui.GetAddonByName("Dialogue", 1).Address;
            if (DialogueAddon != null && DialogueAddon->IsVisible)
            {
                short X;
                short Y;
                short Width;
                short Height;
                DialogueAddon->GetPosition(&X, &Y);
                DialogueAddon->GetSize(&Width, &Height, true);
                StartingPositionX = (int)X + Width;
                StartingPositionY = (int)Y;
            }
            else
            {
                StartingPositionX = (int)((Device.Instance()->Width / 2) + (Device.Instance()->Width / 10));
                StartingPositionY = (int)(Device.Instance()->Height / 2);
            }
            Position = new System.Numerics.Vector2(StartingPositionX, StartingPositionY);
        }

        public override void Draw()
        {
            ImGui.SetWindowFocus();
            if (Plugin.PluginConfig.CurrentError != "none")
            {
                if (Convert.ToUInt16(Plugin.PluginConfig.CurrentError) == ErrorCode.LobbyConnectionError.GameCode)
                {
                    EECodeExplanation = ErrorCode.LobbyConnectionError.LongDescription;
                }
                else if (Convert.ToUInt16(Plugin.PluginConfig.CurrentError) == ErrorCode.SessionTokenExpired.GameCode)
                {
                    EECodeExplanation = ErrorCode.SessionTokenExpired.LongDescription;
                }
                else if (Convert.ToUInt16(Plugin.PluginConfig.CurrentError) == ErrorCode.E90002.GameCode)
                {
                    EECodeExplanation = ErrorCode.E90002.LongDescription;
                }
                ImGuiHelpers.CenteredText(EECodeExplanation);
                ImGui.Separator();
            }
            ImGuiHelpers.CenteredText(ExitText);
            ImGui.Indent((ImGui.CalcTextSize(EECodeExplanation + ExitText).X - ImGui.CalcTextSize("Exit Game").X) / 2);
            if (ImGui.Button("Exit Game"))
            {
                Environment.Exit(0);
            }
        }
    }
}