using System;
using System.Drawing;
﻿using Server.MirDatabase;
using Server.MirEnv;
using Server.MirObjects.Monsters;
using System.Diagnostics.Eventing.Reader;
using Shared;
using S = ServerPackets;

namespace Server.MirObjects
{
    public class MonsterObject : MapObject
    {
        public static MonsterObject GetMonster(MonsterInfo info)
        {
            return info.AI switch
            {
                1 or 2 => new Deer(info),
                3 => new Tree(info),
                4 =>
                    //Common AI: 1 Line Attack with Poison
                    new SpittingSpider(info),
                5 => new CannibalPlant(info),
                6 => new Guard(info),
                7 => new CaveMaggot(info),
                8 =>
                    //Common AI: 1 Range Projectile Attack with Fear
                    new AxeSkeleton(info),
                9 => new HarvestMonster(info),
                10 =>
                    //Common AI: 1 Magic Attack
                    new FlamingWooma(info),
                11 => new WoomaTaurus(info),
                12 => new BugBagMaggot(info),
                13 => new RedMoonEvil(info),
                14 => new EvilCentipede(info),
                15 => new ZumaMonster(info),
                16 => new RedThunderZuma(info),
                17 => new ZumaTaurus(info),
                18 => new Shinsu(info),
                19 => new KingScorpion(info),
                20 => new DarkDevil(info),
                21 => new IncarnatedGhoul(info),
                22 => new IncarnatedZT(info),
                23 => new BoneFamiliar(info),
                24 => new DigOutZombie(info),
                25 => new RevivingZombie(info),
                26 => new ShamanZombie(info),
                27 => new Khazard(info),
                28 => new ToxicGhoul(info),
                29 =>
                    //Common AI: 1 Line Attack
                    new BoneSpearman(info),
                30 => new BoneLord(info),
                31 =>
                    //Common AI: 2 Magic Attacks, 1 Close, 1 Range
                    new RightGuard(info),
                32 =>
                    //Common AI: 2 Magic Attacks, 1 Close, 1 Range Projectile
                    new LeftGuard(info),
                33 => new MinotaurKing(info),
                34 => new FrostTiger(info) //Effect 0/1
                ,
                35 =>
                    //Common AI: 1 Line Attack
                    new SandWorm(info),
                36 => new Yimoogi(info),
                37 => new CrystalSpider(info),
                38 => new HolyDeva(info),
                39 => new RootSpider(info),
                40 => new BombSpider(info),
                41 or 42 => new YinDevilNode(info),
                43 => new OmaKing(info),
                44 =>
                    //Common AI: 2 Attacks, 1 Close, 1 Line Attack
                    new BlackFoxman(info),
                45 => new RedFoxman(info),
                46 => new WhiteFoxman(info),
                47 => new TrapRock(info),
                48 => new GuardianRock(info),
                49 => new ThunderElement(info),
                50 => new GreatFoxSpirit(info),
                51 =>
                    //Common AI: 2 Physical Attacks, 1 Close, 1 Range
                    new HedgeKekTal(info),
                52 => new EvilMir(info),
                53 => new EvilMirBody(info),
                54 => new DragonStatue(info),
                55 => new HumanWizard(info),
                56 => new Trainer(info),
                57 => new TownArcher(info),
                58 => new Guard(info),
                59 => new HumanAssassin(info),
                60 => new VampireSpider(info) //TODO - Clean up
                ,
                61 => new SpittingToad(info),
                62 => new SnakeTotem(info),
                63 => new CharmedSnake(info),
                64 => new IntelligentCreatureObject(info),
                65 =>
                    //Common AI: 2 Close attacks with WeakerTeleport
                    new MutatedManworm(info),
                66 =>
                    //Common AI: 2 Close Attacks
                    new CrazyManworm(info),
                67 => new DarkDevourer(info),
                68 => new Football(info),
                69 => new PoisonHugger(info),
                70 => new Hugger(info),
                71 => new Behemoth(info),
                72 => new FinialTurtle(info),
                73 => new TurtleKing(info),
                74 => new LightTurtle(info),
                75 => new WitchDoctor(info),
                76 =>
                    //Common AI: 2 Close Attacks, 1 Normal, 1 Halfmoon
                    new HellSlasher(info),
                77 =>
                    //Common AI: 2 Close Attacks, 1 Normal, 1 Fullmoon
                    new HellPirate(info),
                78 => new HellCannibal(info),
                79 => new HellKeeper(info),
                80 => new ConquestArcher(info),
                81 => new Gate(info),
                82 => new Wall(info),
                83 => new Tornado(info),
                84 => new WingedTigerLord(info),
                85 => new FlamingMutant(info),
                86 => new ManectricClaw(info),
                87 => new ManectricBlest(info),
                88 => new ManectricKing(info),
                89 => new IcePillar(info),
                90 => new TrollBomber(info),
                91 => new TrollKing(info),
                92 =>
                    //Common AI: 2 Attacks with Fear, 1 Normal, 1 Long Line
                    new FlameSpear(info),
                93 =>
                    //Common AI: 2 Magic Attacks with Fear, 1 Close, 1 Range AOE
                    new FlameMage(info),
                94 =>
                    //Common AI: 2 Magic Attacks with Fear, 1 Close, 1 Close AOE
                    new FlameScythe(info),
                95 => new FlameAssassin(info),
                96 => new FlameQueen(info),
                97 => new HellKnight(info),
                98 => new HellLord(info),
                99 => new HellBomb(info),
                100 =>
                    //Common AI: 1 Magic Line Attack with Poison
                    new VenomSpider(info),
                101 => new AncientBringer(info),
                102 => new IceGuard(info),
                103 => new ElementGuard(info),
                104 => new DemonGuard(info),
                105 => new KingGuard(info),
                106 => new DeathCrawler(info),
                107 =>
                    //Common AI: 2 Magic Attacks with Rush, 1 Close, 1 Range
                    new BurningZombie(info),
                108 => new MudZombie(info),
                109 => new HardenRhino(info),
                110 => new DemonWolf(info) //Effect 0/1
                ,
                111 => new WhiteMammoth(info),
                112 =>
                    //Common AI: 2 Close attacks
                    new DarkBeast(info) //Effect 0/1
                ,
                113 => new ArcherGuard(info),
                114 =>
                    //Common AI: 1 Close attack with WeakerTeleport
                    new Mandrill(info),
                115 => new SandSnail(info),
                116 => new BlackHammerCat(info),
                117 => new StrayCat(info),
                118 => new CatShaman(info),
                119 => new Jar1(info),
                120 => new Jar2(info),
                121 => new SeedingsGeneral(info),
                122 => new RestlessJar(info),
                123 => new GeneralMeowMeow(info),
                124 => new Armadillo(info),
                125 => new ArmadilloElder(info),
                126 => new TucsonMage(info),
                127 => new TucsonWarrior(info),
                128 => new TucsonEgg(info) //Effect 0/1
                ,
                129 => new SwampWarrior(info),
                130 => new CannibalTentacles(info),
                131 => new TucsonGeneral(info),
                132 => new GasToad(info),
                133 => new Mantis(info),
                134 => new AssassinBird(info),
                135 => new StoningStatue(info),
                136 => new FlyingStatue(info),
                137 => new RhinoPriest(info),
                138 => new ElephantMan(info),
                139 => new StoneGolem(info),
                140 => new EarthGolem(info),
                141 => new TreeGuardian(info),
                142 => new TreeQueen(info),
                143 => new PeacockSpider(info),
                144 => new OmaCannibal(info),
                145 =>
                    //Common AI: 2 Attacks, 1 Close, 1 Close AOE
                    new OmaBlest(info),
                146 =>
                    //Common AI: 1 Halfmoon Attack
                    new OmaSlasher(info),
                147 => new OmaMage(info),
                148 => new OmaWitchDoctor(info),
                149 => new PowerBead(info) //Effect 0/1/2
                ,
                150 => new DarkOmaKing(info),
                151 => new CaveStatue(info),
                152 => new PlagueCrab(info),
                153 => new CreeperPlant(info),
                154 => new Nadz(info),
                155 => new AvengingSpirit(info),
                156 => new AvengingWarrior(info),
                157 => new AxePlant(info),
                158 =>
                    //Common AI: None With Attack On Death
                    new WoodBox(info),
                159 => new DarkCaptain(info),
                160 =>
                    //Common AI: 1 Range Attack with Fear
                    new BlueSoul(info),
                161 => new SackWarrior(info),
                162 => new KingHydrax(info),
                163 => new HornedMage(info),
                164 => new HornedArcher(info) //Effect 0/1
                ,
                165 => new HornedWarrior(info),
                166 => new FloatingRock(info),
                167 => new ScalyBeast(info),
                168 => new WereTiger(info),
                169 => new HornedSorceror(info),
                170 => new BoulderSpirit(info),
                171 => new HornedCommander(info),
                //case 172: MoonSunLightningStone
                173 => new TurtleGrass(info),
                174 => new ManTree(info),
                175 => new ChieftainArcher(info),
                //case 176: ChieftainSword
                177 => new FrozenKnight(info),
                178 => new IcePhantom(info) //TODO
                ,
                179 => new SnowWolf(info),
                180 => new SnowWolfKing(info),
                181 => new WaterDragon(info),
                182 => new BlackTortoise(info),
                //case 183: Manticore
                184 => new DragonWarrior(info) //TODO
                ,
                //case 185: DragonArcher
                186 => new Kirin(info),
                187 => new FrozenMiner(info),
                188 => new FrozenAxeman(info),
                189 => new FrozenMagician(info),
                190 => new SnowYeti(info),
                191 => new IceCrystalSoldier(info),
                192 => new DarkWraith(info),
                //case 193: CrystalBeast
                //case 194: RedOrb
                //case 195: FatalLotus
                196 => new AntCommander(info),
                
                197 => new GlacierSnail(info),
                198 => new FurbolgWarrior(info),
                199 => new FurbolgArcher(info),
                200 => new FurbolgCommander(info),
                201 => new FurbolgGuard(info),
                202 => new GlacierBeast(info),
                203 => new GlacierWarrior(info),
                210 => new HoodedSummonerScrolls(info),
                211 => new HoodedSummoner(info),
                212 => new PurpleFaeFlower(info),
                213 => new Siege(info) //TODO
                ,
                214 => new SepWarrior(info) //TODO
                ,
                215 => new SepWizard(info) //TODO
                ,
                216 => new SepTaoist(info) //TODO
                ,
                217 => new SepAssassin(info) //TODO
                ,
                218 => new SepArcher(info) //TODO
                ,
                219 => new SepHighWarrior(info) //TODO
                ,
                220 => new SepHighWizard(info) //TODO
                ,
                221 => new SepHighTaoist(info) //TODO
                ,
                222 => new SepHighAssassin(info) //TODO
                ,
                223 => new SepHighArcher(info) //TODO
                ,
                255 => //Skill 
                    new StoneTrap(info),
                _ => new MonsterObject(info)
            };
        }

        public override ObjectType Race => ObjectType.Monster;

        public virtual bool IgnoresNoPetRestriction => false;
        
        public readonly MonsterInfo Info;
        public MapRespawn? Respawn;
        public MonsterType MonsterType { get; private set; } = MonsterType.Normal;
        private long NextRecallTime;

        public override string Name
        {
            get => Master == null ? Info.GameName : $"{Info.GameName}({Master.Name})";
            set => throw new NotSupportedException();
        }

        public override int CurrentMapIndex { get; protected set; }
        public override Point CurrentLocation { get; set; }
        
        public sealed override MirDirection Direction { get; set; }
        public override ushort Level
        {
            get => Info.Level;
            set => throw new NotSupportedException();
        }

        public sealed override AttackMode AMode
        {
            get => base.AMode;
            set => base.AMode = value;
        }
        
        public sealed override PetMode PMode
        {
            get => base.PMode;
            set => base.PMode = value;
        }

        public override int Health => HP;

        public override int MaxHealth => Stats[Stat.HP];

        public int HealthPercent => (Health * 100) / MaxHealth;

        public int HP;

        public ushort MoveSpeed;

