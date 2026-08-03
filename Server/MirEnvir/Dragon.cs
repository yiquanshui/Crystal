using System.Drawing;
﻿using Server.MirDatabase;
using Server.MirObjects;
using Server.MirObjects.Monsters;

namespace Server.MirEnv
{
    public class Dragon
    {
        private readonly int ProcessDelay = 2000;
        public int DeLevelDelay = 60 * (60 * 1000);
        private long ProcessTime;
        public byte MaxLevel = Globals.MaxDragonLevel;
        private Rectangle DropArea;
        public long DeLevelTime;
        public bool Loaded;

        private static Env Env => Env.Main;

        protected static MessageQueue MessageQueue => MessageQueue.Instance;

        private readonly Point[] BodyLocations =
        [
            new Point(-3, -1),
            new Point(-3, -0),
            new Point(-2, -3),
            new Point(-2, -2),
            new Point(-2, -1),
            new Point(-2, 0),
            new Point(-2, 1),
            new Point(-1, -2),
            new Point(-1, -1),
            new Point(-1, 0),
            new Point(-1, 1),
            new Point(-1, 2),
            new Point(0, -2),
            new Point(0, -1),
            new Point(0, 1),
            new Point(0, 2),
            new Point(0, 3),
            new Point(1, -2),
            new Point(1, 0),
            new Point(1, 1),
            new Point(1, 2),
            new Point(1, 3),
            new Point(2, 1),
            new Point(2, 2)
        ];


        public DragonInfo Info;
        public MonsterObject? LinkedMonster;

        public Dragon(DragonInfo info)
        {
            Info = info;
        }
        public bool Load()
        {
            try
            {
                MonsterInfo? info = Env.GetMonsterInfo(Info.MonsterName);
                if (info == null)
                {
                    MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.FailedLoadDragonBadMonsterName) + Info.MonsterName);
                    return false;
                }
                LinkedMonster = MonsterObject.GetMonster(info);

                Map? map = Env.GetMapByNameAndInstance(Info.MapFileName);
                if (map == null)
                {
                    MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.FailedToLoadDragonBadMapName) + Info.MapFileName);
                    return false;
                }

                if (Info.Location.X > map.Width || Info.Location.Y > map.Height)
                {
                    MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.FailedToLoadDragonBadMapXY) + Info.MapFileName);
                    return false;
                }

                if (LinkedMonster.Spawn(map, Info.Location))
                {
                    if (LinkedMonster is EvilMir mob)
                    {
                        mob.DragonLink = true;
                    }
                    MonsterInfo? bodyInfo = Env.GetMonsterInfo(Info.BodyName);
                    if (bodyInfo != null)
                    {
                        Point spawnLocation = Point.Empty;
                        for (int i = 0; i <= BodyLocations.Length - 1; i++)
                        {
                            var bodyMob = MonsterObject.GetMonster(bodyInfo);
                            spawnLocation = new Point(LinkedMonster.CurrentLocation.X + BodyLocations[i].X, LinkedMonster.CurrentLocation.Y + BodyLocations[i].Y);
                            bodyMob.Spawn(LinkedMonster.CurrentMap!, spawnLocation);
                        }
                    }

                    DropArea = new Rectangle(Info.DropAreaTop.X, Info.DropAreaTop.Y, Info.DropAreaBottom.X - Info.DropAreaTop.X, Info.DropAreaBottom.Y - Info.DropAreaTop.Y);
                    Loaded = true;
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageQueue.Enqueue(ex);
            }

            MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.FailedToLoadDragon));
            return false;
        }
        
        public void GainExp(int amount)
        {
            if (amount <= 0) return;

            Info.Experience += amount;
            if (Info.Experience >= Info.Exps[Math.Min(11, Info.Level - 1)])
            {
                Info.Experience -= Info.Exps[Math.Min(11, Info.Level - 1)];
                LevelUp();
            }
        }
        
        public void LevelUp()
        {
            Drop(Info.Level);//i would suggest having the max level drop be empty or 'trash' > that way you stop ppl from exploiting it
            if (Info.Level < Globals.MaxDragonLevel) Info.Level = (byte)(Math.Max(1, (Info.Level + 1)));
            //if it reaches max level > make it stay that level for 6*deleveldelay and then reset to 0, rather then letting ppl farm it by making it drop every hour
            if (Info.Level == Globals.MaxDragonLevel)
                DeLevelTime = Env.Time + (6 * DeLevelDelay);
        }
        
        public void LevelDown()
        {
            if (Info.Level > 1)
            {
                Info.Level = (byte)(Math.Max(1, (Info.Level - 1)));
                Info.Experience = 0;
            }
        }
        
        public void Drop(byte level)
        {
            if (level > Info.Drops.Length) return;
            if (Info.Drops[level - 1] == null) return;
            
            if (LinkedMonster == null) return;
            List<DragonInfo.DropInfo> droplist = new List<DragonInfo.DropInfo>(Info.Drops[level - 1]!);

            foreach (var drop in droplist)
            {
                int rate = (int)(drop.Chance / Settings.DropRate); if (rate < 1) rate = 1;
                if (RandomProvider.Next(rate) != 0) continue;

                if (drop.Gold > 0)
                {
                    int gold = RandomProvider.Next((int)(drop.Gold / 2), (int)(drop.Gold + drop.Gold / 2)); //Messy

                    if (gold <= 0) continue;

                    if (!DropGold((uint)gold)) return;
                }
                else
                {
                    UserItem? item = Env.CreateDropItem(drop.Item);
                    if (item == null) continue;
                    if (!DropItem(item)) return;
                }
            }
        }
        
        protected bool DropItem(UserItem item)
        {
            if (LinkedMonster == null) throw new InvalidOperationException("LinkedMonster is null");
            Point dropLocation = new Point(DropArea.Left + (DropArea.Width / 2), DropArea.Top);
            ItemObject ob = new ItemObject(this.LinkedMonster, item, dropLocation)
            {
                Owner = this.LinkedMonster.EXPOwner,
                OwnerTime = Env.Time + Settings.Minute,
            };

            return ob.DragonDrop(DropArea.Width / 2);
        }

        protected bool DropGold(uint gold)
        {
            if (LinkedMonster == null) throw new InvalidOperationException("LinkedMonster is null");
            if (this.LinkedMonster.EXPOwner != null && this.LinkedMonster.EXPOwner.CanGainGold(gold))
            {
                this.LinkedMonster.EXPOwner.WinGold(gold);
                return true;
            }

            Point dropLocation = new Point(DropArea.Left + (DropArea.Width / 2), DropArea.Top);
            ItemObject ob = new ItemObject(this.LinkedMonster, gold, dropLocation)
            {
                Owner = this.LinkedMonster.EXPOwner,
                OwnerTime = Env.Time + Settings.Minute,
            };

            return ob.DragonDrop(DropArea.Width / 2);
        }

        public void Process()
        {
            if (!Loaded) return;
            if (Env.Time < ProcessTime) return;

            ProcessTime = Env.Time + ProcessDelay;

            if ((Info.Level >= Globals.MaxDragonLevel) && (Env.Time > DeLevelTime))
            {
                Info.Level = (byte)1;
                Info.Experience = 0;
                DeLevelTime = Env.Time + DeLevelDelay;
            }

            if (Info.Level > 1 && Env.Time > DeLevelTime)
            {
                LevelDown();
                DeLevelTime = Env.Time + DeLevelDelay;
            }
        }
    }
}