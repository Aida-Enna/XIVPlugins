using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Numerics;

namespace FoodCheck
{
    public class PluginWindow : Window
    {
        public PluginWindow() : base("TemplateWindow")
        {
            IsOpen = false;
            Size = new Vector2(810, 520);
            SizeCondition = ImGuiCond.FirstUseEver;
        }

        public override void Draw()
        {
            ImGui.Text("Hello, world!");
        }
    }
}
