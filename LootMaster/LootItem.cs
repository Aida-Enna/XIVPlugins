//using FFXIVClientStructs.FFXIV.Client.Game.UI;
//using System.Runtime.InteropServices;

//namespace LootMaster
//{
//    [StructLayout(LayoutKind.Explicit, Size = 64)]
//    public struct LootItem
//    {
//        [FieldOffset(0)]
//        public uint ObjectId;
//        [FieldOffset(4)]
//        public uint ChestItemIndex;
//        [FieldOffset(8)]
//        public uint ItemId;
//        [FieldOffset(12)]
//        public uint ItemCount;
//        [FieldOffset(32)]
//        public RollState RollState;
//        [FieldOffset(36)]
//        public RollOption RolledState;
//        [FieldOffset(40)]
//        public uint RolledValue;
//        [FieldOffset(44)]
//        public float LeftRollTime;
//        [FieldOffset(48)]
//        public float TotalRollTime;
//        [FieldOffset(56)]
//        public LootMode LootMode;
//        [FieldOffset(60)]
//        public uint Index;

//        public bool Rolled => RolledState > 0;

//        public bool Valid => ObjectId != 3758096384U && ObjectId > 0U;
//    }
//}