        public virtual uint Experience
        {
            get
            {
                MonsterRarityProfile profile = GetRarityProfile();
                double scaled = Info.Experience * profile.ExpMultiplier;

                if (scaled <= 0)
                {
                    return 0;
                }
                
                if (scaled > uint.MaxValue) return uint.MaxValue;

                return (uint)Math.Round(scaled);
            }
        }
        
        public int DeadDelay
        {
            get
            {
                return Info.AI switch
                {
                    64 => 0,
                    81 or 82 => int.MaxValue,
                    252 => 5000,
                    _ => 180000
                };
            }
        }
        public const int RegenDelay = 10000, EXPOwnerDelay = 5000, AloneDelay = 3000, SearchDelay = 3000, RoamDelay = 1000, HealDelay = 600, RevivalDelay = 2000;
        public long ActionTime, MoveTime, AttackTime, RegenTime, DeadTime, AloneTime, SearchTime, RoamTime, HealTime;
        public long ShockTime, RageTime, HallucinationTime;
        public bool BindingShotCenter, PoisonStopRegen = true;

        protected bool Alone = false, Stacking = false;

        public byte PetLevel;
        public uint PetExperience;
        public byte MaxPetLevel;
        public long TameTime;
        public bool DieNextTurn;

        public int RoutePoint;
        public bool Waiting;
        public bool GMMade;
        public bool Frozen;

        public readonly List<MonsterObject> SlaveList = [];
        public readonly List<RouteInfo> Route = [];

        public override bool Blocking => !Dead;

        protected virtual bool CanRegen => Env.Time >= RegenTime;

        protected virtual bool CanMove =>
            !Dead && 
            Env.Time > MoveTime && 
            Env.Time > ActionTime && 
            Env.Time > ShockTime &&
            (Master == null || Master.PMode == PetMode.MoveOnly || Master.PMode == PetMode.Both || Master.PMode == PetMode.FocusMasterTarget) && 
            !CurrentPoison.HasFlag(PoisonType.Paralysis) && 
            !CurrentPoison.HasFlag(PoisonType.LRParalysis) &&
            !CurrentPoison.HasFlag(PoisonType.Frozen) &&
            (!CurrentPoison.HasFlag(PoisonType.Stun) || (Info.Light == 10 || Info.Light == 5));

        protected virtual bool CanAttack =>
            !Dead &&
            Env.Time > AttackTime &&
            Env.Time > ActionTime &&
            (Master == null || Master.PMode == PetMode.AttackOnly || Master.PMode == PetMode.Both || Master.PMode == PetMode.FocusMasterTarget) &&
            !CurrentPoison.HasFlag(PoisonType.Paralysis) &&
            !CurrentPoison.HasFlag(PoisonType.LRParalysis) &&
            !CurrentPoison.HasFlag(PoisonType.Dazed) &&
            !CurrentPoison.HasFlag(PoisonType.Frozen) &&
            (!CurrentPoison.HasFlag(PoisonType.Stun) || (Info.Light == 10 || Info.Light == 5));


        protected internal MonsterObject(MonsterInfo info)
        {
            Info = info;

            Undead = Info.Undead;
            AutoRev = info.AutoRev;
            CoolEye = info.CoolEye > RandomProvider.Next(100);
            Direction = (MirDirection)RandomProvider.Next(8);

            AMode = AttackMode.All;
            PMode = PetMode.Both;

            RegenTime = RandomProvider.Next(RegenDelay) + Env.Time;
            SearchTime = RandomProvider.Next(SearchDelay) + Env.Time;
            RoamTime = RandomProvider.Next(RoamDelay) + Env.Time;
        }

        public void SetMonsterType(MonsterType type)
        {
            MonsterType = type;
        }
        
        public bool Spawn(Map temp, Point location)
        {
            if (!temp.ValidPoint(location)) return false;

            CurrentMap = temp;
            CurrentLocation = location;

            CurrentMap.AddObject(this);

            RefreshAll();
            SetHP(Stats[Stat.HP]);

            Spawned();
            Env.MonsterCount++;
            CurrentMap.MonsterCount++;
            return true;
        }
        
        public bool Spawn(MapRespawn respawn)
        {
            Respawn = respawn;

            if (respawn.Map == null) return false;
            if (Respawn.WalkableCells == null || Respawn.WalkableCells.Count == 0) return false;

            var spawnPoint = Respawn.WalkableCells[RandomProvider.Next(Respawn.WalkableCells.Count)];

            CurrentLocation = spawnPoint;

            respawn.Map.AddObject(this);

            CurrentMap = respawn.Map;

            if (Respawn.Route.Count > 0)
                Route.AddRange(Respawn.Route);

            RefreshAll();
            SetHP(Stats[Stat.HP]);

            Spawned();
            Respawn.Count++;
            respawn.Map.MonsterCount++;
            Env.MonsterCount++;
            return true;
        }

        public override void Spawned()
        {
            ActionTime = Env.Time + 2000;

            if (Info.HasSpawnScript && (Env.MonsterNPC != null))
            {
                Env.MonsterNPC.Call(this, $"[@_SPAWN({Info.Index})]");
            }

            base.Spawned();
        }

        protected virtual void RefreshBase()
        {
            Stats.Clear();

            Stats.Add(Info.Stats);
            ApplyMonsterTypeBonuses();

            MoveSpeed = Info.MoveSpeed;
            AttackSpeed = Info.AttackSpeed;
        }

        protected virtual void ApplyMonsterTypeBonuses()
        {
            if (MonsterType == MonsterType.Normal) return;

            MonsterRarityProfile profile = GetRarityProfile();

            ScaleStat(Stat.HP, profile.HpMultiplier);

            ScaleStat(Stat.MinAC, profile.DefenseMultiplier);
            ScaleStat(Stat.MaxAC, profile.DefenseMultiplier);
            ScaleStat(Stat.MinMAC, profile.DefenseMultiplier);
            ScaleStat(Stat.MaxMAC, profile.DefenseMultiplier);

            ScaleStat(Stat.MinDC, profile.DamageMultiplier);
            ScaleStat(Stat.MaxDC, profile.DamageMultiplier);
            ScaleStat(Stat.MinMC, profile.DamageMultiplier);
            ScaleStat(Stat.MaxMC, profile.DamageMultiplier);
            ScaleStat(Stat.MinSC, profile.DamageMultiplier);
            ScaleStat(Stat.MaxSC, profile.DamageMultiplier);
        }

        protected void ScaleStat(Stat stat, double multiplier)
        {
            if (Math.Abs(multiplier - 1D) < double.Epsilon) return;

            int value = Stats[stat];
            if (value == 0) return;

            double scaled = value * multiplier;

            if (stat == Stat.HP)
                Stats[stat] = Math.Max(1, (int)Math.Round(scaled));
            else
                Stats[stat] = (int)Math.Round(scaled);
        }

        protected MonsterRarityProfile GetRarityProfile()
        {
            return MonsterRarityData.GetProfile(MonsterType);
        }

        protected uint ApplyGoldModifier(uint amount)
        {
            if (amount == 0) return 0;

            MonsterRarityProfile profile = GetRarityProfile();
            double scaled = amount * profile.GoldMultiplier;

            if (scaled <= 0) return 0;
            if (scaled > uint.MaxValue) return uint.MaxValue;

            return (uint)Math.Round(scaled);
        }

        public virtual void RefreshAll()
        {
            RefreshBase();

            Stats[Stat.HP] += PetLevel * 20;
            Stats[Stat.MinAC] += PetLevel * 2;
            Stats[Stat.MaxAC] += PetLevel * 2;
            Stats[Stat.MinMAC] += PetLevel * 2;
            Stats[Stat.MaxMAC] += PetLevel * 2;
            Stats[Stat.MinDC] += PetLevel;
            Stats[Stat.MaxDC] += PetLevel;

            if (Info.Name == Settings.SkeletonName || Info.Name == Settings.ShinsuName || Info.Name == Settings.AngelName)
            {
                MoveSpeed = (ushort)Math.Min(ushort.MaxValue, (Math.Max(ushort.MinValue, MoveSpeed - MaxPetLevel * 130)));
                AttackSpeed = (ushort)Math.Min(ushort.MaxValue, (Math.Max(ushort.MinValue, AttackSpeed - MaxPetLevel * 70)));
            }

            if (MoveSpeed < 400) MoveSpeed = 400;
            if (AttackSpeed < 400) AttackSpeed = 400;

            RefreshBuffs();
        }

        protected virtual void RefreshBuffs()
        {
            for (int i = 0; i < Buffs.Count; i++)
            {
                Buff buff = Buffs[i];

                if (buff.Stats != null)
                {
                    Stats.Add(buff.Stats);
                }

                switch (buff.Type)
                {
                    case BuffType.SwiftFeet:
                        MoveSpeed = (ushort)Math.Max(ushort.MinValue, MoveSpeed + 100);
                        break;
                }
            }
        }
        
        public virtual void RefreshNameColour(bool send = true)
        {
            if (ShockTime < Env.Time) BindingShotCenter = false;

            Color colour = Color.White;

            if (Master != null)
            {
                colour = PetLevel switch
                {
                    1 => Color.Aqua,
                    2 => Color.Aquamarine,
                    3 => Color.LightSeaGreen,
                    4 => Color.SlateBlue,
                    5 => Color.SteelBlue,
                    6 => Color.Blue,
                    7 => Color.Navy,
                    _ => colour
                };
            }
            else if (MonsterType != MonsterType.Normal)
            {
                colour = GetRarityProfile().NameColour;
            }

            if (Env.Time < ShockTime)
                colour = Color.Peru;
            else if (Env.Time < RageTime)
                colour = Color.Red;
            else if (Env.Time < HallucinationTime)
                colour = Color.MediumOrchid;

            if (colour == NameColour || !send) return;

            NameColour = colour;

            Broadcast(new S.ObjectColourChanged { ObjectID = ObjectID, NameColour = NameColour });
        }

        public void SetHP(int amount)
        {
            if (HP == amount) return;

            HP = amount <= Stats[Stat.HP] ? amount : Stats[Stat.HP];

            if (!Dead && HP == 0) Die();

            //  HealthChanged = true;
            BroadcastHealthChange();
        }
        
        public virtual void ChangeHP(int amount)
        {
            if (HP + amount > Stats[Stat.HP])
                amount = Stats[Stat.HP] - HP;

            if (amount == 0) return;

            HP += amount;

            if (HP < 0) HP = 0;

            if (!Dead && HP == 0) Die();

            // HealthChanged = true;
            BroadcastHealthChange();
        }

        //use this so you can have mobs take no/reduced poison damage
        public virtual void PoisonDamage(int amount, MapObject? Attacker)
        {
            ChangeHP(amount);
        }


        public override bool Teleport(Map? temp, Point location, bool effects = true, byte effectnumber = 0)
        {
            if (temp == null || !temp.ValidPoint(location)) return false;

            CurrentMap!.RemoveObject(this);
            if (effects) Broadcast(new S.ObjectTeleportOut { ObjectID = ObjectID, Type = effectnumber });
            Broadcast(new S.ObjectRemove { ObjectID = ObjectID });

            CurrentMap.MonsterCount--;

            CurrentMap = temp;
            CurrentLocation = location;

            CurrentMap.MonsterCount++;

            InTrapRock = false;

            CurrentMap.AddObject(this);
            BroadcastInfo();

            if (effects) Broadcast(new S.ObjectTeleportIn { ObjectID = ObjectID, Type = effectnumber });

            BroadcastHealthChange();

            return true;
        }


        public override void Die()
        {
            if (Dead) return;

            HP = 0;
            Dead = true;

            DeadTime = Env.Time + DeadDelay;

            Broadcast(new S.ObjectDied { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });

            if (Info.HasDieScript && (Env.MonsterNPC != null))
            {
                Env.MonsterNPC.Call(this, $"[@_DIE({Info.Index})]");
            }

            if (EXPOwner is { Node: not null } && Master == null && EXPOwner.Race is ObjectType.Player or ObjectType.Hero)
            {
                EXPOwner.WinExp(Experience, Level);

                if (EXPOwner.Race != ObjectType.Hero)
                {
                    PlayerObject playerObj = (PlayerObject)EXPOwner;
                    playerObj.CheckGroupQuestKill(Info);
                }

            }

            if (Respawn != null)
                Respawn.Count--;

            if (Master == null && EXPOwner != null)
                Drop();

            Master = null;

            PoisonList.Clear();
            Env.MonsterCount--;
            
            if (CurrentMap != null)
                CurrentMap.MonsterCount--;
        }

