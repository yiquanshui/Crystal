using System.Drawing;
using Server.MirEnv;

namespace Server.MirDatabase
{
    public class MapInfo
    {
        protected static Env Env => Env.Main;

        protected static Env EditEnv => Env.Edit;

        public int Index;
        public string FileName = string.Empty, Title = string.Empty;
        public ushort MiniMap, BigMap, Music;
        public LightSetting Light;
        public byte MapDarkLight = 0, MineIndex = 0, GTIndex = 0;

        public bool NoTeleport, NoReconnect, NoRandom, NoEscape, NoRecall, NoDrug, NoPosition, NoFight,
            NoThrowItem, NoDropPlayer, NoDropMonster, NoNames, NoMount, NeedBridle, Fight, NeedHole, Fire, Lightning,
            NoTownTeleport, NoReincarnation, GT, NoExperience, NoGroup = false, NoPets, NoIntelligentCreatures, NoHero, RequiredGroup = false, FireWallLimit;

        public int RequiredGroupSize = 0, FireWallCount = 0;


        public string NoReconnectMap = string.Empty;
        public int FireDamage, LightningDamage;

        public List<SafeZoneInfo> SafeZones = [];
        public List<MovementInfo> Movements = [];
        public List<RespawnInfo> Respawns = [];
        public List<NPCInfo> NPCs = [];
        public List<MineZone> MineZones = [];
        public List<Point> ActiveCoords = [];
        public WeatherSetting WeatherParticles = WeatherSetting.None;

        public MapInfo()
        {

        }

        public MapInfo(BinaryReader reader)
        {
            Index = reader.ReadInt32();
            FileName = reader.ReadString();
            Title = reader.ReadString();
            MiniMap = reader.ReadUInt16();
            Light = (LightSetting)reader.ReadByte();

            BigMap = reader.ReadUInt16();

            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
                SafeZones.Add(new SafeZoneInfo(reader) { Info = this });

            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
                Respawns.Add(new RespawnInfo(reader, Env.LoadVersion, Env.LoadCustomVersion));

            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
                Movements.Add(new MovementInfo(reader));

            NoTeleport = reader.ReadBoolean();
            NoReconnect = reader.ReadBoolean();
            NoReconnectMap = reader.ReadString();

            NoRandom = reader.ReadBoolean();
            NoEscape = reader.ReadBoolean();
            NoRecall = reader.ReadBoolean();
            NoDrug = reader.ReadBoolean();
            NoPosition = reader.ReadBoolean();
            NoThrowItem = reader.ReadBoolean();
            NoDropPlayer = reader.ReadBoolean();
            NoDropMonster = reader.ReadBoolean();
            NoNames = reader.ReadBoolean();
            Fight = reader.ReadBoolean();
            Fire = reader.ReadBoolean();
            FireDamage = reader.ReadInt32();
            Lightning = reader.ReadBoolean();
            LightningDamage = reader.ReadInt32();
            MapDarkLight = reader.ReadByte();
            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
                MineZones.Add(new MineZone(reader));
            MineIndex = reader.ReadByte();
            NoMount = reader.ReadBoolean();
            NeedBridle = reader.ReadBoolean();
            NoFight = reader.ReadBoolean();
            Music = reader.ReadUInt16();

            if (Env.LoadVersion < 78) return;
            NoTownTeleport = reader.ReadBoolean();
            if (Env.LoadVersion < 79) return;
            NoReincarnation = reader.ReadBoolean();

            if (Env.LoadVersion >= 110)
            {
                WeatherParticles = (WeatherSetting)reader.ReadUInt16();
            }

            if (Env.LoadVersion >= 111)
            {
                GT = reader.ReadBoolean();
                GTIndex = reader.ReadByte();
            }
            if (Env.LoadVersion >= 114)
            {
                NoExperience = reader.ReadBoolean();
                NoGroup = reader.ReadBoolean();
                NoPets = reader.ReadBoolean();
                NoIntelligentCreatures = reader.ReadBoolean();
                NoHero = reader.ReadBoolean();
                RequiredGroupSize = reader.ReadInt32();
                RequiredGroup = reader.ReadBoolean();
                FireWallLimit = reader.ReadBoolean();
                FireWallCount = reader.ReadInt32();
            }
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(Index);
            writer.Write(FileName);
            writer.Write(Title);
            writer.Write(MiniMap);
            writer.Write((byte)Light);
            writer.Write(BigMap);
            writer.Write(SafeZones.Count);

            foreach (var zone in SafeZones)
                zone.Save(writer);

            writer.Write(Respawns.Count);
            foreach (var respawn in Respawns)
                respawn.Save(writer);

            writer.Write(Movements.Count);
            foreach (var movement in Movements)
                movement.Save(writer);

            writer.Write(NoTeleport);
            writer.Write(NoReconnect);
            writer.Write(NoReconnectMap);
            writer.Write(NoRandom);
            writer.Write(NoEscape);
            writer.Write(NoRecall);
            writer.Write(NoDrug);
            writer.Write(NoPosition);
            writer.Write(NoThrowItem);
            writer.Write(NoDropPlayer);
            writer.Write(NoDropMonster);
            writer.Write(NoNames);
            writer.Write(Fight);
            writer.Write(Fire);
            writer.Write(FireDamage);
            writer.Write(Lightning);
            writer.Write(LightningDamage);
            writer.Write(MapDarkLight);
            writer.Write(MineZones.Count);
            
            foreach (var mineZone in MineZones)
                mineZone.Save(writer);

            writer.Write(MineIndex);

            writer.Write(NoMount);
            writer.Write(NeedBridle);

            writer.Write(NoFight);

            writer.Write(Music);
            writer.Write(NoTownTeleport);
            writer.Write(NoReincarnation);

            writer.Write((ushort)WeatherParticles);

            writer.Write(GT);
            writer.Write(GTIndex);

            writer.Write(NoExperience);
            writer.Write(NoGroup);
            writer.Write(NoPets);
            writer.Write(NoIntelligentCreatures);
            writer.Write(NoHero);
            writer.Write(RequiredGroupSize);
            writer.Write(RequiredGroup);
            writer.Write(FireWallLimit);
            writer.Write(FireWallCount);

        }


