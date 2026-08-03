using System.Drawing;
using Server.MirEnv;

namespace Server.MirDatabase
{
    public class NPCInfo
    {
        protected static Env EditEnv => Env.Edit;

        public int Index;

        public string FileName = string.Empty, Name = string.Empty;

        public int MapIndex;
        public Point Location;
        public ushort Rate = 100;
        public ushort Image;
        public Color Colour;

        public bool TimeVisible = false;
        public byte HourStart = 0;
        public byte MinuteStart = 0;
        public byte HourEnd = 0;
        public byte MinuteEnd = 1;
        public short MinLev = 0;
        public short MaxLev = 0;
        public string DayOfWeek = string.Empty;
        public string ClassRequired = string.Empty;
        public bool Sabuk = false;
        public int FlagNeeded = 0;
        public int Conquest;
        public bool ShowOnBigMap;
        public int BigMapIcon;
        public bool CanTeleportTo;
        public bool ConquestVisible = true;

        public List<int> CollectQuestIndexes = [];
        public List<int> FinishQuestIndexes = [];

        public NPCInfo() { }
        public NPCInfo(BinaryReader reader)
        {
            Index = reader.ReadInt32();
            MapIndex = reader.ReadInt32();

            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
                CollectQuestIndexes.Add(reader.ReadInt32());

            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
                FinishQuestIndexes.Add(reader.ReadInt32());

            FileName = reader.ReadString();
            Name = reader.ReadString();

            Location = new Point(reader.ReadInt32(), reader.ReadInt32());

            if (Env.LoadVersion >= 72)
            {
                Image = reader.ReadUInt16();
            }
            else
            {
                Image = reader.ReadByte();
            }

            Rate = reader.ReadUInt16();

            if (Env.LoadVersion >= 64)
            {
                TimeVisible = reader.ReadBoolean();
                HourStart = reader.ReadByte();
                MinuteStart = reader.ReadByte();
                HourEnd = reader.ReadByte();
                MinuteEnd = reader.ReadByte();
                MinLev = reader.ReadInt16();
                MaxLev = reader.ReadInt16();
                DayOfWeek = reader.ReadString();
                ClassRequired = reader.ReadString();
                if (Env.LoadVersion >= 66)
                    Conquest = reader.ReadInt32();
                else
                    Sabuk = reader.ReadBoolean();
                FlagNeeded = reader.ReadInt32();
            }

            if (Env.LoadVersion > 95)
            {
                ShowOnBigMap = reader.ReadBoolean();
                BigMapIcon = reader.ReadInt32();
            }
            if (Env.LoadVersion > 96)
                CanTeleportTo = reader.ReadBoolean();

            if (Env.LoadVersion >= 107)
            {
                ConquestVisible = reader.ReadBoolean();
            }
        }
        public void Save(BinaryWriter writer)
        {
            writer.Write(Index);
            writer.Write(MapIndex);

            writer.Write(CollectQuestIndexes.Count);
            foreach (int questIndex in CollectQuestIndexes)
                writer.Write(questIndex);

            writer.Write(FinishQuestIndexes.Count);
            foreach (int questIndex in FinishQuestIndexes)
                writer.Write(questIndex);

            writer.Write(FileName);
            writer.Write(Name);

            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write(Image);
            writer.Write(Rate);

            writer.Write(TimeVisible);
            writer.Write(HourStart);
            writer.Write(MinuteStart);
            writer.Write(HourEnd);
            writer.Write(MinuteEnd);
            writer.Write(MinLev);
            writer.Write(MaxLev);
            writer.Write(DayOfWeek);
            writer.Write(ClassRequired);
            writer.Write(Conquest);
            writer.Write(FlagNeeded);

            writer.Write(ShowOnBigMap);
            writer.Write(BigMapIcon);
            writer.Write(CanTeleportTo);
            writer.Write(ConquestVisible);
        }

        public static void FromText(string text)
        {
            string[] data = text.Split([','], StringSplitOptions.RemoveEmptyEntries);

            if (data.Length < 6) return;

            NPCInfo? info;
            bool isNew = false;
            if (!int.TryParse(data[0], out var index))
            {
                index = -1;
            }
            if (index == -1 || (info = EditEnv.NPCInfoList.FirstOrDefault(d => d.Index == index)) == null)
            {
                info = new NPCInfo() { Index = ++EditEnv.NPCIndex };
                isNew = true;
            }
            info.FileName = data[1];

            //TODO 
            info.MapIndex = EditEnv.MapInfoList.FirstOrDefault(d => d.FileName == data[2])!.Index;

            if (!int.TryParse(data[3], out int x)) return;
            if (!int.TryParse(data[4], out int y)) return;

            info.Location = new Point(x, y);

            info.Name = data[5];

            if (!ushort.TryParse(data[6], out info.Image)) return;
            if (!ushort.TryParse(data[7], out info.Rate)) return;

            if (!bool.TryParse(data[8], out info.ShowOnBigMap)) return;
            if (!int.TryParse(data[9], out info.BigMapIcon)) return;
            if (!bool.TryParse(data[10], out info.CanTeleportTo)) return;
            if (!bool.TryParse(data[11], out info.ConquestVisible)) return;
            if (!short.TryParse(data[12], out info.MinLev)) return;
            if (!short.TryParse(data[13], out info.MaxLev)) return;
            if (!bool.TryParse(data[14], out info.TimeVisible)) return;
            if (!byte.TryParse(data[15], out info.HourStart)) return;
            if (!byte.TryParse(data[16], out info.MinuteStart)) return;
            if (!byte.TryParse(data[17], out info.HourEnd)) return;
            if (!byte.TryParse(data[18], out info.MinuteEnd)) return;

            if (isNew) EditEnv.NPCInfoList.Add(info);
        }
        public string ToText()
        {
            return
                $"{Index},{FileName},{EditEnv.MapInfoList.FirstOrDefault(d => d.Index == MapIndex)?.FileName},{Location.X},{Location.Y},{Name},{Image},{Rate},{ShowOnBigMap},{BigMapIcon},{CanTeleportTo},{ConquestVisible},{MinLev},{MaxLev},{TimeVisible},{HourStart},{MinuteStart},{HourEnd},{MinuteEnd}";
        }

        public override string ToString()
        {
            return $" [{Index}] {FileName}: {Name}, {Functions.PointToString(Location)}";
        }

        public string GameName
        {
            get
            {
                string s = Name;
                if (s.Contains('_'))
                {
                    string[] splitName = s.Split('_');
                    s = splitName[^1];
                }
                return s;
            }
        }

        public ClientNPCInfo ClientInformation =>
            new()
            {
                ObjectID = 0,
                Index = Index,
                FileName = FileName,
                Name = Name,
                MapIndex = MapIndex,
                Location = Location,
                Image = Image,
                Rate = Rate,
                ShowOnBigMap = ShowOnBigMap,
                BigMapIcon = BigMapIcon,
                Icon = BigMapIcon,
                CanTeleportTo = CanTeleportTo
            };
    }
}
