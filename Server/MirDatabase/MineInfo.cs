using System.Drawing;
﻿namespace Server.MirDatabase
{
    public class MineSet
    {
        public string Name = string.Empty;
        public byte SpotRegenRate = 5;
        public byte MaxStones = 80;
        public byte HitRate = 25;
        public byte DropRate = 10;
        public byte TotalSlots = 100;
        public List<MineDrop> Drops = [];
        private bool DropsSet;

        public MineSet(byte mineType = 0)
        {
            switch (mineType)
            {
                case 1:
                    TotalSlots = 120;
                    Drops.Add(new MineDrop() { ItemName = "GoldOre", MinSlot = 1, MaxSlot = 2, MinDura = 3, MaxDura = 16, BonusChance = 20, MaxBonusDura = 10 });
                    Drops.Add(new MineDrop() { ItemName = "SilverOre", MinSlot = 3, MaxSlot = 20, MinDura = 3, MaxDura = 16, BonusChance = 20, MaxBonusDura = 10 });
                    Drops.Add(new MineDrop() { ItemName = "CopperOre", MinSlot = 21, MaxSlot = 45, MinDura = 3, MaxDura = 16, BonusChance = 20, MaxBonusDura = 10 });
                    Drops.Add(new MineDrop() { ItemName = "BlackIronOre", MinSlot = 46, MaxSlot = 56, MinDura = 3, MaxDura = 16, BonusChance = 20, MaxBonusDura = 10 });
                    break;
                case 2:
                    TotalSlots = 100;
                    Drops.Add(new MineDrop() { ItemName = "PlatinumOre", MinSlot = 1, MaxSlot = 2, MinDura = 3, MaxDura = 16, BonusChance = 20, MaxBonusDura = 10 });
                    Drops.Add(new MineDrop() { ItemName = "RubyOre", MinSlot = 3, MaxSlot = 20, MinDura = 3, MaxDura = 16, BonusChance = 20, MaxBonusDura = 10 });
                    Drops.Add(new MineDrop() { ItemName = "NephriteOre", MinSlot = 21, MaxSlot = 45, MinDura = 3, MaxDura = 16, BonusChance = 20, MaxBonusDura = 10 });
                    Drops.Add(new MineDrop() { ItemName = "AmethystOre", MinSlot = 46, MaxSlot = 56, MinDura = 3, MaxDura = 16, BonusChance = 20, MaxBonusDura = 10 });
                    break;
            }
        }

        public void SetDrops(List<ItemInfo> items)
        {
            if (DropsSet) return;
            foreach (var drop in Drops)
            {
                foreach (var item in items.Where(item => string.Compare(item.Name.Replace(" ", ""), drop.ItemName, StringComparison.OrdinalIgnoreCase) == 0))
                {
                    drop.Item = item;
                    break;
                }
            }
            DropsSet = true;
        }
    }

    public class MineSpot
    {
        public byte StonesLeft = 0;
        public long LastRegenTick = 0;
        public MineSet? Mine;
    }

    public class MineDrop
    {
        public string ItemName = string.Empty;
        public ItemInfo? Item;
        public byte MinSlot;
        public byte MaxSlot;
        public byte MinDura = 1;
        public byte MaxDura = 1;
        public byte BonusChance;
        public byte MaxBonusDura = 1;
    }

    public class MineZone
    {
        public byte Mine;
        public Point Location;
        public ushort Size;

        public MineZone()
        {
        }

        public MineZone(BinaryReader reader)
        {
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Size = reader.ReadUInt16();
            Mine = reader.ReadByte();
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write(Size);
            writer.Write(Mine);
        }
        public override string ToString()
        {
            return $"Mine: {Functions.PointToString(Location)}- {Mine}";
        }
    }
}
