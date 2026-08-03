using System.Drawing;
using Server.MirEnv;

namespace Server.Library.MirDatabase.Conquest;

public class WallInfo
{
    public int Index;
    public Point Location;
    public int MobIndex;
    public string Name = string.Empty;
    public int RepairCost;

    public WallInfo() { }

    public WallInfo(BinaryReader reader)
    {
        Index = reader.ReadInt32();
        Location = new Point(reader.ReadInt32(), reader.ReadInt32());
        MobIndex = reader.ReadInt32();
        Name = reader.ReadString();

        if (Env.LoadVersion <= 84)
        {
            RepairCost = (int)reader.ReadUInt32();
        }
        else
        {
            RepairCost = reader.ReadInt32();
        }
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(Index);
        writer.Write(Location.X);
        writer.Write(Location.Y);
        writer.Write(MobIndex);
        writer.Write(Name);
        writer.Write(RepairCost);
    }

    public override string ToString()
    {
        return $"{Index} - {Name} ({Location})";
    }
}
