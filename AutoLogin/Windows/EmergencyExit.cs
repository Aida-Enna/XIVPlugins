using Dalamud.Bindings.ImGui;
using Dalamud.Configuration;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using System;

namespace AutoLogin.Windows
{
    public unsafe class EmergencyExitWindow : Window, IDisposable
    {
        private string EECodeExplanation = "";

        private string ExitText = "If you get stuck in an endless loop of errors,\nyou can close the game by clicking below.\nPlease report this on the github repo.";

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
            switch (Plugin.PluginConfig.CurrentError)
            {
                // Hex / Decimal / Game
                // 0x32C9 / 13001 / 5006 Session token expired?
                // 0x332C / 13100 Auth failed
                // 0x3390 / 13200 Maintenance
                // 0x3E80 / 16000 Server connection lost
                case "2002":
                    EECodeExplanation = "Error 2002:\nThe lobby server gave an error. Auto-reconnection possible.\n";
                    break;
                case "5006":
                    EECodeExplanation = "Error 5006:\nYour session token has expired. You will need to close the game and login again.\n";
                    break;
            }
            string EEWindowText = EECodeExplanation + ExitText;
            ImGui.Text(EEWindowText);
            ImGui.Indent((ImGui.CalcTextSize(EEWindowText).X - ImGui.CalcTextSize("Exit Game").X) / 2);
            if (ImGui.Button("Exit Game"))
            {
                Environment.Exit(0);
            }
        }
    }
}