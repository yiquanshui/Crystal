using Server.MirDatabase;
using Server.MirObjects;

namespace Server.Library.MirDatabase.Conquest;

public class GuildInfo
{
    public List<GuildArcherInfo> ArcherList = [];
    public List<GuildGateInfo> GateList = [];
    public List<GuildWallInfo> WallList = [];
    public List<GuildSiegeInfo> SiegeList = [];
    public List<GuildFlagInfo> FlagList = [];

    public Dictionary<GuildFlagInfo, Dictionary<GuildObject, int>> ControlPoints = new();

    public int Owner = 0;
    public uint GoldStorage;
    public int AttackerID;
    public byte NPCRate = 0;

    public ConquestInfo Info;

    public bool NeedSave = false;

    public GuildInfo() { }

    public GuildInfo(BinaryReader reader)
    {
        Owner = reader.ReadInt32();

        var archerCount = reader.ReadInt32();
        for (int i = 0; i < archerCount; i++)
        {
            ArcherList.Add(new GuildArcherInfo(reader));
        }

        var gateCount = reader.ReadInt32();
        for (int i = 0; i < gateCount; i++)
        {
            GateList.Add(new GuildGateInfo(reader));
        }

        var wallCount = reader.ReadInt32();
        for (int i = 0; i < wallCount; i++)
        {
            WallList.Add(new GuildWallInfo(reader));
        }

        var siegeCount = reader.ReadInt32();
        for (int i = 0; i < siegeCount; i++)
        {
            SiegeList.Add(new GuildSiegeInfo(reader));
        }

        GoldStorage = reader.ReadUInt32();
        NPCRate = reader.ReadByte();
        AttackerID = reader.ReadInt32();
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(Owner);
        writer.Write(ArcherList.Count);
        for (int i = 0; i < ArcherList.Count; i++)
        {
            ArcherList[i].Save(writer);
        }

        writer.Write(GateList.Count);
        for (int i = 0; i < GateList.Count; i++)
        {
            GateList[i].Save(writer);
        }

        writer.Write(WallList.Count);
        for (int i = 0; i < WallList.Count; i++)
        {
            WallList[i].Save(writer);
        }

        writer.Write(SiegeList.Count);
        for (int i = 0; i < SiegeList.Count; i++)
        {
            SiegeList[i].Save(writer);
        }

        writer.Write(GoldStorage);
        writer.Write(NPCRate);
        writer.Write(AttackerID);
    }
}