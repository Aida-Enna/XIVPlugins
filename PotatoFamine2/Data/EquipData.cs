using System.Runtime.InteropServices;

namespace PotatoFamine2
{
    public class EquipDataOffsets
    {
        public const int Model = 0x0;
        public const int Variant = 0x2;
        public const int Dye = 0x3;
    }
    
    [StructLayout(LayoutKind.Explicit)]
    public struct EquipData
    {
        [FieldOffset(EquipDataOffsets.Model)] public short model;
        [FieldOffset(EquipDataOffsets.Variant)] public byte variant;
        [FieldOffset(EquipDataOffsets.Dye)] public byte dye;
    }
}