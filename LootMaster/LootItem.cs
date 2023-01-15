using System.Runtime.InteropServices;

namespace DalamudPluginProjectTemplate
{
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public struct LootItem
    {
        [FieldOffset(0)]
        public uint ObjectId;
        [FieldOffset(8)]
        public uint ItemId;
        [FieldOffset(32)]
        public RollState RollState;
        [FieldOffset(36)]
        public RollOption RolledState;
        [FieldOffset(44)]
        public float LeftRollTime;
        [FieldOffset(32)]
        public float TotalRollTime;
        [FieldOffset(60)]
        public uint Index;

        public bool Rolled => RolledState > 0;

        public bool Valid => ObjectId != 3758096384U && ObjectId > 0U;
    }
}