        public MapObject GetAttacker(MapObject attacker)
        {
            return attacker switch
            {
                HeroObject hero => hero.Owner,
                _ => attacker
            };
        }

        public void Revive(int hp, bool effect)
        {
            if (!Dead) return;

            SetHP(hp);

            Dead = false;
            ActionTime = Env.Time + RevivalDelay;

            Broadcast(new S.ObjectRevived { ObjectID = ObjectID, Effect = effect });

            if (Respawn != null)
                Respawn.Count++;

            Env.MonsterCount++;

            if (CurrentMap != null) CurrentMap.MonsterCount++;
        }

        public override int Pushed(MapObject pusher, MirDirection dir, int distance)
        {
            if (!Info.CanPush || CurrentMap == null) return 0;
            //if (!CanMove) return 0; //stops mobs that can't move (like cannibalplants) from being pushed

            int result = 0;
            MirDirection reverse = Functions.ReverseDirection(dir);
            for (int i = 0; i < distance; i++)
            {
                Point location = Functions.PointMove(CurrentLocation, dir, 1);

                if (!CurrentMap.ValidPoint(location)) return result;

                Cell cell = CurrentMap.GetCell(location);

                if (cell.Objects.Any(ob => ob.Blocking))
                    break;

                CurrentMap.GetCell(CurrentLocation).Remove(this);

                Direction = reverse;
                RemoveObjects(dir, 1);
                CurrentLocation = location;
                CurrentMap.GetCell(CurrentLocation).Add(this);
                AddObjects(dir, 1);

                Broadcast(new S.ObjectPushed { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });

                result++;
            }

            ActionTime = Env.Time + 300 * result;
            MoveTime = Env.Time + 500 * result;

            if (result > 0)
            {
                Cell cell = CurrentMap.GetCell(CurrentLocation);

                foreach (var ob in cell.Objects.Where(cellObject => cellObject.Race == ObjectType.Spell).Cast<SpellObject>())
                {
                    ob.ProcessSpell(this);
                }
            }

            return result;
        }

        protected virtual void Drop()
        {
            if (CurrentMap is { Info.NoDropMonster: true })
                return;

            MonsterRarityProfile profile = GetRarityProfile();
            int ownerItemBonus = EXPOwner?.Stats[Stat.ItemDropRatePercent] ?? 0;
            int ownerGoldBonus = EXPOwner?.Stats[Stat.GoldDropRatePercent] ?? 0;
            int totalItemBonus = ownerItemBonus + profile.ItemDropBonusPercent;
            int totalGoldBonus = ownerGoldBonus + profile.GoldDropBonusPercent;

            for (int i = 0; i < Info.Drops.Count; i++)
            {
                DropInfo drop = Info.Drops[i];

                var reward = drop.AttemptDrop(totalItemBonus, totalGoldBonus);

                if (reward != null)
                {
                    uint scaledGold = ApplyGoldModifier(reward.Gold);

                    if (scaledGold > 0)
                    {
                        DropGold(scaledGold);
                    }

                    foreach (var item in reward.Items.Select(dropItem => Env.CreateDropItem(dropItem)).OfType<UserItem>())
                    {
                        if (GMMade)
                        {
                            item.GMMade = true;
                        }

                        if (EXPOwner is { Race: ObjectType.Player })
                        {
                            PlayerObject ob = (PlayerObject)EXPOwner;

                            if (ob.CheckGroupQuestItem(item))
                            {
                                continue;
                            }
                        }

                        if (drop.QuestRequired) continue;
                        if (!DropItem(item)) return;
                    }
                }
            }
        }

        protected virtual bool DropItem(UserItem item)
        {
            if (CurrentMap is { Info.NoDropMonster: true })
                return false;

            ItemObject ob = new ItemObject(this, item)
            {
                Owner = EXPOwner,
                OwnerTime = Env.Time + Settings.Minute,
            };

            if (!item.Info.GlobalDropNotify)
                return ob.Drop(Settings.DropRange);

            foreach (var player in Env.Players)
            {
                player.ReceiveChat(GameLanguage.ServerTextMap.GetLocalization((ServerTextKeys.PlayerHasDroppedItem), Name, item.FriendlyName), ChatType.System2);
            }

            return ob.Drop(Settings.DropRange);
        }

        protected virtual bool DropGold(uint gold)
        {
            if (EXPOwner != null && EXPOwner.CanGainGold(gold) && !Settings.DropGold)
            {
                EXPOwner.WinGold(gold);
                return true;
            }

            uint count = gold / Settings.MaxDropGold == 0 ? 1 : gold / Settings.MaxDropGold + 1;
            for (int i = 0; i < count; i++)
            {
                ItemObject ob = new ItemObject(this, i != count - 1 ? Settings.MaxDropGold : gold % Settings.MaxDropGold)
                {
                    Owner = EXPOwner,
                    OwnerTime = Env.Time + Settings.Minute,
                };

                ob.Drop(Settings.DropRange);
            }

            return true;
        }

        public override void Process()
        {
            base.Process();

            RefreshNameColour();

            if (Target != null && (Target.CurrentMap != CurrentMap || !Target.IsAttackTarget(this) || !Functions.InRange(CurrentLocation, Target.CurrentLocation, Globals.DataRange)))
                Target = null;

            for (int i = SlaveList.Count - 1; i >= 0; i--)
                if (SlaveList[i].Dead || SlaveList[i].Node == null)
                    SlaveList.RemoveAt(i);

            if (Dead && Env.Time >= DeadTime)
            {
                CurrentMap?.RemoveObject(this);
                if (Master != null)
                {
                    Master.Pets.Remove(this);
                    Master = null;
                }

                Despawn();
                return;
            }

            if (Master != null && TameTime > 0 && Env.Time >= TameTime)
            {
                Master.Pets.Remove(this);
                Master = null;
                Broadcast(new S.ObjectName { ObjectID = ObjectID, Name = Name });
            }

            ProcessAI();

            ProcessBuffs();
            ProcessRegen();
            ProcessPoison();
        }

        public override void SetOperateTime()
        {
            long time = Env.Time + 2000;

            if (AloneTime < time && AloneTime > Env.Time)
                time = AloneTime;

            if (DeadTime < time && DeadTime > Env.Time)
                time = DeadTime;

            if (OwnerTime < time && OwnerTime > Env.Time)
                time = OwnerTime;

            if (ExpireTime < time && ExpireTime > Env.Time)
                time = ExpireTime;

            if (PKPointTime < time && PKPointTime > Env.Time)
                time = PKPointTime;

            if (LastHitTime < time && LastHitTime > Env.Time)
                time = LastHitTime;

            if (EXPOwnerTime < time && EXPOwnerTime > Env.Time)
                time = EXPOwnerTime;

            if (SearchTime < time && SearchTime > Env.Time)
                time = SearchTime;

            if (RoamTime < time && RoamTime > Env.Time)
                time = RoamTime;

            if (ShockTime < time && ShockTime > Env.Time)
                time = ShockTime;

            if (RegenTime < time && RegenTime > Env.Time && Health < MaxHealth)
                time = RegenTime;

            if (RageTime < time && RageTime > Env.Time)
                time = RageTime;

            if (HallucinationTime < time && HallucinationTime > Env.Time)
                time = HallucinationTime;

            if (ActionTime < time && ActionTime > Env.Time)
                time = ActionTime;

            if (MoveTime < time && MoveTime > Env.Time)
                time = MoveTime;

            if (AttackTime < time && AttackTime > Env.Time)
                time = AttackTime;

            if (HealTime < time && HealTime > Env.Time && HealAmount > 0)
                time = HealTime;

            if (BrownTime < time && BrownTime > Env.Time)
                time = BrownTime;

            foreach (var action in ActionList.Where(action => action.Time < time || action.Time <= Env.Time))
            {
                time = action.Time;
            }

            foreach (var poison in PoisonList.Where(poison => poison.TickTime < time || poison.TickTime <= Env.Time))
            {
                time = poison.TickTime;
            }

            foreach (var buff in Buffs.Where(buff => buff.NextTime < time || buff.NextTime <= Env.Time))
            {
                time = buff.NextTime;
            }

            if (OperateTime <= Env.Time || time < OperateTime)
                OperateTime = time;
        }

        public override void Process(DelayedAction action)
        {
            switch (action.Type)
            {
                case DelayedType.Damage:
                    CompleteAttack(action.Params);
                    break;
                case DelayedType.RangeDamage:
                    CompleteRangeAttack(action.Params);
                    break;
                case DelayedType.Die:
                    CompleteDeath(action.Params);
                    break;
                case DelayedType.Recall:
                    PetRecall();
                    break;
                case DelayedType.SpellEffect:
                    CompleteSpellEffect(action.Params);
                    break;
            }
        }

        public void PetRecall()
        {
            if (Master?.CurrentMap == null) return;

            // Prevent pet from warping into NoPets maps (unless exempt e.g. pickup pets)
            if (Master.CurrentMap.Info.NoPets && !IgnoresNoPetRestriction)
            {
                Master.ReceiveChat(GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotFollowIntoMapWaitHere, Name), ChatType.System);

                Frozen = true;
                Target = null;
                PMode = PetMode.None;

                Broadcast(new S.ObjectTurn
                {
                    Direction = Direction,
                    Location = CurrentLocation
                });

                return;
            }

            bool wasFrozen = Frozen;

            // Restore pet state
            Frozen = false;
            Target = null;
            PMode = PetMode.Both;

