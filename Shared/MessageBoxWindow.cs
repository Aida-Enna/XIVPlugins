using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System;

namespace Veda.Windows
{
    public class MessageBoxWindow : Window, IDisposable
    {
        public string MessageBoxText = "N/A";

        // We give this window a constant ID using ###.
        // This allows for labels to be dynamic, like "{FPS Counter}fps###XYZ counter window",
        // and the window ID will always be "###XYZ counter window" for ImGui
        public MessageBoxWindow() : base("###MessageBoxWindow")
        {
            Flags = ImGuiWindowFlags.AlwaysAutoResize;
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
            if (ImGui.Button("OK"))
            {
                Toggle();
            }
        }
    }
}