using Dalamud.Bindings.ImGui;
using Dalamud.Configuration;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using System;

namespace AutoLogin.Windows
{
    public unsafe class EmergencyExitWindow : Window, IDisposable
    {
        private string EECodeExplanation = "";
        private string ExitText = "If you get stuck in an endless loop of errors,\nyou can close the game by clicking below.";

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
            Position = new((Device.Instance()->Width / 2) + (Device.Instance()->Width / 10), Device.Instance()->Height / 2);
        }

        public override void Draw()
        {
            ImGui.SetWindowFocus();
            switch (Convert.ToUInt16(Plugin.PluginConfig.CurrentError))
            {
                case ErrorCode.LobbyConnectionError:
                    EECodeExplanation = "Error 2002:\nThe lobby server gave an error.\nAuto-reconnection possible.\n";
                    break;
                case ErrorCode.SessionTokenExpired:
                    EECodeExplanation = "Error 5006:\nYour session token has expired.You will need to close the game and login again.\n";
                    break;
                case ErrorCode.E90002:
                    EECodeExplanation = "Error 90002:\nYou have been disconnected from the server.\nAuto-reconnection possible.\n";
                    break;
            }
            ImGuiHelpers.CenteredText(EECodeExplanation);
            ImGui.Separator();
            ImGuiHelpers.CenteredText(ExitText);
            ImGui.Indent((ImGui.CalcTextSize(EECodeExplanation + ExitText).X - ImGui.CalcTextSize("Exit Game").X) / 2);
            if (ImGui.Button("Exit Game"))
            {
                Environment.Exit(0);
            }
        }
    }
}