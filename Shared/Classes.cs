using Dalamud.Game.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Veda
{
    public class ColorType
    {
        //https://i.imgur.com/cZceCI3.png
        public const ushort Normal = 0;
        public const ushort Error = 17;
        public const ushort Success = 45;
        public const ushort Warn = 31;
        public const ushort Info = 37;
        public const ushort Twitch = 541;
    }
    public class PendingMessage
    {
        internal ulong ReceiverId { get; set; }
        internal ulong ContentId { get; set; } // 0 if unknown
        internal XivChatType Type { get; set; }
        internal int Timestamp { get; set; }
        internal SeString Sender { get; set; }
        internal SeString Content { get; set; }
    }
}
