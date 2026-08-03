using System.Drawing;
﻿using Server.MirEnv;
using Server.MirObjects;

namespace Server.MirDatabase
{
    public class GuildInfo
    {
        public int GuildIndex;
        public string Name;
        public byte Level;
        public byte SparePoints;
        public long Experience;
        public uint Gold;

        public int Votes;
        public DateTime LastVoteAttempt;
        public bool Voting;

        public int Membercount;
        public List<GuildRank> Ranks = [];
        public GuildStorageItem?[] StoredItems = new GuildStorageItem[112];
        public List<GuildBuff> BuffList = [];
        public List<string> Notice = [];

        public long MaxExperience;
        public int MemberCap;

        public ushort FlagImage = 1000;
        public Color FlagColour = Color.White;

        public bool NeedSave;

        public DateTime GTRent = DateTime.MinValue;
        public DateTime GTBegin = DateTime.MinValue;
        public int GTIndex = -1;
        public int GTKey;
        public int GTPrice;
        protected static Env Env => Env.Main;

        public bool HasGT => GTRent > DateTime.Now;


        public GuildInfo(PlayerObject owner, string name)
        {
            Name = name;

            var ownerRank = new GuildRank { Name = "Leader", Options = (GuildRankOptions)255, Index = 0 };
            var leader = new GuildMember { Name = owner.CharacterInfo.Name, Player = owner, Id = owner.CharacterInfo.Index, LastLogin = Env.Now, Online = true };

            ownerRank.Members.Add(leader);
            Ranks.Add(ownerRank);

            Membercount++;
            NeedSave = true;

            if (Level < Settings.Guild_ExperienceList.Count)
            {
                MaxExperience = Settings.Guild_ExperienceList[Level];
            }

            if (Name == Settings.NewbieGuild)
            {
                MemberCap = Settings.NewbieGuildMaxSize;
                Level = 21;
            }
            else if(Level < Settings.Guild_MembercapList.Count)
            {
                MemberCap = Settings.Guild_MembercapList[Level];
            }

            FlagColour = Color.FromArgb(255, RandomProvider.Next(255), RandomProvider.Next(255), RandomProvider.Next(255));
        }

        public GuildInfo(BinaryReader reader)
        {
            int customVersion = Env.LoadCustomVersion;
            int version = reader.ReadInt32();
            GuildIndex = version;

            if (version == int.MaxValue)
            {
                version = reader.ReadInt32();
                customVersion = reader.ReadInt32();
                GuildIndex = reader.ReadInt32();
            }
            else
            {
                version = Env.LoadVersion;
                NeedSave = true;
            }

            Name = reader.ReadString();
            Level = reader.ReadByte();
            SparePoints = reader.ReadByte();
            Experience = reader.ReadInt64();
            Gold = reader.ReadUInt32();
            Votes = reader.ReadInt32();
            LastVoteAttempt = DateTime.FromBinary(reader.ReadInt64());
            Voting = reader.ReadBoolean();

            int rankCount = reader.ReadInt32();
            Membercount = 0;

            for (int i = 0; i < rankCount; i++)
            {
                Ranks.Add(new GuildRank(reader, true) { Index = i });
                Membercount += Ranks[i]!.Members.Count;
            }

            int itemCount = reader.ReadInt32();
            for (int j = 0; j < itemCount; j++)
            {
                if (!reader.ReadBoolean()) continue;

                GuildStorageItem guildItem = new GuildStorageItem()
                {
                    Item = new UserItem(reader, version, customVersion),
                    UserId = reader.ReadInt64()
                };

                if (Env.BindItem(guildItem.Item) && j < StoredItems.Length)
                    StoredItems[j] = guildItem;
            }

            int buffCount = reader.ReadInt32();
            if (version < 61)
            {
                for (int j = 0; j < buffCount; j++)
                    _ = new GuildBuffOld(reader);
            }
            else
            {
                for (int j = 0; j < buffCount; j++)
                {
                    //new GuildBuff(reader);
                    BuffList.Add(new GuildBuff(reader));
                }
            }

            foreach (var buff in BuffList)
            {
                buff.Info = Env.FindGuildBuffInfo(buff.Id);
            }

            int noticeCount = reader.ReadInt32();
            for (int j = 0; j < noticeCount; j++)
            {
                Notice.Add(reader.ReadString());
            }

            if (Level < Settings.Guild_ExperienceList.Count)
            {
                MaxExperience = Settings.Guild_ExperienceList[Level];
            }

            if (Name == Settings.NewbieGuild)
            {
                MemberCap = Settings.NewbieGuildMaxSize;
            }
            else if (Level < Settings.Guild_MembercapList.Count)
            {
                MemberCap = Settings.Guild_MembercapList[Level];
            }

            if (version > 72)
            {
                FlagImage = reader.ReadUInt16();
                FlagColour = Color.FromArgb(reader.ReadInt32());
            }

            if (version > 110)
            {
                GTRent = DateTime.FromBinary(reader.ReadInt64());
                GTIndex = reader.ReadInt32();
                GTKey = reader.ReadInt32();
                GTPrice = reader.ReadInt32();
                GTBegin = DateTime.FromBinary(reader.ReadInt64());
            }
        }

        public void Save(BinaryWriter writer)
        {
            int temp = int.MaxValue;
            writer.Write(temp);
            writer.Write(Env.Version);
            writer.Write(Env.CustomVersion);

            int rankCount = 0;
            for (int i = Ranks.Count - 1; i >= 0; i--)
            {
                if (Ranks[i].Members.Count > 0)
                {
                    rankCount++;
                }
            }

            writer.Write(GuildIndex);
            writer.Write(Name);
            writer.Write(Level);
            writer.Write(SparePoints);
            writer.Write(Experience);
            writer.Write(Gold);
            writer.Write(Votes);
            writer.Write(LastVoteAttempt.ToBinary());
            writer.Write(Voting);

            writer.Write(rankCount);
            foreach (var rank in Ranks)
            {
                if (rank.Members.Count > 0)
                {
                    rank.Save(writer, true);
                }
            }

            writer.Write(StoredItems.Length);
            foreach (var item in StoredItems)
            {
                writer.Write(item != null);
                if (item != null)
                {
                    item.Item.Save(writer);
                    writer.Write(item.UserId);
                }
            }

            writer.Write(BuffList.Count);
            foreach (var buff in BuffList)
            {
                buff.Save(writer);
            }

            writer.Write(Notice.Count);
            foreach (string notice in Notice)
            {
                writer.Write(notice);
            }

            writer.Write(FlagImage);
            writer.Write(FlagColour.ToArgb());

            writer.Write(GTRent.ToBinary());
            writer.Write(GTIndex);
            writer.Write(GTKey);
            writer.Write(GTPrice);
            writer.Write(GTBegin.ToBinary());
        }
    }
}
