using System;
using Dalamud.Plugin;
using Glamourer.Api.Enums;
using Glamourer.Api.IpcSubscribers;

namespace BangbooPlugin.Utils;

public class GlamourerIPC : IDisposable
{

    private ApplyState ApplyState;

    public GlamourerIPC(IDalamudPluginInterface pluginInterface)
    {

        ApplyState = new ApplyState(pluginInterface);

    }

    public void ApplyOutfit(ushort objectIndex, String outfit)
    {
        ApplyState.Invoke(outfit, objectIndex, 0, ApplyFlag.Customization | ApplyFlag.Once);
    }

    public void Dispose() { }
}
