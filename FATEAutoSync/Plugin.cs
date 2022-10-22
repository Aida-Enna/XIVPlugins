using System;
using Dalamud.Plugin;
using FATEAutoSync.Attributes;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.Logging;
using Dalamud.Game.Command;

namespace FATEAutoSync
{
  public class Plugin : IDalamudPlugin
  {
    public string Name => "FATE AutoSync";

    private DalamudPluginInterface pluginInterface;
    private Framework framework;
    private SigScanner sigScanner;
    private PluginCommandManager<Plugin> commandManager;
    private Configuration config;
    // private PluginUI ui;

    // Command execution
    private delegate void ProcessChatBoxDelegate(IntPtr uiModule, IntPtr message, IntPtr unused, byte a4);
    private delegate IntPtr GetUIModuleDelegate(IntPtr basePtr);
    private ProcessChatBoxDelegate? ProcessChatBox;
    private IntPtr uiModule = IntPtr.Zero;

    // Our specific stuff
    private IntPtr inFateAreaPtr = IntPtr.Zero;
    private bool inFateArea = false;
    private bool firstRun = true;
    private ChatGui chat;

    public Plugin(DalamudPluginInterface pluginInterface, ChatGui chat, Framework framework, CommandManager commands, SigScanner sigScanner)
	  {
      this.pluginInterface = pluginInterface;
      this.chat = chat;
      this.framework = framework;
      this.sigScanner = sigScanner;

      config = this.pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
      config.Initialize(this.pluginInterface);

      // this.ui = new PluginUI();
      // this.pluginInterface.UiBuilder.OnBuildUi += this.ui.Draw;

      framework.Update += Update;

      commandManager = new PluginCommandManager<Plugin>(this, commands);

      InitializePointers();
    }

    private void Update(Dalamud.Game.Framework framework)
    {
      if (!this.config.enabled) return;

      var wasInFateArea = inFateArea;
      inFateArea = Marshal.ReadByte(inFateAreaPtr) == 1;
      if (wasInFateArea != inFateArea)
      {
        if (inFateArea)
        {
          if (firstRun)
          {
            chat.Print("FATE Auto Sync ran for the first time in this session (/fateautosync to toggle)");
            firstRun = false;
          }
          ExecuteCommand("/levelsync on");
        }
      }
    }

    [Command("/fateautosync")]
    [HelpMessage("Toggles the plug-in on and off.")]
    public void ToggleCommand(string command, string args)
    {
      this.config.enabled = !this.config.enabled;
      chat.Print($"Toggled FATE Auto Sync {(this.config.enabled ? "on" : "off")}");
    }

    // Courtesy of https://github.com/UnknownX7/QoLBar
    private unsafe void InitializePointers()
    {
      // FATE pointer (thanks to Pohky#8008)
      try
      {
        var sig = sigScanner.ScanText("80 3D ?? ?? ?? ?? ?? 0F 84 ?? ?? ?? ?? 48 8B 42 20");
        inFateAreaPtr = sig + Marshal.ReadInt32(sig, 2) + 7;
        chat.Print("Retrieved 'inFateAreaPtr' successfully");
        chat.Print(inFateAreaPtr.ToString("X8"));
      }
      catch
      {
        PluginLog.Error("Failed loading 'inFateAreaPtr'");
      }

      // for ExecuteCommand
      try
      {
        var getUIModulePtr = sigScanner.ScanText("E8 ?? ?? ?? ?? 48 83 7F ?? 00 48 8B F0");
        var easierProcessChatBoxPtr = sigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 45 84 C9");
        var uiModulePtr = sigScanner.GetStaticAddressFromSig("48 8B 0D ?? ?? ?? ?? 48 8D 54 24 ?? 48 83 C1 10 E8");

        var GetUIModule = Marshal.GetDelegateForFunctionPointer<GetUIModuleDelegate>(getUIModulePtr);

        uiModule = GetUIModule(*(IntPtr*)uiModulePtr);
        ProcessChatBox = Marshal.GetDelegateForFunctionPointer<ProcessChatBoxDelegate>(easierProcessChatBoxPtr);
      }
      catch { PluginLog.Error("Failed loading 'ExecuteCommand'"); }
    }

    public void ExecuteCommand(string command)
    {
      try
      {
        var bytes = System.Text.Encoding.UTF8.GetBytes(command);

        var mem1 = Marshal.AllocHGlobal(400);
        var mem2 = Marshal.AllocHGlobal(bytes.Length + 30);

        Marshal.Copy(bytes, 0, mem2, bytes.Length);
        Marshal.WriteByte(mem2 + bytes.Length, 0);
        Marshal.WriteInt64(mem1, mem2.ToInt64());
        Marshal.WriteInt64(mem1 + 8, 64);
        Marshal.WriteInt64(mem1 + 8 + 8, bytes.Length + 1);
        Marshal.WriteInt64(mem1 + 8 + 8 + 8, 0);

        ProcessChatBox(uiModule, mem1, IntPtr.Zero, 0);

        Marshal.FreeHGlobal(mem1);
        Marshal.FreeHGlobal(mem2);
      }
      catch (Exception err) { chat.PrintError(err.Message); }
    }

    #region IDisposable Support
    protected virtual void Dispose(bool disposing)
    {
      if (!disposing) return;

      this.commandManager.Dispose();

      this.pluginInterface.SavePluginConfig(this.config);

      // this.pluginInterface.UiBuilder.OnBuildUi -= this.ui.Draw;

      framework.Update -= Update;

      this.pluginInterface.Dispose();
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }
    #endregion
  }
}