            // Only teleport if needed
            if (CurrentMap != Master.CurrentMap)
            {
                if (!Teleport(Master.CurrentMap, Master.Back))
                    Teleport(Master.CurrentMap, Master.CurrentLocation);

                // Only show message if returning from frozen/waiting state
                if (wasFrozen)
                {
                    Master.ReceiveChat(GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.HasReturnedToYourSide,Name), ChatType.System);
                }
            }
        }
        protected virtual void CompleteAttack(IList<object> data)
        {
            MapObject? target = (MapObject?)data[0];
            int damage = (int)data[1];
            DefenceType defence = (DefenceType)data[2];

            if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;

            target.Attacked(this, damage, defence);
        }

        protected virtual void CompleteRangeAttack(IList<object> data)
        {
            MapObject? target = (MapObject?)data[0];
            int damage = (int)data[1];
            DefenceType defence = (DefenceType)data[2];

            if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;

            target.Attacked(this, damage, defence);
        }

        protected virtual void CompleteDeath(IList<object> data)
        {
            throw new NotImplementedException();
        }

        protected virtual void CompleteSpellEffect(IList<object> data)
        {
            MapObject? target = (MapObject?)data[0];
            SpellEffect effect = (SpellEffect)data[1];

            if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;

            S.ObjectEffect p = new S.ObjectEffect { ObjectID = target.ObjectID, Effect = effect };
            CurrentMap?.Broadcast(p, target.CurrentLocation);
        }

        protected virtual void ProcessRegen()
        {
            if (Dead) return;

            int healthRegen = 0;

            if (CanRegen)
            {
                RegenTime = Env.Time + RegenDelay;


                if (HP < Stats[Stat.HP])
                    healthRegen += (int)(Stats[Stat.HP] * 0.022F) + 1;
            }


            if (Env.Time > HealTime)
            {
                HealTime = Env.Time + HealDelay;

                if (HealAmount > 5)
                {
                    healthRegen += 5;
                    HealAmount -= 5;
                }
                else
                {
                    healthRegen += HealAmount;
                    HealAmount = 0;
                }
            }

            if (healthRegen > 0)
            {
                ChangeHP(healthRegen);
                BroadcastDamageIndicator(DamageType.Hit, healthRegen);
            }
            if (HP == Stats[Stat.HP]) HealAmount = 0;
        }
        
        
        protected virtual void ProcessPoison()
        {
            PoisonType type = PoisonType.None;
            ArmourRate = 1F;
            DamageRate = 1F;

            for (int i = PoisonList.Count - 1; i >= 0; i--)
            {
                if (Dead) return;

                Poison poison = PoisonList[i];
                if (poison.Owner is { Node: null })
                {
                    if (poison.PType == PoisonType.Slow)
                    {
                        MoveSpeed = Info.MoveSpeed;
                        AttackSpeed = Info.AttackSpeed;
                        AttackTime = Env.Time + AttackSpeed;
                    }
                    PoisonList.RemoveAt(i);
                    continue;
                }

                if (Env.Time > poison.TickTime)
                {
                    poison.Time++;
                    poison.TickTime = Env.Time + poison.TickSpeed;

                    if (poison.Time >= poison.Duration)
                    {
                        if (poison.PType == PoisonType.Slow)
                        {
                            MoveSpeed = Info.MoveSpeed;
                            AttackSpeed = Info.AttackSpeed;
                            AttackTime = Env.Time + AttackSpeed;
                        }
                        PoisonList.RemoveAt(i);
                        continue;
                    }

                    if (poison.PType is PoisonType.Green or PoisonType.Bleeding)
                    {
                        if (EXPOwner == null || EXPOwner.Dead)
                        {
                            EXPOwner = poison.Owner;
                            EXPOwnerTime = Env.Time + EXPOwnerDelay;
                        }
                        else if (EXPOwner == poison.Owner)
                            EXPOwnerTime = Env.Time + EXPOwnerDelay;

                        if (poison.PType == PoisonType.Bleeding)
                        {
                            Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.Bleeding, EffectType = 0 });
                        }

                        //ChangeHP(-poison.Value);
                        PoisonDamage(-poison.Value, poison.Owner);
                        BroadcastDamageIndicator(DamageType.Hit, -poison.Value);
                        if (PoisonStopRegen)
                            RegenTime = Env.Time + RegenDelay;
                        if (poison.Owner != null && Target == null)
                            Target = poison.Owner;
                    }

                    if (poison.PType == PoisonType.DelayedExplosion)
                    {
                        if (Env.Time > ExplosionInflictedTime) ExplosionInflictedStage++;

                        if (!ProcessDelayedExplosion(poison))
                        {
                            ExplosionInflictedStage = 0;
                            ExplosionInflictedTime = 0;

                            if (Dead) break; //temp to stop crashing

                            PoisonList.RemoveAt(i);
                            continue;
                        }
                    }
                }

                switch (poison.PType)
                {
                    case PoisonType.Red:
                        ArmourRate -= 0.5F;
                        break;
                    case PoisonType.Stun:
                        DamageRate += 0.5F;
                        break;
                    case PoisonType.Blindness:
                        break;
                    case PoisonType.Slow:
                        MoveSpeed = (ushort)Math.Min(3500, MoveSpeed + 100);
                        AttackSpeed = (ushort)Math.Min(3500, AttackSpeed + 100);

                        if (poison.Time >= poison.Duration)
                        {
                            MoveSpeed = Info.MoveSpeed;
                            AttackSpeed = Info.AttackSpeed;
                            //Reset the Attack time
                            AttackTime = Env.Time + AttackSpeed;
                        }
                        break;
                    default:
                        break;
                }
                type |= poison.PType;
                /*
                if ((int)type < (int)poison.PType)
                    type = poison.PType;
                 */
            }


            if (type == CurrentPoison) return;

            CurrentPoison = type;
            Broadcast(new S.ObjectPoisoned { ObjectID = ObjectID, Poison = type });
        }

        private bool ProcessDelayedExplosion(Poison poison)
        {
            if (Dead) return false;

            switch (ExplosionInflictedStage)
            {
                case 0:
                    Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.DelayedExplosion, EffectType = 0 });
                    return true;
                case 1:
                {
                    if (Env.Time > ExplosionInflictedTime)
                        ExplosionInflictedTime = poison.TickTime + 3000;
                    Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.DelayedExplosion, EffectType = 1 });
                    return true;
                }
                case 2:
                {
                    Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.DelayedExplosion, EffectType = 2 });
                    if (poison.Owner != null)
                    {
                        switch (poison.Owner.Race)
                        {
                            case ObjectType.Player:
                                PlayerObject caster = (PlayerObject)poison.Owner;
                                DelayedAction action = new DelayedAction(DelayedType.Magic, Env.Time, poison.Owner, caster.GetMagic(Spell.DelayedExplosion), poison.Value, this.CurrentLocation);
                                CurrentMap.ActionList.Add(action);
                                //Attacked((PlayerObject)poison.Owner, poison.Value, DefenceType.MAC, false);
                                break;
                            case ObjectType.Monster://this is in place so it could be used by mobs if one day someone chooses to
                                Attacked((MonsterObject)poison.Owner, poison.Value, DefenceType.MAC);
                                break;
                        }
                        LastHitter = poison.Owner;
                    }
                    return false;
                }
                default:
                    return false;
            }
        }

        private void ProcessBuffs()
        {
            bool refresh = false;
            for (int i = Buffs.Count - 1; i >= 0; i--)
            {
                Buff buff = Buffs[i];

                if (buff.NextTime > Env.Time) continue;

                if (!buff.Paused && buff.StackType != BuffStackType.Infinite)
                {
                    var change = Env.Time - buff.LastTime;
                    buff.ExpireTime -= change;
                }

                buff.LastTime = Env.Time;
                buff.NextTime = Env.Time + 1000;

                if ((buff.ExpireTime > 0 || buff.StackType == BuffStackType.Infinite) && !buff.FlagForRemoval) continue;

                Buffs.RemoveAt(i);

                if (buff.Info.Visible)
                {
                    Broadcast(new S.RemoveBuff { Type = buff.Type, ObjectID = ObjectID });
                }

                switch (buff.Type)
                {
                    case BuffType.Hiding:
                    case BuffType.MoonLight:
                    case BuffType.DarkBody:
                        if (!HasAnyBuffs(buff.Type, BuffType.ClearRing, BuffType.Hiding, BuffType.MoonLight, BuffType.DarkBody))
                        {
                            Hidden = false;
                        }
                        if (buff.Type is BuffType.MoonLight or BuffType.DarkBody)
                        {
                            if (!HasAnyBuffs(buff.Type, BuffType.MoonLight, BuffType.DarkBody))
                            {
                                Sneaking = false;
                            }
                        }
                        break;
                }

                ProcessBuffEnd(buff);

                refresh = true;
            }

            if (refresh) RefreshAll();
        }

        protected virtual void ProcessBuffEnd(Buff buff)
        {

        }

        protected virtual void ProcessAI()
        {
            if (Dead) return;

            if (DieNextTurn)
            {
                Die();
                return;
            }

            if (Master is { CurrentMap: not null })
            {
                bool masterAllowsPets = !Master.CurrentMap.Info.NoPets || IgnoresNoPetRestriction;
                bool needsRecall = CurrentMap != Master.CurrentMap;

                // If frozen AND on different map AND master allows pets — force recall
                if (Frozen && needsRecall && masterAllowsPets)
                {
                    PetRecall();
                    return;
                }

                // If frozen but already on correct map — unfreeze
                if (Frozen && !needsRecall && masterAllowsPets)
                {
                    Frozen = false;
                    PMode = PetMode.Both;
                }
            }

            // Still frozen = do nothing
            if (Frozen) return;

            if (Master != null)
            {
                PetMode mode = Master.PMode;
                Map? masterMap = Master.CurrentMap;
                Point masterLocation = Master.CurrentLocation;

                if (mode is PetMode.Both or PetMode.MoveOnly or PetMode.FocusMasterTarget
                    && masterMap != null)
                {
                    if (!Functions.InRange(CurrentLocation, masterLocation, Globals.DataRange) || CurrentMap != masterMap)
                        PetRecall();
                }

                if (mode is PetMode.MoveOnly or PetMode.None)
                {
                    Target = null;
                }
            }

            CheckAlone();

            if (!Alone || Settings.MonsterProcessWhenAlone)
            {
                ProcessStacking();

                ProcessSearch();
                ProcessRoam();
                ProcessTarget();
            }
        }

        protected virtual void CheckAlone()
        {
            if (Env.Time < AloneTime) return;

            AloneTime = Env.Time + AloneDelay;

            if (Route.Count > 0)
            {
                Alone = false;
                return;
            }

            if (CurrentMap == null)
            {
                return;
            }

            if (CurrentMap.Players.Count == 0)
            {
                Alone = true;
                return;
            }

            if (CurrentMap.Players.Any(t => Functions.InRange(CurrentLocation, t.CurrentLocation, Globals.DataRange * 2)))
            {
                Alone = false;
                return;
            }

            Alone = true;
        }

        protected virtual void ProcessStacking()
        {
            //Stacking or In front of master - Move
            Stacking = CheckStacked();

            if (CanMove && ((Master != null && Master.Front == CurrentLocation) || Stacking))
            {
                //Walk Randomly
                if (!Walk(Direction))
                {
                    MirDirection dir = Direction;

                    switch (RandomProvider.Next(3)) // favour Clockwise
                    {
                        case 0:
                            for (int i = 0; i < 7; i++)
                            {
                                dir = Functions.NextDir(dir);

                                if (Walk(dir))
                                    break;
                            }
                            break;
                        default:
                            for (int i = 0; i < 7; i++)
                            {
                                dir = Functions.PreviousDir(dir);

                                if (Walk(dir))
                                    break;
                            }
                            break;
                    }
                }

                return;
            }
        }

        protected virtual void ProcessSearch()
        {
            if (Env.Time < SearchTime) return;
            if (Master is { PMode: PetMode.MoveOnly or PetMode.None or PetMode.FocusMasterTarget }) return;

            SearchTime = Env.Time + SearchDelay;

            if (Target == null || RandomProvider.Next(3) == 0)
                FindTarget();
        }

        protected virtual void ProcessRoam()
        {
            if (Target != null || Env.Time < RoamTime) return;

            if (ProcessRoute()) return;

            if (Master != null)
            {
                MoveTo(Master.Back);
                return;
            }

            RoamTime = Env.Time + RoamDelay;

            if (RandomProvider.Next(10) != 0) return;

            switch (RandomProvider.Next(3)) //Face Walk
            {
                case 0:
                    Turn((MirDirection)RandomProvider.Next(8));
                    break;
                default:
                    Walk(Direction);
                    break;
            }
        }

        protected virtual void ProcessTarget()
        {
            if (Target == null || !CanAttack) return;

            if (InAttackRange())
            {
                Attack();

                if (Target is { Dead: true })
                {
                    FindTarget();
                }

                return;
            }

            if (Env.Time < ShockTime)
            {
                Target = null;
                return;
            }

            if (Settings.MonsterRecallEnabled && Info.CanRecall && TryRecallToTarget())
            {
                return;
            }

            MoveTo(Target.CurrentLocation);
        }

        protected virtual bool TryRecallToTarget()
        {
            if (Target?.CurrentMap == null) return false;

            int recallRange = Math.Max(1, Settings.MonsterRecallRange);
            int recallCooldown = Math.Max(0, Settings.MonsterRecallCooldown);

            if (Env.Time < NextRecallTime) return false;

            bool needsRecall = Target.CurrentMap != CurrentMap ||
                               !Functions.InRange(CurrentLocation, Target.CurrentLocation, recallRange);

            if (!needsRecall) return false;

            Point destination = FindRecallPoint(Target.CurrentMap, Target.CurrentLocation);
            if (!Target.CurrentMap.ValidPoint(destination)) return false;

            if (!Teleport(Target.CurrentMap, destination, true))
                return false;

            NextRecallTime = Env.Time + recallCooldown;
            ActionTime = Env.Time + 1000;
            return true;
        }

        protected virtual Point FindRecallPoint(Map map, Point targetLocation)
        {
            if (map.ValidPoint(targetLocation)) return targetLocation;

            for (int i = 0; i < 8; i++)
            {
                Point point = Functions.PointMove(targetLocation, (MirDirection)i, 1);
                if (map.ValidPoint(point))
                    return point;
            }

            return targetLocation;
        }

        protected virtual bool InAttackRange()
        {
            if (Target == null || Target.CurrentMap != CurrentMap) return false;

            return Target.CurrentLocation != CurrentLocation && Functions.InRange(CurrentLocation, Target.CurrentLocation, 1);
        }

        protected virtual void FindTarget()
        {
            Map? Current = CurrentMap;
            if (Current == null) return;

            for (int d = 0; d <= Info.ViewRange; d++)
            {
                for (int y = CurrentLocation.Y - d; y <= CurrentLocation.Y + d; y++)
                {
                    if (y >= Current.Height) break;
                    
                    if (y < 0) continue;

                    for (int x = CurrentLocation.X - d; x <= CurrentLocation.X + d; x += Math.Abs(y - CurrentLocation.Y) == d ? 1 : d * 2)
                    {
                        if (x >= Current.Width) break;
                        if (x < 0) continue;
                        Cell cell = Current.Cells[x, y];
                        if (!cell.Valid) continue;
                        
                        for (int i = 0; i < cell.Objects.Count; i++)
                        {
                            MapObject ob = cell.Objects[i];
                            switch (ob.Race)
                            {
                                case ObjectType.Monster:
                                case ObjectType.Hero:
                                    if (!ob.IsAttackTarget(this)) continue;
                                    
                                    if (ob.Hidden && (!CoolEye || Level < ob.Level)) continue;
                                    
                                    if (this is TrapRock && ob.InTrapRock) continue;

                                    if (ob.Race == ObjectType.Monster && ob is StoneTrap)
                                    {
                                        if (Target is null || (Target is not null && Target is not StoneTrap))
                                        {
                                            Target = ob;
                                        }
                                        
                                        return;
                                    }
                                    
                                    Target ??= ob;
                                    continue;
                                    
                                case ObjectType.Player:

                                    if (Target != null)
                                    {
                                        continue;
                                    }

                                    PlayerObject player = (PlayerObject)ob;
                                    if (!ob.IsAttackTarget(this)) continue;
                                    if (player.GMGameMaster || ob.Hidden && (!CoolEye || Level < ob.Level) || Env.Time < HallucinationTime) continue;

                                    Target = ob;

                                    if (Master != null)
                                    {
                                        Target = player.Pets.FirstOrDefault(pet => pet.IsAttackTarget(this)) ?? Target;
                                    }
                                    continue;
                                default:
                                    continue;
                            }
                        }
                    }
                }
            }
        }

        protected virtual bool ProcessRoute()
        {
            if (Route.Count < 1) return false;

            RoamTime = Env.Time + 500;

            if (CurrentLocation == Route[RoutePoint].Location)
            {
                if (Route[RoutePoint].Delay > 0 && !Waiting)
                {
                    Waiting = true;
                    RoamTime = Env.Time + RoamDelay + Route[RoutePoint].Delay;
                    return true;
                }

                Waiting = false;
                RoutePoint++;
            }

            if (RoutePoint > Route.Count - 1) RoutePoint = 0;

            if (CurrentMap == null || !CurrentMap.ValidPoint(Route[RoutePoint].Location)) return true;

            MoveTo(Route[RoutePoint].Location);

            return true;
        }

        protected virtual void MoveTo(Point location)
        {
            if (CurrentLocation == location) return;

            bool inRange = Functions.InRange(location, CurrentLocation, 1);

            if (inRange)
            {
                if (CurrentMap == null || !CurrentMap.ValidPoint(location)) return;
                Cell cell = CurrentMap.GetCell(location);
                if (cell.Objects.Any(ob => ob.Blocking))
                {
                    return;
                }
            }

            MirDirection dir = Functions.DirectionFromPoint(CurrentLocation, location);

            if (Walk(dir)) return;

            switch (RandomProvider.Next(2)) //No favour
            {
                case 0:
                    for (int i = 0; i < 7; i++)
                    {
                        dir = Functions.NextDir(dir);

                        if (Walk(dir))
                            return;
                    }
                    break;
                default:
                    for (int i = 0; i < 7; i++)
                    {
                        dir = Functions.PreviousDir(dir);

                        if (Walk(dir))
                            return;
                    }
                    break;
            }
        }

        public virtual void Turn(MirDirection dir)
        {
            if (!CanMove || CurrentMap == null) return;

            Direction = dir;
            InSafeZone = CurrentMap.GetSafeZone(CurrentLocation) != null;

            Cell cell = CurrentMap.GetCell(CurrentLocation);

            foreach (var ob in cell.Objects.Where(obj => obj.Race == ObjectType.Spell).Cast<SpellObject>())
            {
                ob.ProcessSpell(this);
            }

            Broadcast(new S.ObjectTurn { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });
        }

        public virtual bool Walk(MirDirection dir)
        {
            if (!CanMove || CurrentMap == null) return false;

            Point location = Functions.PointMove(CurrentLocation, dir, 1);

            if (!CurrentMap.ValidPoint(location)) return false;

            Cell cell = CurrentMap.GetCell(location);

            if (cell.Objects.Any(ob => ob.Blocking && Race != ObjectType.Creature))
            {
                return false;
            }

            CurrentMap.GetCell(CurrentLocation).Remove(this);

            Direction = dir;
            RemoveObjects(dir, 1);
            CurrentLocation = location;
            CurrentMap.GetCell(CurrentLocation).Add(this);
            AddObjects(dir, 1);

            if (Hidden)
            {
                RemoveBuff(BuffType.Hiding);
            }

            CellTime = Env.Time + 500;
            ActionTime = Env.Time + 300;
            MoveTime = Env.Time + MoveSpeed;
            if (MoveTime > AttackTime)
                AttackTime = MoveTime;

            InSafeZone = CurrentMap.GetSafeZone(CurrentLocation) != null;

            Broadcast(new S.ObjectWalk { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });

            cell = CurrentMap.GetCell(CurrentLocation);

            foreach (var ob in cell.Objects.Where(obj => obj.Race == ObjectType.Spell).Cast<SpellObject>())
            {
                ob.ProcessSpell(this);
            }

            return true;
        }
        
        protected virtual void Attack()
        {
            if (BindingShotCenter) ReleaseBindingShot();

            ShockTime = 0;

            if (Target == null || !Target.IsAttackTarget(this))
            {
                Target = null;
                return;
            }

            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
            Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });

            ActionTime = Env.Time + 300;
            AttackTime = Env.Time + AttackSpeed;

            int damage = GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]);
            if (damage == 0) return;

            DelayedAction action = new DelayedAction(DelayedType.Damage, Env.Time + 300, Target, damage, DefenceType.ACAgility);
            ActionList.Add(action);
        }

        public void ReleaseBindingShot()
        {
            if (!BindingShotCenter) return;

            ShockTime = 0;
            Broadcast(GetInfo());//update clients in range (remove effect)
            BindingShotCenter = false;

            if (CurrentMap == null)
            {
                return;
            }
            
            //the centertarget is escaped so make all shocked mobs awake (3x3 from center)
            Point place = CurrentLocation;
            for (int y = place.Y - 1; y <= place.Y + 1; y++)
            {
                if (y >= CurrentMap.Height) break;
                
                if (y < 0) continue;

                for (int x = place.X - 1; x <= place.X + 1; x++)
                {
                    if (x >= CurrentMap.Width) break;
                    if (x < 0) continue;

                    Cell cell = CurrentMap.GetCell(x, y);
                    if (!cell.Valid) continue;

                    foreach (var targetObj in cell.Objects)
                    {
                        if (targetObj.Node == null || targetObj.Race != ObjectType.Monster) continue;
                        if (((MonsterObject)targetObj).ShockTime == 0) continue;

                        //each centerTarget has its own effect which needs to be cleared when no longer shocked
                        if (((MonsterObject)targetObj).BindingShotCenter) ((MonsterObject)targetObj).ReleaseBindingShot();
                        else ((MonsterObject)targetObj).ShockTime = 0;

                        break;
                    }
                }
            }
        }

        public bool FindNearby(int distance)
        {
            if (CurrentMap == null) return false;
            
            for (int d = 0; d <= distance; d++)
            {
                for (int y = CurrentLocation.Y - d; y <= CurrentLocation.Y + d; y++)
                {
                    if (y < 0) continue;
                    if (y >= CurrentMap.Height) break;

                    for (int x = CurrentLocation.X - d; x <= CurrentLocation.X + d; x += Math.Abs(y - CurrentLocation.Y) == d ? 1 : d * 2)
                    {
                        if (x < 0) continue;
                        if (x >= CurrentMap.Width) break;
                        if (!CurrentMap.ValidPoint(x, y)) continue;
                        Cell cell = CurrentMap.GetCell(x, y);

                        foreach (var ob in cell.Objects)
                        {
                            switch (ob.Race)
                            {
                                case ObjectType.Monster:
                                case ObjectType.Player:
                                case ObjectType.Hero:
                                    if (!ob.IsAttackTarget(this)) continue;
                                    if (ob.Hidden && (!CoolEye || Level < ob.Level)) continue;
                                    if (ob.Race == ObjectType.Player)
                                    {
                                        PlayerObject player = ((PlayerObject)ob);
                                        if (player.GMGameMaster) continue;
                                    }
                                    return true;
                                default:
                                    continue;
                            }
                        }
                    }
                }
            }

            return false;
        }
        public bool FindFriendsNearby(int distance)
        {
            if (CurrentMap == null) return false;
            
            for (int d = 0; d <= distance; d++)
            {
                for (int y = CurrentLocation.Y - d; y <= CurrentLocation.Y + d; y++)
                {
                    if (y < 0) continue;
                    if (y >= CurrentMap.Height) break;

                    for (int x = CurrentLocation.X - d; x <= CurrentLocation.X + d; x += Math.Abs(y - CurrentLocation.Y) == d ? 1 : d * 2)
                    {
                        if (x < 0) continue;
                        if (x >= CurrentMap.Width) break;
                        if (!CurrentMap.ValidPoint(x, y)) continue;
                        Cell cell = CurrentMap.GetCell(x, y);

                        foreach (var ob in cell.Objects)
                        {
                            switch (ob.Race)
                            {
                                case ObjectType.Monster:
                                case ObjectType.Player:
                                case ObjectType.Hero:
                                    if (ob == this || ob.Dead) continue;
                                    if (ob.IsAttackTarget(this)) continue;
                                    if (ob.Race == ObjectType.Player)
                                    {
                                        PlayerObject player = ((PlayerObject)ob);
                                        if (player.GMGameMaster) continue;
                                    }
                                    return true;
                                default:
                                    continue;
                            }
                        }
                    }
                }
            }

            return false;
        }

        protected List<MapObject> FindAllFriends(int dist, Point location, bool needSight = true, bool ownAI = true)
        {
            List<MapObject> targets = [];
            if (CurrentMap == null) return targets;
            
            for (int d = 0; d <= dist; d++)
            {
                for (int y = location.Y - d; y <= location.Y + d; y++)
                {
                    if (y < 0) continue;
                    if (y >= CurrentMap.Height) break;

                    for (int x = location.X - d; x <= location.X + d; x += Math.Abs(y - location.Y) == d ? 1 : d * 2)
                    {
                        if (x < 0) continue;
                        if (x >= CurrentMap.Width) break;

                        Cell cell = CurrentMap.GetCell(x, y);
                        if (!cell.Valid) continue;

                        foreach (var ob in cell.Objects.Where(ob => ob != this))
                        {
                            switch (ob.Race)
                            {
                                case ObjectType.Monster:
                                case ObjectType.Player:
                                case ObjectType.Hero:
                                    if (ob.Dead) continue;
                                    if (!ownAI && ob.Race == ObjectType.Monster && ((MonsterObject)ob).Info.AI == Info.AI) continue;
                                    if (!ob.IsFriendlyTarget(this)) continue;
                                    if (ob.Master != Master) continue;
                                    if (ob.Hidden && (!CoolEye || Level < ob.Level) && needSight) continue;
                                    targets.Add(ob);
                                    continue;
                                default:
                                    continue;
                            }
                        }
                    }
                }
            }
            return targets;
        }

        public List<MapObject> FindAllNearby(int dist, Point location, bool needSight = true)
        {
            List<MapObject> targets = [];
            if (CurrentMap == null) return targets;
            
            for (int d = 0; d <= dist; d++)
            {
                for (int y = location.Y - d; y <= location.Y + d; y++)
                {
                    if (y < 0) continue;
                    if (y >= CurrentMap.Height) break;

                    for (int x = location.X - d; x <= location.X + d; x += Math.Abs(y - location.Y) == d ? 1 : d * 2)
                    {
                        if (x < 0) continue;
                        if (x >= CurrentMap.Width) break;

                        Cell cell = CurrentMap.GetCell(x, y);
                        if (!cell.Valid) continue;

                        foreach (var ob in cell.Objects)
                        {
                            switch (ob.Race)
                            {
                                case ObjectType.Monster:
                                case ObjectType.Player:
                                case ObjectType.Hero:
                                    targets.Add(ob);
                                    continue;
                                default:
                                    continue;
                            }
                        }
                    }
                }
            }
            return targets;
        }

        protected List<MapObject> FindAllTargets(int dist, Point location, bool needSight = true)
        {
            List<MapObject> targets = [];
            if (CurrentMap == null) return targets;
            
            for (int d = 0; d <= dist; d++)
            {
                for (int y = location.Y - d; y <= location.Y + d; y++)
                {
                    if (y < 0) continue;
                    if (y >= CurrentMap.Height) break;

                    for (int x = location.X - d; x <= location.X + d; x += Math.Abs(y - location.Y) == d ? 1 : d * 2)
                    {
                        if (x < 0) continue;
                        if (x >= CurrentMap.Width) break;

                        Cell cell = CurrentMap.GetCell(x, y);
                        if (!cell.Valid) continue;

                        foreach (var ob in cell.Objects)
                        {
                            switch (ob.Race)
                            {
                                case ObjectType.Monster:
                                case ObjectType.Player:
                                case ObjectType.Hero:
                                    if (!ob.IsAttackTarget(this)) continue;
                                    if (ob.Hidden && (!CoolEye || Level < ob.Level) && needSight) continue;
                                    if (ob.Race == ObjectType.Player)
                                    {
                                        PlayerObject player = ((PlayerObject)ob);
                                        if (player.GMGameMaster) continue;
                                    }
                                    targets.Add(ob);
                                    continue;
                                default:
                                    continue;
                            }
                        }
                    }
                }
            }
            return targets;
        }

        public override bool IsAttackTarget(HumanObject? attacker)
        {
            if (attacker?.Node == null) return false;
            if (Dead) return false;
            if (Master == null) return true;

            if (attacker.Race == ObjectType.Hero)
                attacker = ((HeroObject)attacker).Owner;

            if (attacker.AMode == AttackMode.Peace) return false;
            if (Master == attacker) return attacker.AMode == AttackMode.All;
            if (Master.Race == ObjectType.Player && (attacker.InSafeZone || InSafeZone)) return false;

            switch (attacker.AMode)
            {
                case AttackMode.Group:
                    return Master.GroupMembers == null || !Master.GroupMembers.Contains(attacker);
                case AttackMode.Guild:
                {
                    if (Master is not PlayerObject masterPlayer) return false;
                    return masterPlayer.MyGuild == null || masterPlayer.MyGuild != attacker.MyGuild;
                }
                case AttackMode.EnemyGuild:
                {
                    if (Master is not PlayerObject masterPlayer) return false;
                    return (masterPlayer.MyGuild != null && attacker.MyGuild != null) && masterPlayer.MyGuild.IsEnemy(attacker.MyGuild);
                }
                case AttackMode.RedBrown:
                    return Master.PKPoints >= 200 || Env.Time < Master.BrownTime;
                default:
                    return true;
            }
        }
        public override bool IsAttackTarget(MonsterObject? attacker)
        {
            if (attacker?.Node == null) return false;
            if (Dead || attacker == this) return false;
            if (attacker.Race == ObjectType.Creature) return false;

            if (attacker.Info.AI is 6 or 113) // Guard
            {
                if (Info.AI != 1 && Info.AI != 2 && Info.AI != 3 && (Master == null || Master.PKPoints >= 200)) //Not Dear/Hen/Tree/Pets or Red Master 
                    return true;
            }
            else if (attacker.Info.AI == 58) // Tao Guard - attacks Pets
            {
                if (Info.AI != 1 && Info.AI != 2 && Info.AI != 3 && (Master == null || Master.AMode != AttackMode.Peace)) //Not Dear/Hen/Tree or Peaceful Master
                    return true;
            }
            else if (Master != null) //Pet Attacked
            {
                if (attacker.Master == null) //Wild Monster
                    return true;

                //Pet Vs Pet
                if (Master == attacker.Master)
                    return false;

                if (Env.Time < ShockTime) //Shocked
                    return false;

                if (Master.Race == ObjectType.Player && attacker.Master.Race == ObjectType.Player && (Master.InSafeZone || attacker.Master.InSafeZone)) return false;

                switch (attacker.Master.AMode)
                {
                    case AttackMode.Group:
                        if (Master.GroupMembers != null && Master.GroupMembers.Contains((PlayerObject)attacker.Master)) return false;
                        break;
                    case AttackMode.Guild:
                        break;
                    case AttackMode.EnemyGuild:
                        break;
                    case AttackMode.RedBrown:
                        if (attacker.Master.PKPoints < 200 || Env.Time > attacker.Master.BrownTime) return false;
                        break;
                    case AttackMode.Peace:
                        return false;
                }

                if (Master.Pets.Any(t => t.EXPOwner == attacker.Master))
                {
                    return true;
                }

                if (attacker.Master.Pets.Any(ob => ob == Target || ob.Target == this))
                {
                    return true;
                }

                return Master.LastHitter == attacker.Master;
            }
            else if (attacker.Master != null) //Pet Attacking Wild Monster
            {
                if (Env.Time < ShockTime) //Shocked
                    return false;

                if (attacker.Master.Pets.Any(ob => ob == Target || ob.Target == this))
                {
                    return true;
                }

                if (Target == attacker.Master)
                    return true;
            }

            if (Env.Time < attacker.HallucinationTime) return true;

            return Env.Time < attacker.RageTime;
        }
        
        public override bool IsFriendlyTarget(HumanObject ally)
        {
            if (Master == null) return false;
            if (Master == ally) return true;

            switch (ally.AMode)
            {
                case AttackMode.Group:
                    return Master.GroupMembers != null && Master.GroupMembers.Contains(ally);
                case AttackMode.Guild:
                    return false;
                case AttackMode.EnemyGuild:
                    return true;
                case AttackMode.RedBrown:
                    return Master.PKPoints < 200 & Env.Time > Master.BrownTime;
            }
            return true;
        }

        public override bool IsFriendlyTarget(MonsterObject ally)
        {
            if (Master != null) return false;
            if (ally.Race != ObjectType.Monster) return false;
            return ally.Master == null;
        }

        public override int Attacked(HumanObject attacker, int damage, DefenceType type = DefenceType.ACAgility, bool damageWeapon = true)
        {
            if (Target == null && attacker.IsAttackTarget(this))
            {
                Target = attacker;
            }

            var armour = GetArmour(type, attacker, out bool hit);

            if (!hit)
                return 0;

            armour = (int)Math.Max(int.MinValue, (Math.Min(int.MaxValue, (decimal)(armour * ArmourRate))));
            damage = (int)Math.Max(int.MinValue, (Math.Min(int.MaxValue, (decimal)(damage * DamageRate))));

            if (damageWeapon)
                attacker.DamageWeapon();
            damage += attacker.Stats[Stat.AttackBonus];

            if (armour >= damage)
            {
                BroadcastDamageIndicator(DamageType.Miss);
                return 0;
            }

            if (RandomProvider.Next(100) < (attacker.Stats[Stat.CriticalRate] * Settings.CriticalRateWeight))
            {
                Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.Critical });
                damage = Math.Min(int.MaxValue, damage + (int)Math.Floor(damage * (((double)attacker.Stats[Stat.CriticalDamage] / (double)Settings.CriticalDamageWeight) * 10)));
                BroadcastDamageIndicator(DamageType.Critical);
            }

            if (Target != this && attacker.IsAttackTarget(this))
            {
                if (attacker.CharacterInfo.MentalState == 2)
                {
                    if (Functions.MaxDistance(CurrentLocation, attacker.CurrentLocation) < (8 - attacker.CharacterInfo.MentalStateLvl))
                        Target = attacker;
                }
                else
                    Target = attacker;
            }

            if (BindingShotCenter) ReleaseBindingShot();
            ShockTime = 0;

            for (int i = PoisonList.Count - 1; i >= 0; i--)
            {
                if (PoisonList[i].PType != PoisonType.LRParalysis) continue;

                PoisonList.RemoveAt(i);
                OperateTime = 0;
            }

            if (Master != null && Master != attacker && (Master.Race != ObjectType.Hero || Master.Race == ObjectType.Hero && attacker != ((HeroObject)Master).Owner))
                if (Env.Time > Master.BrownTime && Master.PKPoints < 200)
                    attacker.BrownTime = Env.Time + Settings.Minute;

            if (EXPOwner == null || EXPOwner.Dead)
            {
                EXPOwner = GetAttacker(attacker);
            }

            if (EXPOwner == attacker)
                EXPOwnerTime = Env.Time + EXPOwnerDelay;

            ushort levelOffset = (ushort)(Level > attacker.Level ? 0 : Math.Min(10, attacker.Level - Level));

            ApplyNegativeEffects(attacker, type, levelOffset);

            Broadcast(new S.ObjectStruck { ObjectID = ObjectID, AttackerID = attacker.ObjectID, Direction = Direction, Location = CurrentLocation });

            if (attacker.Stats[Stat.HPDrainRatePercent] > 0 && damageWeapon)
            {
                attacker.HpDrain += Math.Max(0, ((float)(damage - armour) / 100) * attacker.Stats[Stat.HPDrainRatePercent]);
                if (attacker.HpDrain > 2)
                {
                    int hpGain = (int)Math.Floor(attacker.HpDrain);
                    attacker.ChangeHP(hpGain);
                    attacker.HpDrain -= hpGain;
                }
            }

            attacker.GatherElement();

            if (attacker.CharacterInfo.Mentor != 0 && attacker.CharacterInfo.IsMentor)
            {
                if (attacker.TryGetBuff(BuffType.Mentor, out _))
                {
                    CharacterInfo? mentee = Env.GetCharacterInfo(attacker.CharacterInfo.Mentor);
                    PlayerObject? player = mentee != null ? Env.GetPlayer(mentee.Name) : null;
                    if (player != null && player.CurrentMap == attacker.CurrentMap && Functions.InRange(player.CurrentLocation, attacker.CurrentLocation, Globals.DataRange) && !player.Dead)
                    {
                        if (GroupMembers != null && GroupMembers.Contains(player))
                            damage += (int)Math.Round((double)(damage * attacker.Stats[Stat.MentorDamageRatePercent]) / 100);
                    }
                }
            }

            if (Master != null && Master != attacker && Master.Race == ObjectType.Player && Env.Time > Master.BrownTime && Master.PKPoints < 200 && !((PlayerObject)Master).AtWar(attacker))
            {
                attacker.BrownTime = Env.Time + Settings.Minute;
            }

            foreach (var pet in attacker.Pets.Where(pet => IsAttackTarget(pet) && pet.Target == null))
            {
                pet.Target = this;
            }

            BroadcastDamageIndicator(DamageType.Hit, armour - damage);

            ChangeHP(armour - damage);
            return damage - armour;
        }

        public override int Attacked(MonsterObject attacker, int damage, DefenceType type = DefenceType.ACAgility)
        {
            if (Target == null && attacker.IsAttackTarget(this))
                Target = attacker;


            var armour = GetArmour(type, attacker, out bool hit);
            if (!hit)
                return 0;

            armour = (int)Math.Max(int.MinValue, (Math.Min(int.MaxValue, (decimal)(armour * ArmourRate))));
            damage = (int)Math.Max(int.MinValue, (Math.Min(int.MaxValue, (decimal)(damage * DamageRate))));

            if (armour >= damage)
            {
                BroadcastDamageIndicator(DamageType.Miss);
                return 0;
            }

            if (Target != this && attacker.IsAttackTarget(this))
                Target = attacker;

            if (BindingShotCenter) ReleaseBindingShot();
            ShockTime = 0;

            for (int i = PoisonList.Count - 1; i >= 0; i--)
            {
                if (PoisonList[i].PType != PoisonType.LRParalysis) continue;

                PoisonList.RemoveAt(i);
                OperateTime = 0;
            }

            if (attacker.Info.AI == 6 || attacker.Info.AI == 58 || attacker.Info.AI == 113)
                EXPOwner = null;

            else if (attacker.Master != null)
            {
                if (attacker.CurrentMap != attacker.Master.CurrentMap || !Functions.InRange(attacker.CurrentLocation, attacker.Master.CurrentLocation, Globals.DataRange))
                    EXPOwner = null;
                else
                {

                    if (EXPOwner == null || EXPOwner.Dead)
                        EXPOwner = attacker.Master switch
                        {
                            HeroObject hero => hero.Owner,
                            _ => attacker.Master
                        };

                    if (EXPOwner == attacker.Master)
                        EXPOwnerTime = Env.Time + EXPOwnerDelay;
                }

            }

            Broadcast(new S.ObjectStruck { ObjectID = ObjectID, AttackerID = attacker.ObjectID, Direction = Direction, Location = CurrentLocation });

            BroadcastDamageIndicator(DamageType.Hit, armour - damage);

            ChangeHP(armour - damage);
            return damage - armour;
        }

        public override int Struck(int damage, DefenceType type = DefenceType.ACAgility)
        {
            int armour = type switch
            {
                DefenceType.ACAgility or DefenceType.AC => GetAttackPower(Stats[Stat.MinAC], Stats[Stat.MaxAC]),
                DefenceType.MACAgility or DefenceType.MAC => GetAttackPower(Stats[Stat.MinMAC], Stats[Stat.MaxMAC]),
                _ => 0
            };

            armour = (int)Math.Max(int.MinValue, (Math.Min(int.MaxValue, (decimal)(armour * ArmourRate))));
            damage = (int)Math.Max(int.MinValue, (Math.Min(int.MaxValue, (decimal)(damage * DamageRate))));

            if (armour >= damage) return 0;
            Broadcast(new S.ObjectStruck { ObjectID = ObjectID, AttackerID = 0, Direction = Direction, Location = CurrentLocation });

            ChangeHP(armour - damage);
            return damage - armour;
        }

        public override void ApplyPoison(Poison p, MapObject? Caster = null, bool NoResist = false, bool ignoreDefence = true)
        {
            if (p.Owner != null && p.Owner.IsAttackTarget(this) && Target == null)
                Target = p.Owner;

            if (Master != null && p.Owner is { Race: ObjectType.Player } && p.Owner != Master)
            {
                if (Env.Time > Master.BrownTime && Master.PKPoints < 200)
                    p.Owner.BrownTime = Env.Time + Settings.Minute;
            }

            if (!ignoreDefence && (p.PType == PoisonType.Green))
            {
                int armour = GetAttackPower(Stats[Stat.MinMAC], Stats[Stat.MaxMAC]);

                if (p.Value < armour)
                    p.PType = PoisonType.None;
                else
                    p.Value -= armour;
            }

            if (p.PType == PoisonType.None) return;

            for (int i = 0; i < PoisonList.Count; i++)
            {
                if (PoisonList[i].PType != p.PType) continue;
                if ((PoisonList[i].PType == PoisonType.Green) && (PoisonList[i].Value > p.Value)) return;//cant cast weak poison to cancel out strong poison
                if ((PoisonList[i].PType != PoisonType.Green) && ((PoisonList[i].Duration - PoisonList[i].Time) > p.Duration)) return;//cant cast 1 second poison to make a 1minute poison go away!
                if (p.PType == PoisonType.DelayedExplosion) return;
                if ((PoisonList[i].PType == PoisonType.Frozen) || (PoisonList[i].PType == PoisonType.Slow) || (PoisonList[i].PType == PoisonType.Paralysis)|| (PoisonList[i].PType == PoisonType.LRParalysis)) return;//prevents mobs from being perma frozen/slowed
                PoisonList[i] = p;
                return;
            }

            if (p.PType == PoisonType.DelayedExplosion)
            {
                ExplosionInflictedTime = Env.Time + 4000;
                Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.DelayedExplosion });
            }
            else if (p.PType == PoisonType.Dazed)
            {
                Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.Stunned, Time = (uint)(p.Duration * p.TickSpeed) });
            }
            else if (p.PType == PoisonType.Blindness)
            {
                var stats = new Stats
                {
                    [Stat.Accuracy] = p.Value * -1
                };

                AddBuff(BuffType.Blindness, Caster, (int)(p.Duration * p.TickSpeed), stats);
            }

            PoisonList.Add(p);
        }

        public override Buff AddBuff(BuffType type, MapObject owner, int duration, Stats stats, bool refreshStats = true, bool updateOnly = false, params int[] values)
        {
            Buff b = base.AddBuff(type, owner, duration, stats, refreshStats, updateOnly, values);

            var packet = new S.AddBuff
            {
                Buff = b.ToClientBuff(),
            };

            if (b.Info.Visible) Broadcast(packet);

            if (refreshStats)
            {
                RefreshAll();
            }

            return b;
        }

        public override Packet GetInfo()
        {
            return new S.ObjectMonster
            {
                ObjectID = ObjectID,
                Name = Name,
                NameColour = NameColour,
                Location = CurrentLocation,
                Image = Info.Image,
                Direction = Direction,
                Effect = Info.Effect,
                AI = Info.AI,
                Light = Info.Light,
                Dead = Dead,
                Skeleton = Harvested,
                Poison = CurrentPoison,
                Hidden = Hidden,
                ShockTime = (ShockTime > 0 ? ShockTime - Env.Time : 0),
                BindingShotCenter = BindingShotCenter,
                Buffs = [.. Buffs.Where(d => d.Info.Visible).Select(e => e.Type)],
                MasterObjectId = Master?.ObjectID ?? 0,
                Rarity= MonsterType
            };
        }

        public override void ReceiveChat(string text, ChatType type)
        {
            throw new NotSupportedException();
        }

        public void RemoveObjects(MirDirection dir, int count)
        {
            if (CurrentMap == null) return;
            
            switch (dir)
            {
                case MirDirection.Up:
                    //Bottom Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y + Globals.DataRange - a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid) continue;

                            foreach (var ob in cell.Objects.Where(ob => ob.Race == ObjectType.Player))
                            {
                                ob.Remove(this);
                            }
                        }
                    }
                    break;
                case MirDirection.UpRight:
                    //Bottom Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y + Globals.DataRange - a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid) continue;

                            foreach (var ob in cell.Objects.Where(ob => ob.Race == ObjectType.Player))
                            {
                                ob.Remove(this);
                            }
                        }
                    }

                    //Left Block
                    for (int a = -Globals.DataRange; a <= Globals.DataRange - count; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X - Globals.DataRange + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid) continue;

                            foreach (var ob in cell.Objects.Where(ob => ob.Race == ObjectType.Player))
                            {
                                ob.Remove(this);
                            }
                        }
                    }
                    break;
                case MirDirection.Right:
                    //Left Block
                    for (int a = -Globals.DataRange; a <= Globals.DataRange; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X - Globals.DataRange + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid) continue;

                            foreach (var ob in cell.Objects.Where(ob => ob.Race == ObjectType.Player))
                            {
                                ob.Remove(this);
                            }
                        }
                    }
                    break;
                case MirDirection.DownRight:
                    //Top Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y - Globals.DataRange + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid) continue;

                            foreach (var ob in cell.Objects.Where(ob => ob.Race == ObjectType.Player))
                            {
                                ob.Remove(this);
                            }
                        }
                    }

                    //Left Block
                    for (int a = -Globals.DataRange + count; a <= Globals.DataRange; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X - Globals.DataRange + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid) continue;

                            foreach (var ob in cell.Objects.Where(ob => ob.Race == ObjectType.Player))
                            {
                                ob.Remove(this);
                            }
                        }
                    }
                    break;
                case MirDirection.Down:
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y - Globals.DataRange + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid) continue;

                            foreach (var ob in cell.Objects.Where(ob => ob.Race == ObjectType.Player))
                            {
                                ob.Remove(this);
                            }
                        }
                    }
                    break;
                case MirDirection.DownLeft:
                    //Top Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y - Globals.DataRange + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid) continue;

                            foreach (var ob in cell.Objects.Where(ob => ob.Race == ObjectType.Player))
                            {
                                ob.Remove(this);
                            }
                        }
                    }

                    //Right Block
                    for (int a = -Globals.DataRange + count; a <= Globals.DataRange; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X + Globals.DataRange - b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid) continue;

                            foreach (var ob in cell.Objects.Where(ob => ob.Race == ObjectType.Player))
                            {
                                ob.Remove(this);
                            }
                        }
                    }
                    break;
                case MirDirection.Left:
                    for (int a = -Globals.DataRange; a <= Globals.DataRange; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X + Globals.DataRange - b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid) continue;

                            foreach (var ob in cell.Objects.Where(ob => ob.Race == ObjectType.Player))
                            {
                                ob.Remove(this);
                            }
                        }
                    }
                    break;
                case MirDirection.UpLeft:
                    //Bottom Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y + Globals.DataRange - a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid) continue;

                            foreach (var ob in cell.Objects.Where(ob => ob.Race == ObjectType.Player))
                            {
                                ob.Remove(this);
                            }
                        }
                    }

                    //Right Block
                    for (int a = -Globals.DataRange; a <= Globals.DataRange - count; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X + Globals.DataRange - b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid) continue;

                            foreach (var ob in cell.Objects.Where(ob => ob.Race == ObjectType.Player))
                            {
                                ob.Remove(this);
                            }
                        }
                    }
                    break;
            }
        }
        public void AddObjects(MirDirection dir, int count)
        {
            if (CurrentMap == null) return;
            
            switch (dir)
            {
                case MirDirection.Up:
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y - Globals.DataRange + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid) continue;

                            foreach (var ob in cell.Objects.Where(ob => ob.Race == ObjectType.Player))
                            {
                                ob.Add(this);
                            }
                        }
                    }
                    break;
                case MirDirection.UpRight:
                    //Top Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y - Globals.DataRange + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid) continue;

                            foreach (var ob in cell.Objects.Where(ob => ob.Race == ObjectType.Player))
                            {
                                ob.Add(this);
                            }
                        }
                    }

                    //Right Block
                    for (int a = -Globals.DataRange + count; a <= Globals.DataRange; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X + Globals.DataRange - b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid) continue;

                            foreach (var ob in cell.Objects.Where(ob => ob.Race == ObjectType.Player))
                            {
                                ob.Add(this);
                            }
                        }
                    }
                    break;
                case MirDirection.Right:
                    for (int a = -Globals.DataRange; a <= Globals.DataRange; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X + Globals.DataRange - b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid) continue;

                            foreach (var ob in cell.Objects.Where(ob => ob.Race == ObjectType.Player))
                            {
                                ob.Add(this);
                            }
                        }
                    }
                    break;
                case MirDirection.DownRight:
                    //Bottom Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y + Globals.DataRange - a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid) continue;

                            foreach (var ob in cell.Objects.Where(ob => ob.Race == ObjectType.Player))
                            {
                                ob.Add(this);
                            }
                        }
                    }

                    //Right Block
                    for (int a = -Globals.DataRange; a <= Globals.DataRange - count; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X + Globals.DataRange - b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid) continue;

                            foreach (var ob in cell.Objects.Where(ob => ob.Race == ObjectType.Player))
                            {
                                ob.Add(this);
                            }
                        }
                    }
                    break;
                case MirDirection.Down:
                    //Bottom Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y + Globals.DataRange - a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid || cell.Objects == null) continue;

                            for (int i = 0; i < cell.Objects.Count; i++)
                            {
                                MapObject ob = cell.Objects[i];
                                if (ob.Race != ObjectType.Player) continue;
                                ob.Add(this);
                            }
                        }
                    }
                    break;
                case MirDirection.DownLeft:
                    //Bottom Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y + Globals.DataRange - a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid || cell.Objects == null) continue;

                            for (int i = 0; i < cell.Objects.Count; i++)
                            {
                                MapObject ob = cell.Objects[i];
                                if (ob.Race != ObjectType.Player) continue;
                                ob.Add(this);
                            }
                        }
                    }

                    //Left Block
                    for (int a = -Globals.DataRange; a <= Globals.DataRange - count; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X - Globals.DataRange + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid) continue;

                            foreach (var ob in cell.Objects.Where(ob => ob.Race == ObjectType.Player))
                            {
                                ob.Add(this);
                            }
                        }
                    }
                    break;
                case MirDirection.Left:
                    //Left Block
                    for (int a = -Globals.DataRange; a <= Globals.DataRange; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X - Globals.DataRange + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid) continue;

                            foreach (var ob in cell.Objects.Where(ob => ob.Race == ObjectType.Player))
                            {
                                ob.Add(this);
                            }
                        }
                    }
                    break;
                case MirDirection.UpLeft:
                    //Top Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y - Globals.DataRange + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid) continue;

                            foreach (var ob in cell.Objects.Where(ob => ob.Race == ObjectType.Player))
                            {
                                ob.Add(this);
                            }
                        }
                    }

                    //Left Block
                    for (int a = -Globals.DataRange + count; a <= Globals.DataRange; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X - Globals.DataRange + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid) continue;

                            foreach (var ob in cell.Objects.Where(ob => ob.Race == ObjectType.Player))
                            {
                                ob.Add(this);
                            }
                        }
                    }
                    break;
            }
        }

        public override void Add(HumanObject player)
        {
            player.Enqueue(GetInfo());
            SendHealth(player);
        }

        public override void SendHealth(HumanObject player)
        {
            if (!player.IsMember(Master) && !(player.IsMember(EXPOwner) && AutoRev) && Env.Time > RevTime) return;
            byte time = Math.Min(byte.MaxValue, (byte)Math.Max(5, (RevTime - Env.Time) / 1000));
            player.Enqueue(new S.ObjectHealth { ObjectID = ObjectID, Percent = PercentHealth, Expire = time });
        }

        public void PetExp(uint amount)
        {
            if (PetLevel >= MaxPetLevel) return;

            if (Info.Name == Settings.SkeletonName || Info.Name == Settings.ShinsuName || Info.Name == Settings.AngelName)
                amount *= 3;

            PetExperience += amount;

            if (PetExperience < (PetLevel + 1) * 20000) return;

            PetExperience = (uint)(PetExperience - ((PetLevel + 1) * 20000));
            PetLevel++;
            RefreshAll();
            OperateTime = 0;
            BroadcastHealthChange();
        }
        
        public override void Despawn()
        {
            SlaveList.Clear();
            base.Despawn();
        }


        // MONSTER AI ATTACKS \\\
        protected virtual void PoisonTarget(MapObject target, int chanceToPoison, long poisonDuration, PoisonType poison, long poisonTickSpeed = 1000, bool noResist = false, bool ignoreDefence = true)
        {
            int value = GetAttackPower(Stats[Stat.MinSC], Stats[Stat.MaxSC]);

            if (RandomProvider.Next(Settings.PoisonResistWeight) >= target.Stats[Stat.PoisonResist])
            {
                if (RandomProvider.Next(chanceToPoison) == 0)
                {
                    target.ApplyPoison(new Poison { Owner = this, Duration = poisonDuration, PType = poison, Value = value, TickSpeed = poisonTickSpeed }, this, noResist, ignoreDefence);
                }
            }
        }

        protected virtual void TriangleAttack(int damage, int distance, int limitWidth = -1, int additionalDelay = 500, DefenceType defenceType = DefenceType.ACAgility, bool push = false)
        {
            if (CurrentMap == null) return;
            
            List<Point> points = [];

            for (int i = 1; i <= distance; i++)
            {
                Point target = Functions.PointMove(CurrentLocation, Direction, i);

                if (!CurrentMap.ValidPoint(target)) continue;

                points.Add(target);

                if (distance > 1)
                {
                    Point left = target;
                    Point right = target;

                    var offset = i - 1;

                    for (int l = 1; l <= offset; l++)
                    {
                        if (limitWidth > -1 && l > limitWidth) break;

                        left = Functions.Left(left, Direction);
                        if (!CurrentMap.ValidPoint(left)) continue;
                        points.Add(left);
                    }

                    for (int r = 1; r <= offset; r++)
                    {
                        if (limitWidth > -1 && r > limitWidth) break;

                        right = Functions.Right(right, Direction);
                        if (!CurrentMap.ValidPoint(right)) continue;
                        points.Add(right);
                    }
                }
            }

            foreach (var cell in points.Select(point => CurrentMap.GetCell(point)))
            {
                if (cell.Objects.FirstOrDefault(IsAttackableTarget) is { } ob)
                {
                    if (push)
                    {
                        var dir = Functions.DirectionFromPoint(CurrentLocation, ob.CurrentLocation);
                        ob.Pushed(this, dir, distance - 1);
                    }

                    int delay = Functions.MaxDistance(CurrentLocation, ob.CurrentLocation) * 50 + additionalDelay; //50 MS per Step
                    DelayedAction action = new DelayedAction(DelayedType.Damage, Env.Time + delay, ob, damage, defenceType);
                    ActionList.Add(action);
                }
            }
        }

        protected virtual void LineAttack(int damage, int distance, int additionalDelay = 500, DefenceType defenceType = DefenceType.ACAgility, bool push = false)
        {
            if (CurrentMap == null) return;
            
            for (int i = 1; i <= distance; i++)
            {
                Point target = Functions.PointMove(CurrentLocation, Direction, i);

                if (!CurrentMap.ValidPoint(target)) continue;

                Cell cell = CurrentMap.GetCell(target);
                if (cell.Objects.FirstOrDefault(IsAttackableTarget) is { } ob)
                {
                    if (push)
                    {
                        ob.Pushed(this, Direction, distance - 1);
                    }

                    int delay = Functions.MaxDistance(CurrentLocation, ob.CurrentLocation) * 50 + additionalDelay; //50 MS per Step
                    DelayedAction action = new DelayedAction(DelayedType.Damage, Env.Time + delay, ob, damage, defenceType);
                    ActionList.Add(action);
                    
                }
            }
        }

        protected virtual void WideLineAttack(int damage, int distance, int additionalDelay = 500, DefenceType defenceType = DefenceType.ACAgility, bool push = false, int width = 3)
        {
            if (CurrentMap == null) return;
            
            if (width <= 2)
            {
                width = 3;
            }

            var even = width % 2 == 0;

            if (even)
            {
                width--;
            }

            var startPoints = new List<Point>
            {
                CurrentLocation 
            };

            var half = (width - 1) / 2;

            var leftLoc = CurrentLocation;
            var rightLoc = CurrentLocation;

            for (int j = 0; j < half; j++)
            {
                leftLoc = Functions.Left(leftLoc, Direction);
                rightLoc = Functions.Right(rightLoc, Direction);

                startPoints.Add(leftLoc);
                startPoints.Add(rightLoc);
            }

            foreach (var point in startPoints)
            {
                for (int i = 1; i <= distance; i++)
                {
                    Point target = Functions.PointMove(point, Direction, i);

                    if (!CurrentMap.ValidPoint(target)) continue;

                    Cell cell = CurrentMap.GetCell(target);
                    if (cell.Objects.FirstOrDefault(IsAttackableTarget) is { } obj)
                    {
                        if (push)
                        {
                            obj.Pushed(this, Direction, distance - 1);
                        }

                        int delay = Functions.MaxDistance(CurrentLocation, obj.CurrentLocation) * 50 + additionalDelay; //50 MS per Step
                        DelayedAction action = new DelayedAction(DelayedType.Damage, Env.Time + delay, obj, damage, defenceType);
                        ActionList.Add(action);
                        
                    }
                }
            }
        }

        protected virtual void HalfmoonAttack(int damage, int delay = 500, DefenceType defenceType = DefenceType.ACAgility)
        {
            if (CurrentMap == null)  return;
            MirDirection dir = Functions.PreviousDir(Direction);

            for (int i = 0; i < 4; i++)
            {
                Point target = Functions.PointMove(CurrentLocation, dir, 1);
                dir = Functions.NextDir(dir);

                if (!CurrentMap.ValidPoint(target)) continue;

                Cell cell = CurrentMap.GetCell(target);
                if (cell.Objects.FirstOrDefault(IsAttackableTarget) is { } obj)
                {
                    DelayedAction action = new DelayedAction(DelayedType.Damage, Env.Time + delay, obj, damage, defenceType);
                    ActionList.Add(action);
                        
                }
            }
        }


        private bool IsAttackableTarget(MapObject target)
        {
            return target.Race is ObjectType.Player or ObjectType.Monster or ObjectType.Hero && target.IsAttackTarget(this);
        }
        
        protected virtual void ThreeQuarterMoonAttack(int damage, int delay = 500, DefenceType defenceType = DefenceType.ACAgility)
        {
            if (CurrentMap == null) return;
            
            MirDirection dir = Functions.PreviousDir(Direction);
            for (int i = 0; i < 6; i++)
            {
                Point target = Functions.PointMove(CurrentLocation, dir, 1);
                dir = Functions.NextDir(dir);

                if (!CurrentMap.ValidPoint(target)) continue;

                Cell cell = CurrentMap.GetCell(target);
                if (cell.Objects.FirstOrDefault(IsAttackableTarget) is { } ob)
                {
                    DelayedAction action = new DelayedAction(DelayedType.Damage, Env.Time + delay, ob, damage, defenceType);
                    ActionList.Add(action);
                    
                }
            }
        }
        protected virtual void JumpBack(int distance)
        {
            if (CurrentMap == null) return;
            
            MirDirection jumpDir = Functions.ReverseDirection(Direction);
            Point location = new Point();

            for (int i = 0; i < distance; i++)
            {
                location = Functions.PointMove(CurrentLocation, jumpDir, 1);
                if (!CurrentMap.ValidPoint(location)) return;
            }

            for (int i = 0; i < distance; i++)
            {
                location = Functions.PointMove(CurrentLocation, jumpDir, 1);

                CurrentMap.GetCell(CurrentLocation).Remove(this);
                RemoveObjects(jumpDir, 1);
                CurrentLocation = location;
                CurrentMap.GetCell(CurrentLocation).Add(this);
                AddObjects(jumpDir, 1);
            }

            Broadcast(new S.ObjectBackStep { ObjectID = ObjectID, Direction = Direction, Location = location, Distance = distance });
        }

        protected virtual void FullMoonAttack(int damage, int delay = 500, DefenceType defenceType = DefenceType.ACAgility, int pushDistance = -1, int distance = 1)
        {
            if (CurrentMap == null) return;
            
            MirDirection dir = Direction;
            bool pushed = false;

            for (int j = 1; j <= distance; j++)
            {
                for (int i = 0; i < 8; i++)
                {
                    dir = Functions.NextDir(dir);
                    Point point = Functions.PointMove(CurrentLocation, dir, j);

                    if (!CurrentMap.ValidPoint(point)) continue;

                    Cell cell = CurrentMap.GetCell(point);
                    if (cell.Objects.FirstOrDefault(IsAttackableTarget) is { } ob)
                    {
                        if (pushDistance > 0 && !pushed)
                        {
                            ob.Pushed(this, Direction, pushDistance);
                            pushed = true;
                        }

                        DelayedAction action = new DelayedAction(DelayedType.Damage, Env.Time + delay, ob, damage, defenceType);
                        ActionList.Add(action);
                    }
                }
            }     
        }
    
        protected virtual void ProjectileAttack(int damage, DefenceType type = DefenceType.ACAgility, int additionalDelay = 500)
        {
            if (Target == null) return;
            int delay = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation) * 50 + additionalDelay;
            DelayedAction action = new DelayedAction(DelayedType.RangeDamage, Env.Time + delay, Target, damage, type);
            ActionList.Add(action);
        }

        protected virtual void SinglePushAttack(int damage, DefenceType type = DefenceType.AC, int delay = 500, int pushDistance = 3)
        {
            //Repulsion - (utilises DelayedAction so player is hit at end of push)
            //need to put Damage Stats (DC/MC/SC) on mob for it to push
            if (Target == null) return;
            
            const int levelGap = 5;
            int mobLevel = this.Level;
            int targetLevel = Target.Level;

            if ((targetLevel <= mobLevel + levelGap))
            {
                if (Target.Pushed(this, Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation), pushDistance) > 0)
                {
                    AttackTime = Env.Time + AttackSpeed + 300;
                    if (damage == 0) return;

                    DelayedAction action = new DelayedAction(DelayedType.Damage, Env.Time + delay, Target, damage, type);
                    ActionList.Add(action);
                }
                else
                {
                    if (damage == 0) return;

                    DelayedAction action = new DelayedAction(DelayedType.Damage, Env.Time + delay, Target, damage, type);
                    ActionList.Add(action);
                }
            }
        }
    }
}