using System.Drawing;
using Server.MirEnv;

namespace Server.Library.MirDatabase.Conquest;

public class ConquestInfo
{
    public int Index;
    public bool FullMap;
    public Point Location;
    public ushort Size;
    public string Name = string.Empty;
    public int MapIndex;
    public int PalaceIndex;

    public List<int> ExtraMaps = [];
    public List<ArcherInfo> ConquestGuards = [];
    public List<GateInfo> ConquestGates = [];
    public List<WallInfo> ConquestWalls = [];
    public List<SiegeInfo> ConquestSieges = [];
    public List<FlagInfo> ConquestFlags = [];

    public int GuardIndex;
    public int GateIndex;
    public int WallIndex;
    public int SiegeIndex;
    public int FlagIndex;

    public byte StartHour = 0;
    public int WarLength = 60;

    public ConquestType Type = ConquestType.Request;
    public ConquestGame Game = ConquestGame.CapturePalace;

    public bool Monday;
    public bool Tuesday;
    public bool Wednesday;
    public bool Thursday;
    public bool Friday;
    public bool Saturday;
    public bool Sunday;

    //King of the hill
    public Point KingLocation;
    public ushort KingSize;

    //Control points
    public readonly List<FlagInfo> ControlPoints = [];
    public int ControlPointIndex;

    public ConquestInfo() { }

    public ConquestInfo(BinaryReader reader)
    {
        Index = reader.ReadInt32();

        if (Env.LoadVersion > 73)
        {
            FullMap = reader.ReadBoolean();
        }

        Location = new Point(reader.ReadInt32(), reader.ReadInt32());
        Size = reader.ReadUInt16();
        Name = reader.ReadString();
        MapIndex = reader.ReadInt32();
        PalaceIndex = reader.ReadInt32();
        GuardIndex = reader.ReadInt32();
        GateIndex = reader.ReadInt32();
        WallIndex = reader.ReadInt32();
        SiegeIndex = reader.ReadInt32();

        if (Env.LoadVersion > 72)
        {
            FlagIndex = reader.ReadInt32();
        }

        var counter = reader.ReadInt32();
        for (int i = 0; i < counter; i++)
        {
            ConquestGuards.Add(new ArcherInfo(reader));
        }

        counter = reader.ReadInt32();
        for (int i = 0; i < counter; i++)
        {
            ExtraMaps.Add(reader.ReadInt32());
        }

        counter = reader.ReadInt32();
        for (int i = 0; i < counter; i++)
        {
            ConquestGates.Add(new GateInfo(reader));
        }

        counter = reader.ReadInt32();
        for (int i = 0; i < counter; i++)
        {
            ConquestWalls.Add(new WallInfo(reader));
        }

        counter = reader.ReadInt32();
        for (int i = 0; i < counter; i++)
        {
            ConquestSieges.Add(new SiegeInfo(reader));
        }

        if (Env.LoadVersion > 72)
        {
            counter = reader.ReadInt32();
            for (int i = 0; i < counter; i++)
            {
                ConquestFlags.Add(new FlagInfo(reader));
            }
        }

        StartHour = reader.ReadByte();
        WarLength = reader.ReadInt32();
        Type = (ConquestType)reader.ReadByte();
        Game = (ConquestGame)reader.ReadByte();

        Monday = reader.ReadBoolean();
        Tuesday = reader.ReadBoolean();
        Wednesday = reader.ReadBoolean();
        Thursday = reader.ReadBoolean();
        Friday = reader.ReadBoolean();
        Saturday = reader.ReadBoolean();
        Sunday = reader.ReadBoolean();

        KingLocation = new Point(reader.ReadInt32(), reader.ReadInt32());
        KingSize = reader.ReadUInt16();

        if (Env.LoadVersion > 74)
        {
            ControlPointIndex = reader.ReadInt32();
            counter = reader.ReadInt32();
            for (int i = 0; i < counter; i++)
            {
                ControlPoints.Add(new FlagInfo(reader));
            }
        }
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(Index);
        writer.Write(FullMap);
        writer.Write(Location.X);
        writer.Write(Location.Y);
        writer.Write(Size);
        writer.Write(Name);
        writer.Write(MapIndex);
        writer.Write(PalaceIndex);
        writer.Write(GuardIndex);
        writer.Write(GateIndex);
        writer.Write(WallIndex);
        writer.Write(SiegeIndex);
        writer.Write(FlagIndex);

        writer.Write(ConquestGuards.Count);
        foreach (var guard in ConquestGuards)
        {
            guard.Save(writer);
        }

        writer.Write(ExtraMaps.Count);
        foreach (int extraMap in ExtraMaps)
        {
            writer.Write(extraMap);
        }

        writer.Write(ConquestGates.Count);
        foreach (var gate in ConquestGates)
        {
            gate.Save(writer);
        }

        writer.Write(ConquestWalls.Count);
        foreach (var wall in ConquestWalls)
        {
            wall.Save(writer);
        }

        writer.Write(ConquestSieges.Count);
        foreach (var siege in ConquestSieges)
        {
            siege.Save(writer);
        }

        writer.Write(ConquestFlags.Count);
        foreach (var flag in ConquestFlags)
        {
            flag.Save(writer);
        }

        writer.Write(StartHour);
        writer.Write(WarLength);
        writer.Write((byte)Type);
        writer.Write((byte)Game);

        writer.Write(Monday);
        writer.Write(Tuesday);
        writer.Write(Wednesday);
        writer.Write(Thursday);
        writer.Write(Friday);
        writer.Write(Saturday);
        writer.Write(Sunday);

        writer.Write(KingLocation.X);
        writer.Write(KingLocation.Y);
        writer.Write(KingSize);

        writer.Write(ControlPointIndex);
        writer.Write(ControlPoints.Count);
        foreach (var point in ControlPoints)
        {
            point.Save(writer);
        }
    }

    public override string ToString()
    {
        return $"{Index}- {Name}";
    }
}