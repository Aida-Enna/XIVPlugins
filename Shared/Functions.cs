using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.Gui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.IoC;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Veda
{
    public class Functions
    {
        public static bool ListContainsPlayer(List<PlayerData> PlayerList, string PlayerName, uint WorldId = 0)
        {
            int Result = 0;
            if (WorldId == 0)
            {
                Result = PlayerList.FindIndex(x => x.Name == PlayerName);
            }
            else
            {
                Result = PlayerList.FindIndex(x => x.Name == PlayerName && x.HomeworldId == WorldId);
            }
            if (Result == -1)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public static SeString BuildSeString(string PluginName, string Message, ushort LogType)
        {
            List<Payload> payloadList = new()
                        {
                            new TextPayload("[" + PluginName + "] "),
                            new UIForegroundPayload(LogType),
                            new TextPayload(Message),
                            new UIForegroundPayload(0)
                        };
            SeString seString = new(payloadList);
            return seString;
        }
    }
}