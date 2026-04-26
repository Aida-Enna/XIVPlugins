# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build

This is a collection of Dalamud FFXIV plugins built with MSBuild. The solution file is `XIVPlugins (3rd Party).sln`.

**VS Code tasks (`.vscode/tasks.json`)**:
- `Debug Build` (default) — builds all plugins in Debug|x64
- `Release Build` — builds all plugins in Release|x64

**From command line** (requires MSBuild on PATH or Visual Studio):
```
msbuild "XIVPlugins (3rd Party).sln" /p:Configuration=Debug /p:Platform=x64
msbuild "XIVPlugins (3rd Party).sln" /p:Configuration=Release /p:Platform=x64
```

Build output lands in per-plugin `Build\<PluginName>\` directories. There are no test projects in this repository. There is no lint step; `.editorconfig` silences CA1416 platform-compatibility warnings across the solution.

## Plugins

Seven active plugins, each in its own directory and `.csproj`. All target `net10.0` or `net10.0-windows7.0` via `Dalamud.NET.Sdk`.

| Plugin | Purpose |
|--------|---------|
| `AutoLogin` | Auto-login a character at startup; auto-reconnect on disconnect |
| `AutoPillion` | Auto-mount party members on multi-seat mounts |
| `MacroChainRedux` | Chain macros with `/nextmacro` / `/runmacro` |
| `PortraitFixer` | Re-syncs portrait when a gearset is updated |
| `FoodCheck` | Warns party members missing food buff at countdown/ready-check |
| `CustomSounds` | Custom audio triggers for in-game events |
| `BangbooPlugin` | Applies Glamourer outfits via Penumbra/Glamourer IPC |

`/Deprecated` contains unmaintained plugins (LootMaster, PotatoFamine2, etc.) — do not edit them.

`/Plogon` contains pre-built plugin archives and metadata JSONs used for distribution. `repo.json` is the Dalamud plugin repository manifest.

## Architecture

### Standard Plugin Pattern

Every plugin follows the same entry-point shape:

```csharp
public class Plugin : IDalamudPlugin {
    // Services injected via [PluginService] static properties
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; set; }
    [PluginService] public static IFramework Framework { get; set; }

    public static Configuration PluginConfig { get; set; }

    public Plugin(IDalamudPluginInterface pi, ...) {
        PluginConfig = PluginInterface.GetPluginConfig() ?? new Configuration();
        PluginConfig.Initialize(PluginInterface);
        // Register UI, commands, hooks, framework events
    }
    public void Dispose() { /* reverse all registrations */ }
}
```

Configuration classes implement `IPluginConfiguration` and persist via `pluginInterface.SavePluginConfig(this)`.

### Shared Utilities (`/Shared`)

- **`PluginCommandManager<T>`** — reflection-based command registration; scans the host type for methods annotated with `[Command]`, `[Aliases]`, and `[HelpMessage]` attributes and registers them with Dalamud's `ICommandManager`.
- **`Attributes.cs`** — defines those command attributes.
- **`MessageBoxWindow.cs`** — reusable ImGui modal dialog; inherits `Dalamud.Interface.Windowing.Window`.
- **`Classes.cs`** — `ColorType` constants for coloured chat output.

### Windows

All UI windows inherit `Dalamud.Interface.Windowing.Window`, override `Draw()` for ImGui rendering, and are collected in a `WindowSystem`. The `WindowSystem.Draw()` call is wired to `IUiBuilder.Draw`.

### Hooks

Game function hooks use `IGameInteropProvider.HookFromAddress<DelegateType>(address, detour)`. Active examples:
- **AutoLogin**: `LobbyErrorHandlerHook` — intercepts disconnect error codes.
- **MacroChainRedux**: `RaptureShellModule.ExecuteMacro` — detects macro completion.
- **FoodCheck**: CountdownTimer / ReadyCheck hooks.
- **PortraitFixer**: `RaptureGearsetModule.UpdateGearset`.

### AutoLogin — Action Queue Pattern

AutoLogin is the most complex plugin. It uses a `Queue<Func<bool>>` to sequence UI interactions after a disconnect. Each frame (`Framework.Update`), it dequeues and invokes the next action; returning `false` re-queues it for the next frame. UI interactions use `GenerateCallback(AtkUnitBase*, params object[])` to invoke game addon callbacks, marshalling values as `AtkValue` structs.

The `EmergencyExitWindow` is shown whenever the `DialogueBox` addon is visible (indicating an error state) and provides a fast-exit button to prevent an infinite reconnect loop.

### IPC (BangbooPlugin)

Uses Penumbra's `IpcSubscribers.EventSubscriber` pattern to subscribe to `CreatingCharacterBase` events, then calls Glamourer IPC to apply an outfit. Follow this pattern if adding more cross-plugin communication.

### Framework Update

Plugins that need per-frame logic register a callback on `Framework.Update`. Common uses: polling action queues (AutoLogin), cooldown timers (AutoPillion), event detection (CustomSounds).
