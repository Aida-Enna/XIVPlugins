//using System;
//using System.Numerics;
//using Dalamud.Interface.Windowing;
//using Dalamud.Configuration;
//using ImGuiNET;

//namespace FATEAutoSync.Windows;

//public class ConfigWindow : Window, IDisposable
//{
//    private Configuration Configuration;

//    public ConfigWindow(Plugin plugin) : base(
//        "FATE Auto Sync Configuration",
//        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
//        ImGuiWindowFlags.NoScrollWithMouse)
//    {
//        this.Size = new Vector2(300, 300);
//        this.SizeCondition = ImGuiCond.FirstUseEver;

//        this.Configuration = plugin.Configuration;
//    }

//    public void Dispose() { }

//    public override void Draw()
//    {
//        ImGui.Text($"The random config bool is {Configuration.SomePropertyToBeSavedAndWithADefault}");
//        can't ref a property, so use a local copy
//        var configValue = this.Configuration.SomePropertyToBeSavedAndWithADefault;
//        if (ImGui.Checkbox("Random Config Bool", ref configValue))
//        {
//            this.Configuration.SomePropertyToBeSavedAndWithADefault = configValue;
//            can save immediately on change, if you don't want to provide a "Save and Close" button
//            this.Configuration.Save();
//        }
//    }
//}