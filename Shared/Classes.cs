using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Veda
{
    public class LogType
    {
        public const ushort Normal = 0;
        public const ushort Error = 17;
        public const ushort Success = 45;
    }

    public class PlayerData
    {
        public string Name
        {
            get => $"{this.Firstname} {this.Lastname}";
            set
            {
                var name = value.Split(' ');
                this.Firstname = name[0];
                this.Lastname = name[1];
            }
        }

        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string HomeworldName { get; set; }
        public string Time { get; set; }
        public int HomeworldId { get; set; }
    }
}
