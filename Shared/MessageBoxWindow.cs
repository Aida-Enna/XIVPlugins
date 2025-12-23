using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System;

namespace Veda.Windows
{
    public class MessageBoxWindow : Window, IDisposable
    {
        public static string MessageBoxText = "Text";

        public MessageBoxWindow() : base("Auto Login New Feature Notification/Explanation###MessageBoxWindow")
        {
            Flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse;
            SizeCondition = ImGuiCond.Always;
        }

        public void Dispose()
        { }

        public override void PreDraw()
        {
        }

        public override void Draw()
        {
            ImGui.SetWindowFocus();
            ImGui.Text(MessageBoxText);
            ImGui.Indent((ImGui.CalcTextSize(MessageBoxText).X - ImGui.CalcTextSize("Click to close").X) / 2);
            if (ImGui.Button("Click to close"))
            {
                Toggle();
            }
        }
    }
}