        public void CreateMap()
        {
            foreach (var npcInfo in Env.NPCInfoList.Where(npcInfo => npcInfo.MapIndex == Index))
            {
                NPCs.Add(npcInfo);
            }

            Map map = new Map(this);

            if (!map.Load()) return;

            Env.MapList.Add(map);

            foreach (var safeZone in SafeZones.Where(safeZone => safeZone.StartPoint))
                Env.StartPoints.Add(safeZone);
        }

        public void CreateSafeZone()
        {
            SafeZones.Add(new SafeZoneInfo { Info = this });
        }

        public void CreateRespawnInfo()
        {
            Respawns.Add(new RespawnInfo { RespawnIndex = ++EditEnv.RespawnIndex });
        }

        public override string ToString()
        {
            return $"{Index}: {Title}";
        }

        public void CreateNPCInfo()
        {
            NPCs.Add(new NPCInfo());
        }

        public void CreateMovementInfo()
        {
            Movements.Add(new MovementInfo());
        }

        public static void FromText(string text)
        {
            string[] data = text.Split([','], StringSplitOptions.RemoveEmptyEntries);

            if (data.Length < 8) return;

            MapInfo info = new MapInfo {FileName = data[0], Title = data[1]};


            if (!ushort.TryParse(data[2], out info.MiniMap)) return;

            if (!Enum.TryParse(data[3], out info.Light)) return;

            if (!int.TryParse(data[4], out int sziCount)) return;
            if (!int.TryParse(data[5], out int miCount)) return;
            if (!int.TryParse(data[6], out int riCount)) return;
            if (!int.TryParse(data[7], out int npcCount)) return;


            int start = 8;

            for (int i = 0; i < sziCount; i++)
            {
                SafeZoneInfo temp = new SafeZoneInfo { Info = info };

                if (!int.TryParse(data[start + (i * 4)], out int x)) return;
                if (!int.TryParse(data[start + 1 + (i * 4)], out int y)) return;
                if (!ushort.TryParse(data[start + 2 + (i * 4)], out temp.Size)) return;
                if (!bool.TryParse(data[start + 3 + (i * 4)], out temp.StartPoint)) return;

                temp.Location = new Point(x, y);
                info.SafeZones.Add(temp);
            }
            start += sziCount * 4;



            for (int i = 0; i < miCount; i++)
            {
                MovementInfo temp = new MovementInfo();

                if (!int.TryParse(data[start + (i * 5)], out int x)) return;
                if (!int.TryParse(data[start + 1 + (i * 5)], out int y)) return;
                temp.Source = new Point(x, y);

                if (!int.TryParse(data[start + 2 + (i * 5)], out temp.MapIndex)) return;

                if (!int.TryParse(data[start + 3 + (i * 5)], out x)) return;
                if (!int.TryParse(data[start + 4 + (i * 5)], out y)) return;
                temp.Destination = new Point(x, y);

                info.Movements.Add(temp);
            }
            start += miCount * 5;


            for (int i = 0; i < riCount; i++)
            {
                RespawnInfo temp = new RespawnInfo();

                if (!int.TryParse(data[start + (i * 7)], out temp.MonsterIndex)) return;
                if (!int.TryParse(data[start + 1 + (i * 7)], out int x)) return;
                if (!int.TryParse(data[start + 2 + (i * 7)], out int y)) return;

                temp.Location = new Point(x, y);

                if (!ushort.TryParse(data[start + 3 + (i * 7)], out temp.Count)) return;
                if (!ushort.TryParse(data[start + 4 + (i * 7)], out temp.Spread)) return;
                if (!ushort.TryParse(data[start + 5 + (i * 7)], out temp.Delay)) return;
                if (!byte.TryParse(data[start + 6 + (i * 7)], out temp.Direction)) return;
                if (!int.TryParse(data[start + 7 + (i * 7)], out temp.RespawnIndex)) return;
                if (!bool.TryParse(data[start + 8 + (i * 7)], out temp.SaveRespawnTime)) return;
                if (!ushort.TryParse(data[start + 9 + (i * 7)], out temp.RespawnTicks)) return;

                info.Respawns.Add(temp);
            }
            start += riCount * 7;


            for (int i = 0; i < npcCount; i++)
            {

                if (!int.TryParse(data[start + 2 + (i * 6)], out int x)) return;
                if (!int.TryParse(data[start + 3 + (i * 6)], out int y)) return;
                if (!ushort.TryParse(data[start + 4 + (i * 6)], out ushort Rate)) return;
                if (!ushort.TryParse(data[start + 5 + (i * 6)], out ushort Image)) return;

                NPCInfo temp = new NPCInfo
                {
                    FileName = data[start + (i * 6)], Name = data[start + 1 + (i * 6)],
                    Location = new Point(x, y),
                    Rate = Rate,
                    Image = Image,
                };

                info.NPCs.Add(temp);
            }

            info.Index = ++EditEnv.MapIndex;
            EditEnv.MapInfoList.Add(info);
        }
        
        public static string GetMapTitleByIndex(int index) // For Players Online tab
        {
            var mapInfo = Env.MapInfoList.FirstOrDefault(m => m.Index == index);
            return mapInfo != null ? mapInfo.Title : $"UnknownMap({index})";
        }
    }
}
