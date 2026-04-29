using Dalamud.Game;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using static FFXIVClientStructs.FFXIV.Client.System.String.Utf8String.Delegates;

namespace Veda
{
    public class Functions
    {
        public static void OpenWebsite(string URL)
        {
            Process.Start(new ProcessStartInfo { FileName = URL, UseShellExecute = true });
        }
    }
}