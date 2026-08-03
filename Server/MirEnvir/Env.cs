using ClientPackets;
using Server.Library.MirDatabase;
using Server.Library.Utils;
using Server.MirDatabase;
using Server.MirNetwork;
using Server.MirObjects;
using Server.MirObjects.Monsters;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text.RegularExpressions;
using static System.Int32;
using S = ServerPackets;

namespace Server.MirEnv
{
    public class MobThread
    {
        public int Id = 0;
        public long LastRunTime = 0;
        public long StartTime = 0;
        public long EndTime = 0;
        public readonly LinkedList<MapObject> ObjectsList = new LinkedList<MapObject>();
        public LinkedListNode<MapObject>? Current = null;
        public bool Stop = false;
    }
    
    
    public class RandomProvider
    {
        private static int seed = Environment.TickCount;
        private static readonly ThreadLocal<Random> RandomWrapper = new(() => new Random(Interlocked.Increment(ref seed)));

        public static Random GetThreadRandom() =>
            RandomWrapper.Value!;


        public static int Next(int maxValue) =>
            RandomWrapper.Value!.Next(maxValue);
        
        public static int Next(int minValue, int maxValue) =>
            RandomWrapper.Value!.Next(minValue, maxValue);
    }

    public class Env
    {
        public static Env Main { get; } = new Env();

        public static Env Edit { get; } = new Env();

        protected static MessageQueue MessageQueue => MessageQueue.Instance;

        public static object AccountLock = new object();
        public static object LoadLock = new object();

        public const int MinVersion = 60;
        public const int Version = 117;
        public const int CustomVersion = 0;
        public static readonly string DatabasePath = Path.Combine(".", "Server.MirDB");
        public static readonly string AccountPath = Path.Combine(".", "Server.MirADB");
        public static readonly string BackUpPath = Path.Combine(".", "Back Up");
        public static readonly string AccountsBackUpPath = Path.Combine(".", "Back Up", "Accounts");
        public static readonly string DatabaseBackUpPath = Path.Combine(".", "Back Up", "Database");
        public static readonly string ArchivePath = Path.Combine(".", "Archive");
        public bool ResetGS = false;
        public bool GuildRefreshNeeded;

        private static readonly Regex AccountIDReg, PasswordReg, EMailReg, CharacterReg;

        public static int LoadVersion;
        public static int LoadCustomVersion;

        private readonly DateTime _startTime = DateTime.UtcNow;
        public readonly Stopwatch Stopwatch = Stopwatch.StartNew();

        public long Time { get; private set; }
        public RespawnTimer RespawnTick = new RespawnTimer();

        private static List<string> DisabledCharNames = new List<string>();
        private static List<string> LineMessages = new List<string>();

        public static ConcurrentDictionary<string, DateTime> IPBlocks = new();

        public static ConcurrentDictionary<string, MirConnectionLog> ConnectionLogs = new();

        public DateTime Now =>
            _startTime.AddMilliseconds(Time);

        public bool Running { get; private set; }


        private static uint _objectID;
        public uint ObjectID => ++_objectID;

        public static int _playerCount;
        public int PlayerCount => Players.Count;

        public int[] OnlineRankingCount = new int[6];
        public int HeroCount => Heroes.Count;

        public RandomProvider Random = new RandomProvider();

        private Thread? _thread;
        private TcpListener _listener;
        private bool StatusPortEnabled = true;
        public List<MirStatusConnection> StatusConnections = new List<MirStatusConnection>();
        private TcpListener _StatusPort;
        private int _sessionID;
        public List<MirConnection> Connections = [];

        //Server DB
        public int MapIndex, ItemIndex, MonsterIndex, NPCIndex, QuestIndex, GameshopIndex, ConquestIndex, RespawnIndex, ScriptIndex;
        public List<MapInfo> MapInfoList = [];
        public List<ItemInfo> ItemInfoList = [];
        public List<MonsterInfo> MonsterInfoList = [];
        public List<MagicInfo> MagicInfoList = [];
        public List<NPCInfo> NPCInfoList = [];
        public DragonInfo DragonInfo = new DragonInfo();
        public List<QuestInfo> QuestInfoList = [];
        public List<GameShopItem> GameShopList = [];
        public List<RecipeInfo> RecipeInfoList = [];
        public List<BuffInfo> BuffInfoList = [];
        public List<ConquestInfo> ConquestInfoList = [];
        public List<GTMap> GTMapList = [];

        //User DB
        public int NextAccountID, NextCharacterID, NextGuildID, NextHeroID;
        public ulong NextUserItemID, NextAuctionID, NextMailID, NextRecipeID;
        public List<AccountInfo> AccountList = [];
        public List<CharacterInfo> CharacterList = [];
        public List<GuildInfo> GuildList = [];
        public LinkedList<AuctionInfo> Auctions = new();
        public List<ConquestGuildInfo> ConquestList = [];
        public Dictionary<int, int> GameshopLog = new();
        public List<HeroInfo> HeroList = [];

        public int GuildCount; //This shouldn't be needed?? -> remove in the future

        //Live Info
        public bool Saving = false;
        public List<Map> MapList = [];
        public List<SafeZoneInfo> StartPoints = [];
        public List<ItemInfo> StartItems = [];

        public List<PlayerObject> Players = [];
        public List<SpellObject> Spells = [];
        public List<NPCObject> NPCs = [];
        public List<GuildObject> Guilds = [];
        public List<ConquestObject> Conquests = [];
        public List<HeroObject> Heroes = [];

        public LightSetting Lights;
        public LinkedList<MapObject> Objects = new();
        public Dictionary<int, NPCScript> Scripts = new();
        public Dictionary<string, Timer> Timers = new();

        //multithread vars
        readonly object _locker = new object();
        public MobThread[] MobThreads = new MobThread[Settings.ThreadLimit];
        private readonly Thread[] MobThreading = new Thread[Settings.ThreadLimit];
        public int SpawnMultiplier = 1;//set this to 2 if you want double spawns (warning this can easily lag your server far beyond what you imagine)

        public List<string> CustomCommands = new List<string>();

        public Dragon? DragonSystem;
        public NPCScript DefaultNPC, MonsterNPC, RobotNPC;

        public List<DropInfo> FishingDrops = [];
        public List<DropInfo> AwakeningDrops = [];

        public List<DropInfo> StrongboxDrops = [];
        public List<DropInfo> BlackstoneDrops = [];

        public List<GuildAtWar> GuildsAtWar = [];
        public List<MapRespawn> SavedSpawns = [];

        public List<RankCharacterInfo> RankTop = [];
        public readonly List<RankCharacterInfo>?[] RankClass = new List<RankCharacterInfo>[5];

        static HttpServer http;

        static Env()
        {
            AccountIDReg = new Regex(@"^[A-Za-z0-9]{" + Globals.MinAccountIDLength + "," + Globals.MaxAccountIDLength + "}$");
            PasswordReg = new Regex(@"^[A-Za-z0-9]{" + Globals.MinPasswordLength + "," + Globals.MaxPasswordLength + "}$");
            EMailReg = new Regex(@"\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*");
            CharacterReg = new Regex(@"^[\u4e00-\u9fa5_A-Za-z0-9]{" + Globals.MinCharacterNameLength + "," + Globals.MaxCharacterNameLength + "}$");
        }

        public static bool IsPasswordValid(string password)
        {
            if (string.IsNullOrEmpty(password)) return false;

            return PasswordReg.IsMatch(password);
        }

        public static int LastCount = 0, LastRealCount = 0;
        public static long LastRunTime = 0;
        public int MonsterCount;

        private long warTime, guildTime, conquestTime, rentalItemsTime, auctionTime, spawnTime, robotTime, timerTime;
        private int dailyTime = DateTime.UtcNow.Day;
        
        private bool MagicExists(Spell spell)
        {
            return MagicInfoList.Any(t => t.Spell == spell);
        }

        private void UpdateMagicInfo()
        {
            foreach (var magicInfo in MagicInfoList)
            {
                switch (magicInfo.Spell)
                {
                    //warrior
                    case Spell.Thrusting:
                        magicInfo.MultiplierBase = 0.25f;
                        magicInfo.MultiplierBonus = 0.25f;
                        break;
                    case Spell.HalfMoon:
                        magicInfo.MultiplierBase = 0.3f;
                        magicInfo.MultiplierBonus = 0.1f;
                        break;
                    case Spell.ShoulderDash:
                        magicInfo.MPowerBase = 4;
                        break;
                    case Spell.TwinDrakeBlade:
                        magicInfo.MultiplierBase = 0.8f;
                        magicInfo.MultiplierBonus = 0.1f;
                        break;
                    case Spell.FlamingSword:
                        magicInfo.MultiplierBase = 1.4f;
                        magicInfo.MultiplierBonus = 0.4f;
                        break;
                    case Spell.CrossHalfMoon:
                        magicInfo.MultiplierBase = 0.4f;
                        magicInfo.MultiplierBonus = 0.1f;
                        break;
                    case Spell.BladeAvalanche:
                        magicInfo.MultiplierBase = 1f;
                        magicInfo.MultiplierBonus = 0.4f;
                        break;
                    case Spell.SlashingBurst:
                        magicInfo.MultiplierBase = 3.25f;
                        magicInfo.MultiplierBonus = 0.25f;
                        break;
                    //wiz
                    case Spell.Repulsion:
                        magicInfo.MPowerBase = 4;
                        break;
                    //tao
                    case Spell.Poisoning:
                        magicInfo.MPowerBase = 0;
                        break;
                    case Spell.Curse:
                        magicInfo.MPowerBase = 20;
                        break;
                    case Spell.Plague:
                        magicInfo.MPowerBase = 0;
                        magicInfo.PowerBase = 0;
                        break;
                    //sin
                    case Spell.FatalSword:
                        magicInfo.MPowerBase = 20;
                        break;
                    case Spell.DoubleSlash:
                        magicInfo.MultiplierBase = 0.8f;
                        magicInfo.MultiplierBonus = 0.1f;
                        break;
                    case Spell.FireBurst:
                        magicInfo.MPowerBase = 4;
                        break;
                    case Spell.MoonLight:
                    case Spell.DarkBody:
                        magicInfo.MPowerBase = 20;
                        break;
                    case Spell.Hemorrhage:
                        magicInfo.MultiplierBase = 0.2f;
                        magicInfo.MultiplierBonus = 0.05f;
                        break;
                    case Spell.CrescentSlash:
                        magicInfo.MultiplierBase = 1f;
                        magicInfo.MultiplierBonus = 0.4f;
                        break;
                    default:
                        break;
                    // throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void FillMagicInfoList()
        {
            //Warrior
            if (!MagicExists(Spell.Fencing))
                MagicInfoList.Add(new MagicInfo { Name = "Fencing", Spell = Spell.Fencing, Icon = 2, Level1 = 7, Level2 = 9, Level3 = 12, Need1 = 270, Need2 = 600, Need3 = 1300, Range = 0 });
            if (!MagicExists(Spell.Slaying))
                MagicInfoList.Add(new MagicInfo { Name = "Slaying", Spell = Spell.Slaying, Icon = 6, Level1 = 15, Level2 = 17, Level3 = 20, Need1 = 500, Need2 = 1100, Need3 = 1800, Range = 0 });
            if (!MagicExists(Spell.Thrusting))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "Thrusting",
                    Spell = Spell.Thrusting,
                    Icon = 11,
                    Level1 = 22,
                    Level2 = 24,
                    Level3 = 27,
                    Need1 = 2000,
                    Need2 = 3500,
                    Need3 = 6000,
                    Range = 0,
                    MultiplierBase = 0.25f,
                    MultiplierBonus = 0.25f
                });
            if (!MagicExists(Spell.HalfMoon))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "HalfMoon",
                    Spell = Spell.HalfMoon,
                    Icon = 24,
                    Level1 = 26,
                    Level2 = 28,
                    Level3 = 31,
                    Need1 = 5000,
                    Need2 = 8000,
                    Need3 = 14000,
                    BaseCost = 3,
                    Range = 0,
                    MultiplierBase = 0.3f,
                    MultiplierBonus = 0.1f
                });
            if (!MagicExists(Spell.ShoulderDash))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "ShoulderDash",
                    Spell = Spell.ShoulderDash,
                    Icon = 26,
                    Level1 = 30,
                    Level2 = 32,
                    Level3 = 34,
                    Need1 = 3000,
                    Need2 = 4000,
                    Need3 = 6000,
                    BaseCost = 4,
                    LevelCost = 4,
                    DelayBase = 2500,
                    Range = 0,
                    MPowerBase = 4
                });
            if (!MagicExists(Spell.TwinDrakeBlade))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "TwinDrakeBlade",
                    Spell = Spell.TwinDrakeBlade,
                    Icon = 37,
                    Level1 = 32,
                    Level2 = 34,
                    Level3 = 37,
                    Need1 = 4000,
                    Need2 = 6000,
                    Need3 = 10000,
                    BaseCost = 10,
                    Range = 0,
                    MultiplierBase = 0.8f,
                    MultiplierBonus = 0.1f
                });
            if (!MagicExists(Spell.Entrapment))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "Entrapment",
                    Spell = Spell.Entrapment,
                    Icon = 46,
                    Level1 = 32,
                    Level2 = 35,
                    Level3 = 37,
                    Need1 = 2000,
                    Need2 = 3500,
                    Need3 = 5500,
                    BaseCost = 15,
                    LevelCost = 3,
                    Range = 9
                });
            if (!MagicExists(Spell.FlamingSword))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "FlamingSword",
                    Spell = Spell.FlamingSword,
                    Icon = 25,
                    Level1 = 35,
                    Level2 = 37,
                    Level3 = 40,
                    Need1 = 2000,
                    Need2 = 4000,
                    Need3 = 6000,
                    BaseCost = 7,
                    Range = 0,
                    MultiplierBase = 1.4f,
                    MultiplierBonus = 0.4f
                });
            if (!MagicExists(Spell.LionRoar))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "LionRoar",
                    Spell = Spell.LionRoar,
                    Icon = 42,
                    Level1 = 36,
                    Level2 = 39,
                    Level3 = 41,
                    Need1 = 5000,
                    Need2 = 8000,
                    Need3 = 12000,
                    BaseCost = 14,
                    LevelCost = 4,
                    Range = 0
                });
            if (!MagicExists(Spell.CrossHalfMoon))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "CrossHalfMoon",
                    Spell = Spell.CrossHalfMoon,
                    Icon = 33,
                    Level1 = 38,
                    Level2 = 40,
                    Level3 = 42,
                    Need1 = 7000,
                    Need2 = 11000,
                    Need3 = 16000,
                    BaseCost = 6,
                    Range = 0,
                    MultiplierBase = 0.4f,
                    MultiplierBonus = 0.1f
                });
            if (!MagicExists(Spell.BladeAvalanche))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "BladeAvalanche",
                    Spell = Spell.BladeAvalanche,
                    Icon = 43,
                    Level1 = 38,
                    Level2 = 41,
                    Level3 = 43,
                    Need1 = 5000,
                    Need2 = 8000,
                    Need3 = 12000,
                    BaseCost = 14,
                    LevelCost = 4,
                    Range = 0,
                    MultiplierBonus = 0.3f
                });
            if (!MagicExists(Spell.ProtectionField))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "ProtectionField",
                    Spell = Spell.ProtectionField,
                    Icon = 50,
                    Level1 = 39,
                    Level2 = 42,
                    Level3 = 45,
                    Need1 = 6000,
                    Need2 = 12000,
                    Need3 = 18000,
                    BaseCost = 23,
                    LevelCost = 6,
                    Range = 0
                });
            if (!MagicExists(Spell.Rage))
                MagicInfoList.Add(new MagicInfo
                    { Name = "Rage", Spell = Spell.Rage, Icon = 49, Level1 = 44, Level2 = 47, Level3 = 50, Need1 = 8000, Need2 = 14000, Need3 = 20000, BaseCost = 20, LevelCost = 5, Range = 0 });
            if (!MagicExists(Spell.CounterAttack))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "CounterAttack",
                    Spell = Spell.CounterAttack,
                    Icon = 72,
                    Level1 = 47,
                    Level2 = 51,
                    Level3 = 55,
                    Need1 = 7000,
                    Need2 = 11000,
                    Need3 = 15000,
                    BaseCost = 12,
                    LevelCost = 4,
                    DelayBase = 24000,
                    Range = 0,
                    MultiplierBonus = 0.4f
                });
            if (!MagicExists(Spell.SlashingBurst))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "SlashingBurst",
                    Spell = Spell.SlashingBurst,
                    Icon = 55,
                    Level1 = 50,
                    Level2 = 53,
                    Level3 = 56,
                    Need1 = 10000,
                    Need2 = 16000,
                    Need3 = 24000,
                    BaseCost = 25,
                    LevelCost = 4,
                    MPowerBase = 1,
                    PowerBase = 3,
                    DelayBase = 14000,
                    DelayReduction = 4000,
                    Range = 0,
                    MultiplierBase = 3.25f,
                    MultiplierBonus = 0.25f
                });
            if (!MagicExists(Spell.Fury))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "Fury",
                    Spell = Spell.Fury,
                    Icon = 76,
                    Level1 = 45,
                    Level2 = 48,
                    Level3 = 51,
                    Need1 = 8000,
                    Need2 = 14000,
                    Need3 = 20000,
                    BaseCost = 10,
                    LevelCost = 4,
                    DelayBase = 600000,
                    DelayReduction = 120000,
                    Range = 0
                });
            if (!MagicExists(Spell.ImmortalSkin))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "ImmortalSkin",
                    Spell = Spell.ImmortalSkin,
                    Icon = 80,
                    Level1 = 60,
                    Level2 = 61,
                    Level3 = 62,
                    Need1 = 1560,
                    Need2 = 2200,
                    Need3 = 3000,
                    BaseCost = 10,
                    LevelCost = 4,
                    DelayBase = 600000,
                    DelayReduction = 120000,
                    Range = 0
                });

            //Wizard
            if (!MagicExists(Spell.FireBall))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "FireBall",
                    Spell = Spell.FireBall,
                    Icon = 0,
                    Level1 = 7,
                    Level2 = 9,
                    Level3 = 11,
                    Need1 = 200,
                    Need2 = 350,
                    Need3 = 700,
                    BaseCost = 3,
                    LevelCost = 2,
                    MPowerBase = 8,
                    PowerBase = 2,
                    Range = 9
                });
            if (!MagicExists(Spell.Repulsion))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "Repulsion",
                    Spell = Spell.Repulsion,
                    Icon = 7,
                    Level1 = 12,
                    Level2 = 15,
                    Level3 = 19,
                    Need1 = 500,
                    Need2 = 1300,
                    Need3 = 2200,
                    BaseCost = 2,
                    LevelCost = 2,
                    Range = 0,
                    MPowerBase = 4
                });
            if (!MagicExists(Spell.ElectricShock))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "ElectricShock",
                    Spell = Spell.ElectricShock,
                    Icon = 19,
                    Level1 = 13,
                    Level2 = 18,
                    Level3 = 24,
                    Need1 = 530,
                    Need2 = 1100,
                    Need3 = 2200,
                    BaseCost = 3,
                    LevelCost = 1,
                    Range = 9
                });
            if (!MagicExists(Spell.GreatFireBall))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "GreatFireBall",
                    Spell = Spell.GreatFireBall,
                    Icon = 4,
                    Level1 = 15,
                    Level2 = 18,
                    Level3 = 21,
                    Need1 = 2000,
                    Need2 = 2700,
                    Need3 = 3500,
                    BaseCost = 5,
                    LevelCost = 1,
                    MPowerBase = 6,
                    PowerBase = 10,
                    Range = 9
                });
            if (!MagicExists(Spell.HellFire))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "HellFire",
                    Spell = Spell.HellFire,
                    Icon = 8,
                    Level1 = 16,
                    Level2 = 20,
                    Level3 = 24,
                    Need1 = 700,
                    Need2 = 2700,
                    Need3 = 3500,
                    BaseCost = 10,
                    LevelCost = 3,
                    MPowerBase = 14,
                    PowerBase = 6,
                    Range = 0
                });
            if (!MagicExists(Spell.ThunderBolt))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "ThunderBolt",
                    Spell = Spell.ThunderBolt,
                    Icon = 10,
                    Level1 = 17,
                    Level2 = 20,
                    Level3 = 23,
                    Need1 = 500,
                    Need2 = 2000,
                    Need3 = 3500,
                    BaseCost = 9,
                    LevelCost = 2,
                    MPowerBase = 8,
                    MPowerBonus = 20,
                    PowerBase = 9,
                    Range = 9
                });
            if (!MagicExists(Spell.Teleport))
                MagicInfoList.Add(new MagicInfo
                    { Name = "Teleport", Spell = Spell.Teleport, Icon = 20, Level1 = 19, Level2 = 22, Level3 = 25, Need1 = 350, Need2 = 1000, Need3 = 2000, BaseCost = 10, LevelCost = 3, Range = 0 });
            if (!MagicExists(Spell.FireBang))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "FireBang",
                    Spell = Spell.FireBang,
                    Icon = 22,
                    Level1 = 22,
                    Level2 = 25,
                    Level3 = 28,
                    Need1 = 3000,
                    Need2 = 5000,
                    Need3 = 10000,
                    BaseCost = 14,
                    LevelCost = 4,
                    MPowerBase = 8,
                    PowerBase = 8,
                    Range = 9
                });
            if (!MagicExists(Spell.FireWall))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "FireWall",
                    Spell = Spell.FireWall,
                    Icon = 21,
                    Level1 = 24,
                    Level2 = 28,
                    Level3 = 33,
                    Need1 = 4000,
                    Need2 = 10000,
                    Need3 = 20000,
                    BaseCost = 30,
                    LevelCost = 5,
                    MPowerBase = 3,
                    PowerBase = 3,
                    Range = 9
                });
            if (!MagicExists(Spell.Lightning))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "Lightning",
                    Spell = Spell.Lightning,
                    Icon = 9,
                    Level1 = 26,
                    Level2 = 29,
                    Level3 = 32,
                    Need1 = 3000,
                    Need2 = 6000,
                    Need3 = 12000,
                    BaseCost = 38,
                    LevelCost = 7,
                    MPowerBase = 12,
                    PowerBase = 12,
                    Range = 0
                });
            if (!MagicExists(Spell.FrostCrunch))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "FrostCrunch",
                    Spell = Spell.FrostCrunch,
                    Icon = 38,
                    Level1 = 28,
                    Level2 = 30,
                    Level3 = 33,
                    Need1 = 3000,
                    Need2 = 5000,
                    Need3 = 8000,
                    BaseCost = 15,
                    LevelCost = 3,
                    MPowerBase = 12,
                    PowerBase = 12,
                    Range = 9
                });
            if (!MagicExists(Spell.ThunderStorm))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "ThunderStorm",
                    Spell = Spell.ThunderStorm,
                    Icon = 23,
                    Level1 = 30,
                    Level2 = 32,
                    Level3 = 34,
                    Need1 = 4000,
                    Need2 = 8000,
                    Need3 = 12000,
                    BaseCost = 29,
                    LevelCost = 9,
                    MPowerBase = 10,
                    MPowerBonus = 20,
                    PowerBase = 10,
                    PowerBonus = 20,
                    Range = 0
                });
            if (!MagicExists(Spell.MagicShield))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "MagicShield",
                    Spell = Spell.MagicShield,
                    Icon = 30,
                    Level1 = 31,
                    Level2 = 34,
                    Level3 = 38,
                    Need1 = 3000,
                    Need2 = 7000,
                    Need3 = 10000,
                    BaseCost = 35,
                    LevelCost = 5,
                    Range = 0
                });
            if (!MagicExists(Spell.TurnUndead))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "TurnUndead",
                    Spell = Spell.TurnUndead,
                    Icon = 31,
                    Level1 = 32,
                    Level2 = 35,
                    Level3 = 39,
                    Need1 = 3000,
                    Need2 = 7000,
                    Need3 = 10000,
                    BaseCost = 52,
                    LevelCost = 13,
                    Range = 9
                });
            if (!MagicExists(Spell.Vampirism))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "Vampirism",
                    Spell = Spell.Vampirism,
                    Icon = 47,
                    Level1 = 33,
                    Level2 = 36,
                    Level3 = 40,
                    Need1 = 3000,
                    Need2 = 5000,
                    Need3 = 8000,
                    BaseCost = 26,
                    LevelCost = 13,
                    MPowerBase = 12,
                    PowerBase = 12,
                    Range = 9
                });
            if (!MagicExists(Spell.IceStorm))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "IceStorm",
                    Spell = Spell.IceStorm,
                    Icon = 32,
                    Level1 = 35,
                    Level2 = 37,
                    Level3 = 40,
                    Need1 = 4000,
                    Need2 = 8000,
                    Need3 = 12000,
                    BaseCost = 33,
                    LevelCost = 3,
                    MPowerBase = 12,
                    PowerBase = 14,
                    Range = 9
                });
            if (!MagicExists(Spell.FlameDisruptor))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "FlameDisruptor",
                    Spell = Spell.FlameDisruptor,
                    Icon = 34,
                    Level1 = 38,
                    Level2 = 40,
                    Level3 = 42,
                    Need1 = 5000,
                    Need2 = 9000,
                    Need3 = 14000,
                    BaseCost = 28,
                    LevelCost = 3,
                    MPowerBase = 15,
                    MPowerBonus = 20,
                    PowerBase = 9,
                    Range = 9
                });
            if (!MagicExists(Spell.Mirroring))
                MagicInfoList.Add(new MagicInfo
                    { Name = "Mirroring", Spell = Spell.Mirroring, Icon = 41, Level1 = 41, Level2 = 43, Level3 = 45, Need1 = 6000, Need2 = 11000, Need3 = 16000, BaseCost = 21, Range = 0 });
            if (!MagicExists(Spell.FlameField))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "FlameField",
                    Spell = Spell.FlameField,
                    Icon = 44,
                    Level1 = 42,
                    Level2 = 43,
                    Level3 = 45,
                    Need1 = 6000,
                    Need2 = 11000,
                    Need3 = 16000,
                    BaseCost = 45,
                    LevelCost = 8,
                    MPowerBase = 100,
                    PowerBase = 25,
                    Range = 9
                });
            if (!MagicExists(Spell.Blizzard))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "Blizzard",
                    Spell = Spell.Blizzard,
                    Icon = 51,
                    Level1 = 44,
                    Level2 = 47,
                    Level3 = 50,
                    Need1 = 8000,
                    Need2 = 16000,
                    Need3 = 24000,
                    BaseCost = 65,
                    LevelCost = 10,
                    MPowerBase = 30,
                    MPowerBonus = 10,
                    PowerBase = 20,
                    PowerBonus = 5,
                    Range = 9
                });
            if (!MagicExists(Spell.MagicBooster))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "MagicBooster",
                    Spell = Spell.MagicBooster,
                    Icon = 73,
                    Level1 = 47,
                    Level2 = 49,
                    Level3 = 52,
                    Need1 = 12000,
                    Need2 = 18000,
                    Need3 = 24000,
                    BaseCost = 150,
                    LevelCost = 15,
                    Range = 0
                });
            if (!MagicExists(Spell.MeteorStrike))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "MeteorStrike",
                    Spell = Spell.MeteorStrike,
                    Icon = 52,
                    Level1 = 49,
                    Level2 = 52,
                    Level3 = 55,
                    Need1 = 15000,
                    Need2 = 20000,
                    Need3 = 25000,
                    BaseCost = 115,
                    LevelCost = 17,
                    MPowerBase = 40,
                    MPowerBonus = 10,
                    PowerBase = 20,
                    PowerBonus = 15,
                    Range = 9
                });
            if (!MagicExists(Spell.IceThrust))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "IceThrust",
                    Spell = Spell.IceThrust,
                    Icon = 56,
                    Level1 = 53,
                    Level2 = 56,
                    Level3 = 59,
                    Need1 = 17000,
                    Need2 = 22000,
                    Need3 = 27000,
                    BaseCost = 100,
                    LevelCost = 20,
                    MPowerBase = 100,
                    PowerBase = 50,
                    Range = 0
                });
            if (!MagicExists(Spell.Blink))
                MagicInfoList.Add(new MagicInfo
                    { Name = "Blink", Spell = Spell.Blink, Icon = 20, Level1 = 19, Level2 = 22, Level3 = 25, Need1 = 350, Need2 = 1000, Need3 = 2000, BaseCost = 10, LevelCost = 3, Range = 9 });
            //if (!MagicExists(Spell.FastMove)) MagicInfoList.Add(new MagicInfo { Name = "FastMove", Spell = Spell.ImmortalSkin, Icon = ?, Level1 = ?, Level2 = ?, Level3 = ?, Need1 = ?, Need2 = ?, Need3 = ?, BaseCost = ?, LevelCost = ?, DelayBase = ?, DelayReduction = ? });
            if (!MagicExists(Spell.StormEscape))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "StormEscape",
                    Spell = Spell.StormEscape,
                    Icon = 23,
                    Level1 = 60,
                    Level2 = 61,
                    Level3 = 62,
                    Need1 = 2200,
                    Need2 = 3300,
                    Need3 = 4400,
                    BaseCost = 65,
                    LevelCost = 8,
                    MPowerBase = 12,
                    PowerBase = 4,
                    Range = 9
                });


            //Taoist
            if (!MagicExists(Spell.Healing))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "Healing",
                    Spell = Spell.Healing,
                    Icon = 1,
                    Level1 = 7,
                    Level2 = 11,
                    Level3 = 14,
                    Need1 = 150,
                    Need2 = 350,
                    Need3 = 700,
                    BaseCost = 3,
                    LevelCost = 2,
                    MPowerBase = 14,
                    Range = 9
                });
            if (!MagicExists(Spell.SpiritSword))
                MagicInfoList.Add(new MagicInfo
                    { Name = "SpiritSword", Spell = Spell.SpiritSword, Icon = 3, Level1 = 9, Level2 = 12, Level3 = 15, Need1 = 350, Need2 = 1300, Need3 = 2700, Range = 0 });
            if (!MagicExists(Spell.Poisoning))
                MagicInfoList.Add(new MagicInfo
                    { Name = "Poisoning", Spell = Spell.Poisoning, Icon = 5, Level1 = 14, Level2 = 17, Level3 = 20, Need1 = 700, Need2 = 1300, Need3 = 2700, BaseCost = 2, LevelCost = 1, Range = 9 });
            if (!MagicExists(Spell.SoulFireBall))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "SoulFireBall",
                    Spell = Spell.SoulFireBall,
                    Icon = 12,
                    Level1 = 18,
                    Level2 = 21,
                    Level3 = 24,
                    Need1 = 1300,
                    Need2 = 2700,
                    Need3 = 4000,
                    BaseCost = 3,
                    LevelCost = 1,
                    MPowerBase = 8,
                    PowerBase = 3,
                    Range = 9
                });
            if (!MagicExists(Spell.SummonSkeleton))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "SummonSkeleton",
                    Spell = Spell.SummonSkeleton,
                    Icon = 16,
                    Level1 = 19,
                    Level2 = 22,
                    Level3 = 26,
                    Need1 = 1000,
                    Need2 = 2000,
                    Need3 = 3500,
                    BaseCost = 12,
                    LevelCost = 4,
                    Range = 0
                });
            if (!MagicExists(Spell.Hiding))
                MagicInfoList.Add(new MagicInfo
                    { Name = "Hiding", Spell = Spell.Hiding, Icon = 17, Level1 = 20, Level2 = 23, Level3 = 26, Need1 = 1300, Need2 = 2700, Need3 = 5300, BaseCost = 1, LevelCost = 1, Range = 0 });
            if (!MagicExists(Spell.MassHiding))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "MassHiding",
                    Spell = Spell.MassHiding,
                    Icon = 18,
                    Level1 = 21,
                    Level2 = 25,
                    Level3 = 29,
                    Need1 = 1300,
                    Need2 = 2700,
                    Need3 = 5300,
                    BaseCost = 2,
                    LevelCost = 2,
                    Range = 9
                });
            if (!MagicExists(Spell.SoulShield))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "SoulShield",
                    Spell = Spell.SoulShield,
                    Icon = 13,
                    Level1 = 22,
                    Level2 = 24,
                    Level3 = 26,
                    Need1 = 2000,
                    Need2 = 3500,
                    Need3 = 7000,
                    BaseCost = 2,
                    LevelCost = 2,
                    Range = 9
                });
            if (!MagicExists(Spell.Revelation))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "Revelation",
                    Spell = Spell.Revelation,
                    Icon = 27,
                    Level1 = 23,
                    Level2 = 25,
                    Level3 = 28,
                    Need1 = 1500,
                    Need2 = 2500,
                    Need3 = 4000,
                    BaseCost = 4,
                    LevelCost = 4,
                    Range = 9
                });
            if (!MagicExists(Spell.BlessedArmour))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "BlessedArmour",
                    Spell = Spell.BlessedArmour,
                    Icon = 14,
                    Level1 = 25,
                    Level2 = 27,
                    Level3 = 29,
                    Need1 = 4000,
                    Need2 = 6000,
                    Need3 = 10000,
                    BaseCost = 2,
                    LevelCost = 2,
                    Range = 9
                });
            if (!MagicExists(Spell.EnergyRepulsor))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "EnergyRepulsor",
                    Spell = Spell.EnergyRepulsor,
                    Icon = 36,
                    Level1 = 27,
                    Level2 = 29,
                    Level3 = 31,
                    Need1 = 1800,
                    Need2 = 2400,
                    Need3 = 3200,
                    BaseCost = 2,
                    LevelCost = 2,
                    Range = 0,
                    MPowerBase = 4
                });
            if (!MagicExists(Spell.TrapHexagon))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "TrapHexagon",
                    Spell = Spell.TrapHexagon,
                    Icon = 15,
                    Level1 = 28,
                    Level2 = 30,
                    Level3 = 32,
                    Need1 = 2500,
                    Need2 = 5000,
                    Need3 = 10000,
                    BaseCost = 7,
                    LevelCost = 3,
                    Range = 9
                });
            if (!MagicExists(Spell.Purification))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "Purification",
                    Spell = Spell.Purification,
                    Icon = 39,
                    Level1 = 30,
                    Level2 = 32,
                    Level3 = 35,
                    Need1 = 3000,
                    Need2 = 5000,
                    Need3 = 8000,
                    BaseCost = 14,
                    LevelCost = 2,
                    Range = 9
                });
            if (!MagicExists(Spell.MassHealing))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "MassHealing",
                    Spell = Spell.MassHealing,
                    Icon = 28,
                    Level1 = 31,
                    Level2 = 33,
                    Level3 = 36,
                    Need1 = 2000,
                    Need2 = 4000,
                    Need3 = 8000,
                    BaseCost = 28,
                    LevelCost = 3,
                    MPowerBase = 10,
                    PowerBase = 4,
                    Range = 9
                });
            if (!MagicExists(Spell.Hallucination))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "Hallucination",
                    Spell = Spell.Hallucination,
                    Icon = 48,
                    Level1 = 31,
                    Level2 = 34,
                    Level3 = 36,
                    Need1 = 4000,
                    Need2 = 6000,
                    Need3 = 9000,
                    BaseCost = 22,
                    LevelCost = 10,
                    Range = 9
                });
            if (!MagicExists(Spell.UltimateEnhancer))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "UltimateEnchancer",
                    Spell = Spell.UltimateEnhancer,
                    Icon = 35,
                    Level1 = 33,
                    Level2 = 35,
                    Level3 = 38,
                    Need1 = 5000,
                    Need2 = 7000,
                    Need3 = 10000,
                    BaseCost = 28,
                    LevelCost = 4,
                    Range = 9
                });
            if (!MagicExists(Spell.SummonShinsu))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "SummonShinsu",
                    Spell = Spell.SummonShinsu,
                    Icon = 29,
                    Level1 = 35,
                    Level2 = 37,
                    Level3 = 40,
                    Need1 = 2000,
                    Need2 = 4000,
                    Need3 = 6000,
                    BaseCost = 28,
                    LevelCost = 4,
                    Range = 0
                });
            if (!MagicExists(Spell.Reincarnation))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "Reincarnation",
                    Spell = Spell.Reincarnation,
                    Icon = 53,
                    Level1 = 37,
                    Level2 = 39,
                    Level3 = 41,
                    Need1 = 2000,
                    Need2 = 6000,
                    Need3 = 10000,
                    BaseCost = 125,
                    LevelCost = 17,
                    Range = 9
                });
            if (!MagicExists(Spell.SummonHolyDeva))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "SummonHolyDeva",
                    Spell = Spell.SummonHolyDeva,
                    Icon = 40,
                    Level1 = 38,
                    Level2 = 41,
                    Level3 = 43,
                    Need1 = 4000,
                    Need2 = 6000,
                    Need3 = 9000,
                    BaseCost = 28,
                    LevelCost = 4,
                    Range = 0
                });
            if (!MagicExists(Spell.Curse))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "Curse",
                    Spell = Spell.Curse,
                    Icon = 45,
                    Level1 = 40,
                    Level2 = 42,
                    Level3 = 44,
                    Need1 = 4000,
                    Need2 = 6000,
                    Need3 = 9000,
                    BaseCost = 17,
                    LevelCost = 3,
                    Range = 9,
                    MPowerBase = 20
                });
            if (!MagicExists(Spell.Plague))
                MagicInfoList.Add(new MagicInfo
                    { Name = "Plague", Spell = Spell.Plague, Icon = 74, Level1 = 42, Level2 = 44, Level3 = 47, Need1 = 5000, Need2 = 9000, Need3 = 13000, BaseCost = 20, LevelCost = 5, Range = 9 });
            if (!MagicExists(Spell.PoisonCloud))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "PoisonCloud",
                    Spell = Spell.PoisonCloud,
                    Icon = 54,
                    Level1 = 43,
                    Level2 = 45,
                    Level3 = 48,
                    Need1 = 4000,
                    Need2 = 8000,
                    Need3 = 12000,
                    BaseCost = 30,
                    LevelCost = 5,
                    MPowerBase = 40,
                    PowerBase = 20,
                    DelayBase = 18000,
                    DelayReduction = 2000,
                    Range = 9
                });
            if (!MagicExists(Spell.EnergyShield))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "EnergyShield",
                    Spell = Spell.EnergyShield,
                    Icon = 57,
                    Level1 = 48,
                    Level2 = 51,
                    Level3 = 54,
                    Need1 = 5000,
                    Need2 = 9000,
                    Need3 = 13000,
                    BaseCost = 50,
                    LevelCost = 20,
                    Range = 9
                });
            if (!MagicExists(Spell.PetEnhancer))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "PetEnhancer",
                    Spell = Spell.PetEnhancer,
                    Icon = 78,
                    Level1 = 45,
                    Level2 = 48,
                    Level3 = 51,
                    Need1 = 4000,
                    Need2 = 8000,
                    Need3 = 12000,
                    BaseCost = 30,
                    LevelCost = 40,
                    Range = 0
                });
            if (!MagicExists(Spell.HealingCircle))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "HealingCircle",
                    Spell = Spell.HealingCircle,
                    Icon = 82,
                    Level1 = 39,
                    Level2 = 41,
                    Level3 = 43,
                    Need1 = 7000,
                    Need2 = 12000,
                    Need3 = 15000,
                    BaseCost = 10,
                    LevelCost = 100
                });
            //Assassin
            if (!MagicExists(Spell.FatalSword))
                MagicInfoList.Add(new MagicInfo { Name = "FatalSword", Spell = Spell.FatalSword, Icon = 58, Level1 = 7, Level2 = 9, Level3 = 12, Need1 = 500, Need2 = 1000, Need3 = 2300, Range = 0 });
            if (!MagicExists(Spell.DoubleSlash))
                MagicInfoList.Add(new MagicInfo
                    { Name = "DoubleSlash", Spell = Spell.DoubleSlash, Icon = 59, Level1 = 15, Level2 = 17, Level3 = 19, Need1 = 700, Need2 = 1500, Need3 = 2200, BaseCost = 2, LevelCost = 1 });
            if (!MagicExists(Spell.Haste))
                MagicInfoList.Add(new MagicInfo
                    { Name = "Haste", Spell = Spell.Haste, Icon = 60, Level1 = 20, Level2 = 22, Level3 = 25, Need1 = 2000, Need2 = 3000, Need3 = 6000, BaseCost = 3, LevelCost = 2, Range = 0 });
            if (!MagicExists(Spell.FlashDash))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "FlashDash",
                    Spell = Spell.FlashDash,
                    Icon = 61,
                    Level1 = 25,
                    Level2 = 27,
                    Level3 = 30,
                    Need1 = 4000,
                    Need2 = 7000,
                    Need3 = 9000,
                    BaseCost = 12,
                    LevelCost = 2,
                    DelayBase = 200,
                    Range = 0
                });
            if (!MagicExists(Spell.LightBody))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "LightBody",
                    Spell = Spell.LightBody,
                    Icon = 68,
                    Level1 = 27,
                    Level2 = 29,
                    Level3 = 32,
                    Need1 = 5000,
                    Need2 = 7000,
                    Need3 = 10000,
                    BaseCost = 11,
                    LevelCost = 2,
                    Range = 0
                });
            if (!MagicExists(Spell.HeavenlySword))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "HeavenlySword",
                    Spell = Spell.HeavenlySword,
                    Icon = 62,
                    Level1 = 30,
                    Level2 = 32,
                    Level3 = 35,
                    Need1 = 4000,
                    Need2 = 8000,
                    Need3 = 10000,
                    BaseCost = 13,
                    LevelCost = 2,
                    MPowerBase = 8,
                    Range = 0
                });
            if (!MagicExists(Spell.FireBurst))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "FireBurst",
                    Spell = Spell.FireBurst,
                    Icon = 63,
                    Level1 = 33,
                    Level2 = 35,
                    Level3 = 38,
                    Need1 = 4000,
                    Need2 = 6000,
                    Need3 = 8000,
                    BaseCost = 10,
                    LevelCost = 1,
                    Range = 0
                });
            if (!MagicExists(Spell.Trap))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "Trap",
                    Spell = Spell.Trap,
                    Icon = 64,
                    Level1 = 33,
                    Level2 = 35,
                    Level3 = 38,
                    Need1 = 2000,
                    Need2 = 4000,
                    Need3 = 6000,
                    BaseCost = 14,
                    LevelCost = 2,
                    DelayBase = 60000,
                    DelayReduction = 15000,
                    Range = 9
                });
            if (!MagicExists(Spell.PoisonSword))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "PoisonSword",
                    Spell = Spell.PoisonSword,
                    Icon = 69,
                    Level1 = 34,
                    Level2 = 36,
                    Level3 = 39,
                    Need1 = 5000,
                    Need2 = 8000,
                    Need3 = 11000,
                    BaseCost = 14,
                    LevelCost = 3,
                    Range = 0
                });
            if (!MagicExists(Spell.MoonLight))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "MoonLight",
                    Spell = Spell.MoonLight,
                    Icon = 65,
                    Level1 = 36,
                    Level2 = 39,
                    Level3 = 42,
                    Need1 = 3000,
                    Need2 = 5000,
                    Need3 = 8000,
                    BaseCost = 36,
                    LevelCost = 3,
                    Range = 0
                });
            if (!MagicExists(Spell.MPEater))
                MagicInfoList.Add(new MagicInfo { Name = "MPEater", Spell = Spell.MPEater, Icon = 66, Level1 = 38, Level2 = 41, Level3 = 44, Need1 = 5000, Need2 = 8000, Need3 = 11000, Range = 0 });
            if (!MagicExists(Spell.SwiftFeet))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "SwiftFeet",
                    Spell = Spell.SwiftFeet,
                    Icon = 67,
                    Level1 = 40,
                    Level2 = 43,
                    Level3 = 46,
                    Need1 = 4000,
                    Need2 = 6000,
                    Need3 = 9000,
                    BaseCost = 17,
                    LevelCost = 5,
                    DelayBase = 210000,
                    DelayReduction = 40000,
                    Range = 0
                });
            if (!MagicExists(Spell.DarkBody))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "DarkBody",
                    Spell = Spell.DarkBody,
                    Icon = 70,
                    Level1 = 46,
                    Level2 = 49,
                    Level3 = 52,
                    Need1 = 6000,
                    Need2 = 10000,
                    Need3 = 14000,
                    BaseCost = 40,
                    LevelCost = 7,
                    Range = 0
                });
            if (!MagicExists(Spell.Hemorrhage))
                MagicInfoList.Add(new MagicInfo
                    { Name = "Hemorrhage", Spell = Spell.Hemorrhage, Icon = 75, Level1 = 47, Level2 = 51, Level3 = 55, Need1 = 9000, Need2 = 15000, Need3 = 21000, Range = 0 });
            if (!MagicExists(Spell.CrescentSlash))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "CresentSlash",
                    Spell = Spell.CrescentSlash,
                    Icon = 71,
                    Level1 = 50,
                    Level2 = 53,
                    Level3 = 56,
                    Need1 = 12000,
                    Need2 = 16000,
                    Need3 = 24000,
                    BaseCost = 19,
                    LevelCost = 5,
                    Range = 0
                });
            if (!MagicExists(Spell.MoonMist))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "MoonMist",
                    Spell = Spell.MoonMist,
                    Icon = 83,
                    Level1 = 48,
                    Level2 = 51,
                    Level3 = 56,
                    Need1 = 10,
                    Need2 = 20,
                    Need3 = 30,
                    BaseCost = 30,
                    LevelCost = 5,
                    DelayBase = 20000,
                    DelayReduction = 2000
                });
            if (!MagicExists(Spell.CatTongue))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "CatTongue",
                    Spell = Spell.CatTongue,
                    Icon = 79,
                    Level1 = 48,
                    Level2 = 51,
                    Level3 = 56,
                    Need1 = 10,
                    Need2 = 20,
                    Need3 = 30,
                    BaseCost = 30,
                    LevelCost = 5,
                    DelayBase = 20000,
                    DelayReduction = 2000
                });

            //Archer
            if (!MagicExists(Spell.Focus))
                MagicInfoList.Add(new MagicInfo { Name = "Focus", Spell = Spell.Focus, Icon = 88, Level1 = 7, Level2 = 13, Level3 = 17, Need1 = 270, Need2 = 600, Need3 = 1300, Range = 0 });
            if (!MagicExists(Spell.StraightShot))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "StraightShot",
                    Spell = Spell.StraightShot,
                    Icon = 89,
                    Level1 = 9,
                    Level2 = 12,
                    Level3 = 16,
                    Need1 = 350,
                    Need2 = 750,
                    Need3 = 1400,
                    BaseCost = 3,
                    LevelCost = 2,
                    MPowerBase = 8,
                    PowerBase = 3,
                    Range = 9
                });
            if (!MagicExists(Spell.DoubleShot))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "DoubleShot",
                    Spell = Spell.DoubleShot,
                    Icon = 90,
                    Level1 = 14,
                    Level2 = 18,
                    Level3 = 21,
                    Need1 = 700,
                    Need2 = 1500,
                    Need3 = 2100,
                    BaseCost = 3,
                    LevelCost = 2,
                    MPowerBase = 6,
                    PowerBase = 2,
                    Range = 9
                });
            if (!MagicExists(Spell.ExplosiveTrap))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "ExplosiveTrap",
                    Spell = Spell.ExplosiveTrap,
                    Icon = 91,
                    Level1 = 22,
                    Level2 = 25,
                    Level3 = 30,
                    Need1 = 2000,
                    Need2 = 3500,
                    Need3 = 5000,
                    BaseCost = 10,
                    LevelCost = 3,
                    MPowerBase = 15,
                    PowerBase = 15,
                    Range = 0
                });
            if (!MagicExists(Spell.DelayedExplosion))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "DelayedExplosion",
                    Spell = Spell.DelayedExplosion,
                    Icon = 92,
                    Level1 = 31,
                    Level2 = 34,
                    Level3 = 39,
                    Need1 = 3000,
                    Need2 = 7000,
                    Need3 = 10000,
                    BaseCost = 8,
                    LevelCost = 2,
                    MPowerBase = 30,
                    PowerBase = 15,
                    Range = 9
                });
            if (!MagicExists(Spell.Meditation))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "Meditation",
                    Spell = Spell.Meditation,
                    Icon = 93,
                    Level1 = 19,
                    Level2 = 24,
                    Level3 = 29,
                    Need1 = 1800,
                    Need2 = 2600,
                    Need3 = 5600,
                    BaseCost = 8,
                    LevelCost = 2,
                    Range = 0
                });
            if (!MagicExists(Spell.ElementalShot))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "ElementalShot",
                    Spell = Spell.ElementalShot,
                    Icon = 94,
                    Level1 = 20,
                    Level2 = 25,
                    Level3 = 31,
                    Need1 = 1800,
                    Need2 = 2700,
                    Need3 = 6000,
                    BaseCost = 8,
                    LevelCost = 2,
                    MPowerBase = 6,
                    PowerBase = 3,
                    Range = 9
                });
            if (!MagicExists(Spell.Concentration))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "Concentration",
                    Spell = Spell.Concentration,
                    Icon = 96,
                    Level1 = 23,
                    Level2 = 27,
                    Level3 = 32,
                    Need1 = 2100,
                    Need2 = 3800,
                    Need3 = 6500,
                    BaseCost = 8,
                    LevelCost = 2,
                    Range = 0
                });
            if (!MagicExists(Spell.ElementalBarrier))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "ElementalBarrier",
                    Spell = Spell.ElementalBarrier,
                    Icon = 98,
                    Level1 = 33,
                    Level2 = 38,
                    Level3 = 44,
                    Need1 = 3000,
                    Need2 = 7000,
                    Need3 = 10000,
                    BaseCost = 10,
                    LevelCost = 2,
                    MPowerBase = 15,
                    PowerBase = 5,
                    Range = 0
                });
            if (!MagicExists(Spell.BackStep))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "BackStep",
                    Spell = Spell.BackStep,
                    Icon = 95,
                    Level1 = 30,
                    Level2 = 34,
                    Level3 = 38,
                    Need1 = 2400,
                    Need2 = 3000,
                    Need3 = 6000,
                    BaseCost = 12,
                    LevelCost = 2,
                    DelayBase = 2500,
                    Range = 0
                });
            if (!MagicExists(Spell.BindingShot))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "BindingShot",
                    Spell = Spell.BindingShot,
                    Icon = 97,
                    Level1 = 35,
                    Level2 = 39,
                    Level3 = 42,
                    Need1 = 400,
                    Need2 = 7000,
                    Need3 = 9500,
                    BaseCost = 7,
                    LevelCost = 3,
                    Range = 9
                });
            if (!MagicExists(Spell.Stonetrap))
                MagicInfoList.Add(new MagicInfo
                    { Name = "Stonetrap", Spell = Spell.Stonetrap, Icon = 97, Level1 = 40, Level2 = 43, Level3 = 46, Need1 = 4900, Need2 = 9800, Need3 = 141, BaseCost = 7, LevelCost = 3, Range = 9 });
            if (!MagicExists(Spell.SummonVampire))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "SummonVampire",
                    Spell = Spell.SummonVampire,
                    Icon = 99,
                    Level1 = 28,
                    Level2 = 33,
                    Level3 = 41,
                    Need1 = 2000,
                    Need2 = 2700,
                    Need3 = 7500,
                    BaseCost = 10,
                    LevelCost = 5,
                    Range = 9
                });
            if (!MagicExists(Spell.VampireShot))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "VampireShot",
                    Spell = Spell.VampireShot,
                    Icon = 100,
                    Level1 = 26,
                    Level2 = 32,
                    Level3 = 36,
                    Need1 = 3000,
                    Need2 = 6000,
                    Need3 = 12000,
                    BaseCost = 12,
                    LevelCost = 3,
                    MPowerBase = 10,
                    PowerBase = 7,
                    Range = 9
                });
            if (!MagicExists(Spell.SummonToad))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "SummonToad",
                    Spell = Spell.SummonToad,
                    Icon = 101,
                    Level1 = 37,
                    Level2 = 43,
                    Level3 = 47,
                    Need1 = 5800,
                    Need2 = 10000,
                    Need3 = 13000,
                    BaseCost = 10,
                    LevelCost = 5,
                    Range = 9
                });
            if (!MagicExists(Spell.PoisonShot))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "PoisonShot",
                    Spell = Spell.PoisonShot,
                    Icon = 102,
                    Level1 = 40,
                    Level2 = 45,
                    Level3 = 49,
                    Need1 = 6000,
                    Need2 = 14000,
                    Need3 = 16000,
                    BaseCost = 10,
                    LevelCost = 4,
                    MPowerBase = 10,
                    PowerBase = 10,
                    Range = 9
                });
            if (!MagicExists(Spell.CrippleShot))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "CrippleShot",
                    Spell = Spell.CrippleShot,
                    Icon = 103,
                    Level1 = 43,
                    Level2 = 47,
                    Level3 = 50,
                    Need1 = 12000,
                    Need2 = 15000,
                    Need3 = 18000,
                    BaseCost = 15,
                    LevelCost = 3,
                    MPowerBase = 10,
                    MPowerBonus = 20,
                    PowerBase = 10,
                    Range = 9
                });
            if (!MagicExists(Spell.SummonSnakes))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "SummonSnakes",
                    Spell = Spell.SummonSnakes,
                    Icon = 104,
                    Level1 = 46,
                    Level2 = 51,
                    Level3 = 54,
                    Need1 = 14000,
                    Need2 = 17000,
                    Need3 = 20000,
                    BaseCost = 10,
                    LevelCost = 5,
                    Range = 9
                });
            if (!MagicExists(Spell.NapalmShot))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "NapalmShot",
                    Spell = Spell.NapalmShot,
                    Icon = 105,
                    Level1 = 48,
                    Level2 = 52,
                    Level3 = 55,
                    Need1 = 15000,
                    Need2 = 18000,
                    Need3 = 21000,
                    BaseCost = 40,
                    LevelCost = 10,
                    MPowerBase = 25,
                    MPowerBonus = 25,
                    PowerBase = 25,
                    Range = 9
                });
            if (!MagicExists(Spell.OneWithNature))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "OneWithNature",
                    Spell = Spell.OneWithNature,
                    Icon = 106,
                    Level1 = 50,
                    Level2 = 53,
                    Level3 = 56,
                    Need1 = 17000,
                    Need2 = 19000,
                    Need3 = 24000,
                    BaseCost = 80,
                    LevelCost = 15,
                    MPowerBase = 75,
                    MPowerBonus = 35,
                    PowerBase = 30,
                    PowerBonus = 20,
                    Range = 9
                });
            if (!MagicExists(Spell.MentalState))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "MentalState",
                    Spell = Spell.MentalState,
                    Icon = 81,
                    Level1 = 11,
                    Level2 = 15,
                    Level3 = 22,
                    Need1 = 500,
                    Need2 = 900,
                    Need3 = 1800,
                    BaseCost = 1,
                    LevelCost = 1,
                    Range = 0
                });

            //Custom
            if (!MagicExists(Spell.Portal))
                MagicInfoList.Add(new MagicInfo
                    { Name = "Portal", Spell = Spell.Portal, Icon = 1, Level1 = 7, Level2 = 11, Level3 = 14, Need1 = 150, Need2 = 350, Need3 = 700, BaseCost = 3, LevelCost = 2, Range = 9 });
            if (!MagicExists(Spell.BattleCry))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "BattleCry",
                    Spell = Spell.BattleCry,
                    Icon = 42,
                    Level1 = 48,
                    Level2 = 51,
                    Level3 = 55,
                    Need1 = 8000,
                    Need2 = 11000,
                    Need3 = 15000,
                    BaseCost = 22,
                    LevelCost = 10,
                    Range = 0
                });
            if (!MagicExists(Spell.FireBounce))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "FireBounce",
                    Spell = Spell.FireBounce,
                    Icon = 4,
                    Level1 = 15,
                    Level2 = 18,
                    Level3 = 21,
                    Need1 = 2000,
                    Need2 = 2700,
                    Need3 = 3500,
                    BaseCost = 5,
                    LevelCost = 1,
                    MPowerBase = 6,
                    PowerBase = 10,
                    Range = 9
                });
            if (!MagicExists(Spell.MeteorShower))
                MagicInfoList.Add(new MagicInfo
                {
                    Name = "MeteorShower",
                    Spell = Spell.MeteorShower,
                    Icon = 4,
                    Level1 = 15,
                    Level2 = 18,
                    Level3 = 21,
                    Need1 = 2000,
                    Need2 = 2700,
                    Need3 = 3500,
                    BaseCost = 5,
                    LevelCost = 1,
                    MPowerBase = 6,
                    PowerBase = 10,
                    Range = 9
                });
        }


        private string? CheckDbs()
        {
            if (GetMonsterInfo(Settings.SkeletonName, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.SkeletonName;
            
            if (GetMonsterInfo(Settings.ShinsuName, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.ShinsuName;
            
            if (GetMonsterInfo(Settings.BugBatName, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.BugBatName;
            
            if (GetMonsterInfo(Settings.Zuma1, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.Zuma1;
            
            if (GetMonsterInfo(Settings.Zuma2, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.Zuma2;
            if (GetMonsterInfo(Settings.Zuma3, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.Zuma3;
            if (GetMonsterInfo(Settings.Zuma4, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.Zuma4;
            if (GetMonsterInfo(Settings.Zuma5, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.Zuma5;
            if (GetMonsterInfo(Settings.Zuma6, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.Zuma6;
            if (GetMonsterInfo(Settings.Zuma7, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.Zuma7;
            if (GetMonsterInfo(Settings.Turtle1, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.Turtle1;
            if (GetMonsterInfo(Settings.Turtle2, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.Turtle2;
            if (GetMonsterInfo(Settings.Turtle3, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.Turtle3;
            if (GetMonsterInfo(Settings.Turtle4, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.Turtle4;
            if (GetMonsterInfo(Settings.Turtle5, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.Turtle5;
            if (GetMonsterInfo(Settings.BoneMonster1, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.BoneMonster1;
            if (GetMonsterInfo(Settings.BoneMonster2, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.BoneMonster2;
            if (GetMonsterInfo(Settings.BoneMonster3, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.BoneMonster3;
            if (GetMonsterInfo(Settings.BoneMonster4, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.BoneMonster4;
            if (GetMonsterInfo(Settings.BehemothMonster1, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.BehemothMonster1;
            if (GetMonsterInfo(Settings.BehemothMonster2, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.BehemothMonster2;
            if (GetMonsterInfo(Settings.BehemothMonster3, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.BehemothMonster3;
            if (GetMonsterInfo(Settings.HellKnight1, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.HellKnight1;
            if (GetMonsterInfo(Settings.HellKnight2, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.HellKnight2;
            if (GetMonsterInfo(Settings.HellKnight3, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.HellKnight3;
            if (GetMonsterInfo(Settings.HellKnight4, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.HellKnight4;
            if (GetMonsterInfo(Settings.HellBomb1, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.HellBomb1;
            if (GetMonsterInfo(Settings.HellBomb2, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.HellBomb2;
            if (GetMonsterInfo(Settings.HellBomb3, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.HellBomb3;
            if (GetMonsterInfo(Settings.WhiteSnake, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.WhiteSnake;
            if (GetMonsterInfo(Settings.AngelName, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.AngelName;
            if (GetMonsterInfo(Settings.BombSpiderName, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.BombSpiderName;
            if (GetMonsterInfo(Settings.CloneName, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.CloneName;
            if (GetMonsterInfo(Settings.AssassinCloneName, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.AssassinCloneName;
            if (GetMonsterInfo(Settings.VampireName, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.VampireName;
            if (GetMonsterInfo(Settings.ToadName, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.ToadName;
            if (GetMonsterInfo(Settings.SnakeTotemName, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.SnakeTotemName;
            if (GetMonsterInfo(Settings.FishingMonster, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.FishingMonster;
            if (GetMonsterInfo(Settings.GeneralMeowMeowMob1, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.GeneralMeowMeowMob1;
            if (GetMonsterInfo(Settings.GeneralMeowMeowMob2, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.GeneralMeowMeowMob2;
            if (GetMonsterInfo(Settings.GeneralMeowMeowMob3, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.GeneralMeowMeowMob3;
            if (GetMonsterInfo(Settings.GeneralMeowMeowMob4, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.GeneralMeowMeowMob4;
            if (GetMonsterInfo(Settings.KingHydraxMob, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.KingHydraxMob;
            if (GetMonsterInfo(Settings.HornedCommanderMob, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.HornedCommanderMob;
            if (GetMonsterInfo(Settings.HornedCommanderBombMob, true) == null)
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.HornedCommanderBombMob;
            if (GetMonsterInfo(Settings.SnowWolfKingMob, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.SnowWolfKingMob;
            if (GetMonsterInfo(Settings.ScrollMob1, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.ScrollMob1;
            if (GetMonsterInfo(Settings.ScrollMob2, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.ScrollMob2;
            if (GetMonsterInfo(Settings.ScrollMob3, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.ScrollMob3;
            if (GetMonsterInfo(Settings.ScrollMob4, true) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMob) + Settings.ScrollMob4;

            if (GetItemInfo(Settings.RefineOreName) == null) 
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutItem) + Settings.RefineOreName;

            return null;
        }

        private string CanStartEnv()
        {
            if (StartPoints.Count == 0)
            {
                return GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.CannotStartServerWithoutMapAndStartPoint);
            }

            if (Settings.EnforceDBChecks)
            {
                string? error = CheckDbs();
                if (error != null) return error;
            }

            WorldMapIcon? wmi = ValidateWorldMap();
            return wmi != null ? GameLanguage.ServerTextMap.GetLocalization((ServerTextKeys.InvalidWorldmapIndex), wmi.MapIndex, wmi.Title) :
                //add intelligent creature checks?
                "true";
        }

        private void WorkLoop()
        {
            try
            {
                Time = Stopwatch.ElapsedMilliseconds;

                var conTime = Time;
                var saveTime = Time + Settings.SaveDelay * Settings.Minute;
                var userTime = Time + Settings.Minute * 5;
                var lineMessageTime = Time + Settings.Minute * Settings.LineMessageTimer;
                var processTime = Time + 1000;
                var startTime = Time;

                var processCount = 0;
                var processRealCount = 0;

                LinkedListNode<MapObject>? current = null;

                if (Settings.Multithreaded)
                {
                    for (var j = 0; j < MobThreads.Length; j++)
                    {
                        MobThreads[j] = new MobThread
                        {
                            Id = j
                        };
                    }
                }

                StartEnviron();
                string canStartServer = CanStartEnv();
                if (canStartServer != "true")
                {
                    MessageQueue.Enqueue(canStartServer);
                    StopEnv();
                    _thread = null;
                    Stop();
                    return;
                }

                if (Settings.Multithreaded)
                {
                    for (var j = 1; j < MobThreads.Length; j++)
                    {
                        var info = MobThreads[j];
                        MobThreading[j] = new Thread(() => ThreadLoop(info)) { IsBackground = true };
                        MobThreading[j].Start();
                    }
                }

                StartNetwork();
                if (Settings.StartHTTPService)
                {
                    http = new HttpServer();
                    http.Start();
                }
                try
                {
                    while (Running)
                    {
                        Time = Stopwatch.ElapsedMilliseconds;

                        if (Time >= processTime)
                        {
                            LastCount = processCount;
                            LastRealCount = processRealCount;
                            processCount = 0;
                            processRealCount = 0;
                            processTime = Time + 1000;
                        }

                        if (conTime != Time)
                        {
                            conTime = Time;

                            AdjustLights();

                            lock (Connections)
                            {
                                for (var i = Connections.Count - 1; i >= 0; i--)
                                {
                                    Connections[i].Process();
                                }
                            }

                            lock (StatusConnections)
                            {
                                for (var i = StatusConnections.Count - 1; i >= 0; i--)
                                {
                                    StatusConnections[i].Process();
                                }
                            }
                        }


                        if (current == null)
                            current = Objects.First;

                        if (current == Objects.First)
                        {
                            LastRunTime = Time - startTime;
                            startTime = Time;
                        }

                        if (Settings.Multithreaded)
                        {
                            for (var j = 1; j < MobThreads.Length; j++)
                            {
                                var info = MobThreads[j];

                                if (!info.Stop) continue;
                                info.EndTime = Time + 10;
                                info.Stop = false;
                            }
                            lock (_locker)
                            {
                                Monitor.PulseAll(_locker); //changing a blocking condition. (this makes the threads wake up!)
                            }
                            //run the first loop in the main thread so the main thread automaticaly 'halts' until the other threads are finished
                            ThreadLoop(MobThreads[0]);
                        }

                        bool theEnd = false;
                        var start = Stopwatch.ElapsedMilliseconds;
                        while (!theEnd && Stopwatch.ElapsedMilliseconds - start < 20)
                        {
                            if (current == null)
                            {
                                theEnd = true;
                                break;
                            }

                            var next = current.Next;
                            if (!Settings.Multithreaded || current.Value.Race != ObjectType.Monster || current.Value.Master != null)
                            {
                                if (Time > current.Value.OperateTime)
                                {
                                    current.Value.Process();
                                    current.Value.SetOperateTime();
                                }
                                processCount++;
                            }
                            current = next;
                        }

                        foreach (Map map in MapList)
                            map.Process();

                        DragonSystem?.Process();

                        Process();

                        if (Time >= saveTime)
                        {
                            saveTime = Time + Settings.SaveDelay * Settings.Minute;
                            BeginSaveAccounts();
                            SaveDB();
                            SaveGuilds();
                            SaveGoods();
                            SaveConquests();
                        }

                        if (Time >= userTime)
                        {
                            userTime = Time + Settings.Minute * 5;
                            Broadcast(new S.Chat
                            {
                                Message = GameLanguage.ServerTextMap.GetLocalization((ServerTextKeys.OnlinePlayers), Players.Count),
                                Type = ChatType.Hint
                            });
                        }

                        if (LineMessages.Count <= 0 || Time < lineMessageTime)
                        {
                            continue;
                        }

                        lineMessageTime = Time + Settings.Minute * Settings.LineMessageTimer;
                        Broadcast(new S.Chat
                        {
                            Message = LineMessages[RandomProvider.Next(LineMessages.Count)],
                            Type = ChatType.LineMessage
                        });

                        //   if (Players.Count == 0) Thread.Sleep(1);
                        //   GC.Collect();
                    }
                }
                catch (Exception ex)
                {
                    lock (Connections)
                    {
                        for (var i = Connections.Count - 1; i >= 0; i--)
                            Connections[i].SendDisconnect(3);
                    }

                    // Get stack trace for the exception with source file information
                    var st = new StackTrace(ex, true);
                    // Get the top stack frame
                    StackFrame? frame = st.GetFrame(0);
                    // Get the line number from the stack frame
                    int? line = frame?.GetFileLineNumber() ?? 0;

                    MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization((ServerTextKeys.InnerWorkloopErrorLine), line, ex));
                }

                StopNetwork();
                StopEnv();
                SaveAccounts();
                SaveGuilds(true);
                SaveConquests(true);
            }
            catch (Exception ex)
            {
                // Get stack trace for the exception with source file information
                var st = new StackTrace(ex, true);
                // Get the top stack frame
                var frame = st.GetFrame(0);
                // Get the line number from the stack frame
                int? line = frame?.GetFileLineNumber() ?? 0;

                MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization((ServerTextKeys.OuterWorkloopErrorLine), line, ex));
            }

            _thread = null;
        }

        private void ThreadLoop(MobThread mobThread)
        {
            mobThread.Stop = false;

            try
            {
                bool stopping = false;
                mobThread.Current ??= mobThread.ObjectsList.First;
                stopping = mobThread.Current == null;

                while (Running)
                {
                    if (mobThread.Current == null)
                        mobThread.Current = mobThread.ObjectsList.First;
                    else
                    {
                        var next = mobThread.Current.Next;

                        //if we reach the end of our list > go back to the top (since we are running threaded, we dont want the system to sit there for xxms doing nothing)
                        if (mobThread.Current == mobThread.ObjectsList.Last)
                        {
                            next = mobThread.ObjectsList.First;
                            mobThread.LastRunTime = (mobThread.LastRunTime + (Time - mobThread.StartTime)) / 2;
                            //Info.LastRunTime = (Time - Info.StartTime) /*> 0 ? (Time - Info.StartTime) : Info.LastRunTime */;
                            mobThread.StartTime = Time;
                        }
                        if (Time > mobThread.Current.Value.OperateTime)
                        {
                            if (mobThread.Current.Value.Master == null) //since we are running multithreaded, dont allow pets to be processed (unless you constantly move pets into their map appropriate thead)
                            {
                                mobThread.Current.Value.Process();
                                mobThread.Current.Value.SetOperateTime();
                            }
                        }
                        mobThread.Current = next;
                    }

                    //if it's the main thread > make it loop till the subthreads are done, else make it stop after 'endtime'
                    if (mobThread.Id == 0)
                    {
                        stopping = true;
                        for (var x = 1; x < MobThreads.Length; x++)
                        {
                            if (!MobThreads[x].Stop)
                            {
                                stopping = false;
                            }
                        }
                        if (!stopping) continue;
                        
                        mobThread.Stop = true;
                        return;
                    }

                    if (Stopwatch.ElapsedMilliseconds <= mobThread.EndTime || !Running) continue;
                    
                    mobThread.Stop = true;
                    lock (_locker)
                    {
                        while (mobThread.Stop) Monitor.Wait(_locker);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is ThreadInterruptedException) return;

                MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization((ServerTextKeys.ThreadLoopError), ex));
            }
        }

        private void AdjustLights()
        {
            LightSetting oldLights = Lights;

            var hours = Now.Hour * 2 % 24;
            Lights = hours switch
            {
                6 or 7 => LightSetting.Dawn,
                >= 8 and <= 15 => LightSetting.Day,
                16 or 17 => LightSetting.Evening,
                _ => LightSetting.Night
            };

            if (oldLights == Lights) return;

            Broadcast(new S.TimeOfDay { Lights = Lights });
        }

        private void Process()
        {
            if (Now.Day != dailyTime)
            {
                dailyTime = Now.Day;
                ProcessNewDay();
            }

            if (Time >= warTime)
            {
                warTime = Time + Settings.Minute;
                for (int i = GuildsAtWar.Count - 1; i >= 0; i--)
                {
                    GuildsAtWar[i].TimeRemaining -= Settings.Minute;
                    if (GuildsAtWar[i].TimeRemaining >= 0) continue;
                    
                    GuildsAtWar[i].EndWar();
                    GuildsAtWar.RemoveAt(i);
                }
            }

            if (Time >= guildTime)
            {
                guildTime = Time + Settings.Minute;
                foreach (GuildObject guild in Guilds)
                {
                    guild.Process();
                }
            }

            if (Time >= conquestTime)
            {
                conquestTime = Time + Settings.Second * 10;
                foreach (ConquestObject conquest in Conquests)
                {
                    conquest.Process();
                }
            }

            if (Time >= rentalItemsTime)
            {
                rentalItemsTime = Time + Settings.Minute * 5;
                ProcessRentedItems();
            }

            if (Time >= auctionTime)
            {
                auctionTime = Time + Settings.Minute * 10;
                ProcessAuction();
            }

            if (Time >= spawnTime)
            {
                spawnTime = Time + Settings.Second * 10;
                Main.RespawnTick.Process();
            }

            if (Time >= robotTime)
            {
                robotTime = Time + Settings.Minute;
                Robot.Process(RobotNPC);
            }

            if (Time < timerTime)
            {
                return;
            }

            timerTime = Time + Settings.Second;

            string[] keys = [.. Timers.Keys];

            foreach (var key in keys)
            {
                if (Timers[key].RelativeTime <= Time)
                {
                    Timers.Remove(key);
                }
            }
        }

        private void ProcessAuction()
        {
            LinkedListNode<AuctionInfo>? auctionNode = Auctions.First;

            while (auctionNode != null)
            {
                AuctionInfo auction = auctionNode.Value;
                bool skip = auction.Expired || auction.Sold || Now < auction.ConsignmentDate.AddDays(Globals.ConsignmentLength);
                if (!skip)
                {
                    if (auction.ItemType == MarketItemType.Auction && auction.CurrentBid > auction.Price)
                    {
                        string message = GameLanguage.ServerTextMap.GetLocalization((ServerTextKeys.YouWonForGold), auction.Item.FriendlyName, auction.CurrentBid);

                        auction.Sold = true;
                        MailCharacter(auction.CurrentBuyerInfo, item: auction.Item, customMessage: message);

                        MessageAccount(auction.CurrentBuyerInfo.AccountInfo, GameLanguage.ServerTextMap.GetLocalization((ServerTextKeys.YouBoughtForGold), auction.Item.FriendlyName, auction.CurrentBid),
                            ChatType.Hint);
                        MessageAccount(auction.SellerInfo.AccountInfo, GameLanguage.ServerTextMap.GetLocalization((ServerTextKeys.YouSoldForGold), auction.Item.FriendlyName, auction.CurrentBid),
                            ChatType.Hint);
                    }
                    else
                    {
                        auction.Expired = true;
                    }
                }

                auctionNode = auctionNode.Next;
            }
        }

        public void Broadcast(Packet p)
        {
            foreach (PlayerObject player in Players)
                player.Enqueue(p);
        }

        public void RequiresBaseStatUpdate()
        {
            foreach (var player in Players)
                player.HasUpdatedBaseStats = false;
        }

        public void RequiresHeroBaseStatUpdate()
        {
            foreach (HeroObject hero in Heroes)
            {
                hero.HasUpdatedBaseStats = false;
                hero.RefreshStats();
            }
        }

        public void SaveDB()
        {
            if (File.Exists(DatabasePath))
            {
                if (!Directory.Exists(DatabaseBackUpPath)) Directory.CreateDirectory(DatabaseBackUpPath);

                var fileName =
                    $"Database {Now.Year:0000}-{Now.Month:00}-{Now.Day:00} {Now.Hour:00}-{Now.Minute:00}-{Now.Second:00}.bak";

                var backupFile = Path.Combine(DatabaseBackUpPath, fileName);

                if (File.Exists(backupFile)) File.Delete(backupFile);
                File.Copy(DatabasePath, backupFile);
            }

            using FileStream databaseStream = File.Create(DatabasePath);
            using BinaryWriter databaseWriter = new BinaryWriter(databaseStream);
            databaseWriter.Write(Version);
            databaseWriter.Write(CustomVersion);
            databaseWriter.Write(MapIndex);
            databaseWriter.Write(ItemIndex);
            databaseWriter.Write(MonsterIndex);
            databaseWriter.Write(NPCIndex);
            databaseWriter.Write(QuestIndex);
            databaseWriter.Write(GameshopIndex);
            databaseWriter.Write(ConquestIndex);
            databaseWriter.Write(RespawnIndex);

            databaseWriter.Write(MapInfoList.Count);
            foreach (var mapInfo in MapInfoList)
                mapInfo.Save(databaseWriter);

            databaseWriter.Write(ItemInfoList.Count);
            foreach (var itemInfo in ItemInfoList)
                itemInfo.Save(databaseWriter);

            databaseWriter.Write(MonsterInfoList.Count);
            foreach (var monster in MonsterInfoList)
                monster.Save(databaseWriter);

            databaseWriter.Write(NPCInfoList.Count);
            foreach (var npc in NPCInfoList)
                npc.Save(databaseWriter);

            databaseWriter.Write(QuestInfoList.Count);
            foreach (var quest in QuestInfoList)
                quest.Save(databaseWriter);

            DragonInfo.Save(databaseWriter);
            databaseWriter.Write(MagicInfoList.Count);
            foreach (var magic in MagicInfoList)
                magic.Save(databaseWriter);

            databaseWriter.Write(GameShopList.Count);
            foreach (var gameShop in GameShopList)
                gameShop.Save(databaseWriter);

            databaseWriter.Write(ConquestInfoList.Count);
            foreach (var conquest in ConquestInfoList)
                conquest.Save(databaseWriter);

            RespawnTick.Save(databaseWriter);

            databaseWriter.Write(GTMapList.Count);
            foreach (var gtMap in GTMapList)
                gtMap.Save(databaseWriter);
        }


        public CharacterInfo? GetArchivedCharacter(string name)
        {
            DirectoryInfo dir = new DirectoryInfo(ArchivePath);
            FileInfo[] files = dir.GetFiles($"{name}*.MirCA");

            if (files.Length != 1)
            {
                return null;
            }

            var fileInfo = files[0];

            using FileStream fileStream = fileInfo.OpenRead();
            using var reader = new BinaryReader(fileStream);

            var version = reader.ReadInt32();
            var customVersion = reader.ReadInt32();
            return new CharacterInfo(reader, version, customVersion);
        }

        public void SaveArchivedCharacter(CharacterInfo info)
        {
            if (!Directory.Exists(ArchivePath)) Directory.CreateDirectory(ArchivePath);

            using var stream = File.Create(Path.Combine(ArchivePath, @$"{info.Name}{Now:_MMddyyyy_HHmmss}.MirCA"));
            using var writer = new BinaryWriter(stream);

            writer.Write(Version);
            writer.Write(CustomVersion);

            info.Save(writer);
        }

        public void SaveAccounts()
        {
            while (Saving)
                Thread.Sleep(1);

            try
            {
                using var stream = File.Create(AccountPath + "n");
                SaveAccounts(stream);
                
                if (File.Exists(AccountPath))
                    File.Move(AccountPath, AccountPath + "o");
                File.Move(AccountPath + "n", AccountPath);
                if (File.Exists(AccountPath + "o"))
                    File.Delete(AccountPath + "o");
            }
            catch (Exception ex)
            {
                MessageQueue.Enqueue(ex);
            }
        }

        private void SaveAccounts(Stream stream)
        {
            using var writer = new BinaryWriter(stream);
            writer.Write(Version);
            writer.Write(CustomVersion);
            writer.Write(NextAccountID);
            writer.Write(NextCharacterID);
            writer.Write(NextUserItemID);
            writer.Write(NextHeroID);

            writer.Write(GuildList.Count);
            writer.Write(NextGuildID);
            writer.Write(HeroList.Count);
            foreach (HeroInfo hero in HeroList)
                hero.Save(writer);

            writer.Write(AccountList.Count);
            foreach (AccountInfo account in AccountList)
                account.Save(writer);

            writer.Write(NextAuctionID);
            writer.Write(Auctions.Count);
            foreach (AuctionInfo auction in Auctions)
                auction.Save(writer);

            writer.Write(NextMailID);

            writer.Write(GameshopLog.Count);
            foreach ((int key, int value) in GameshopLog)
            {
                writer.Write(key);
                writer.Write(value);
            }

            writer.Write(SavedSpawns.Count);
            foreach (MapRespawn spawn in SavedSpawns)
            {
                RespawnSave save = new()
                {
                    RespawnIndex = spawn.Info.RespawnIndex, 
                    NextSpawnTick = spawn.NextSpawnTick, 
                    Spawned = spawn.Count >= spawn.Info.Count * SpawnMultiplier
                };
                save.Save(writer);
            }
        }

        private void SaveGuilds(bool forced = false)
        {
            if (!Directory.Exists(Settings.GuildPath)) Directory.CreateDirectory(Settings.GuildPath);

            if (GuildRefreshNeeded == true) //deletes guild files and resaves with new indexing if a guild is deleted.
            {
                foreach (string guildFile in Directory.GetFiles(Settings.GuildPath, "*.mgd"))
                {
                    File.Delete(guildFile);
                }

                GuildRefreshNeeded = false;
                forced = true; //triggers a full resave of all guilds
            }

            for (var i = 0; i < GuildList.Count; i++)
            {
                if (!GuildList[i].NeedSave && !forced)
                {
                    continue;
                }

                GuildList[i].NeedSave = false;

                GuildObject? liveGuild = Guilds.Find(g => g.Guildindex == GuildList[i].GuildIndex);
                if (liveGuild != null)
                {
                    GuildList[i] = liveGuild.Info;
                }

                MemoryStream memoryStream = new();
                BinaryWriter writer = new(memoryStream);
                GuildList[i].Save(writer);
                FileStream fileStream = new FileStream(Path.Combine(Settings.GuildPath, i + ".mgdn"), FileMode.Create);
                byte[] data = memoryStream.ToArray();
                fileStream.BeginWrite(data, 0, data.Length, EndSaveGuildsAsync, fileStream);
            }
        }
        
        private static void EndSaveGuildsAsync(IAsyncResult result)
        {
            var fileStream = result.AsyncState as FileStream;
            try
            {
                if (fileStream == null) return;
                var oldFilename = fileStream.Name[..^1];
                var newFilename = fileStream.Name;
                fileStream.EndWrite(result);
                fileStream.Dispose();
                if (File.Exists(oldFilename))
                    File.Move(oldFilename, oldFilename + "o");
                
                File.Move(newFilename, oldFilename);
                if (File.Exists(oldFilename + "o"))
                    File.Delete(oldFilename + "o");
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void SaveGoods(bool forced = false)
        {
            if (!Directory.Exists(Settings.GoodsPath)) Directory.CreateDirectory(Settings.GoodsPath);

            foreach (var npc in MapList.Where(map => map.NPCs.Count != 0).SelectMany(map => map.NPCs))
            {
                if (forced)
                {
                    npc.ProcessGoods(true);
                }

                if (!npc.NeedSave) continue;

                var path = Path.Combine(Settings.GoodsPath, npc.Info.Index + ".msdn");

                var memoryStream = new MemoryStream();
                var writer = new BinaryWriter(memoryStream);
                const int temp = 9999;
                writer.Write(temp);
                writer.Write(Version);
                writer.Write(CustomVersion);
                writer.Write(npc.UsedGoods.Count);

                foreach (var good in npc.UsedGoods)
                {
                    good.Save(writer);
                }

                FileStream fileStream = new FileStream(path, FileMode.Create);
                byte[] data = memoryStream.ToArray();
                fileStream.BeginWrite(data, 0, data.Length, EndSaveGoodsAsync, fileStream);
            }
        }
        
        private static void EndSaveGoodsAsync(IAsyncResult result)
        {
            try
            {
                if (result.AsyncState is not FileStream fileStream) return;
                
                fileStream.EndWrite(result);
                fileStream.Dispose();
                
                string oldFileName = fileStream.Name[..^1];
                string newFileName = fileStream.Name;
                if (File.Exists(oldFileName))
                    File.Move(oldFileName, oldFileName + "o");
                
                File.Move(newFileName, oldFileName);
                if (File.Exists(oldFileName + "o"))
                    File.Delete(oldFileName + "o");
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void SaveConquests(bool forced = false)
        {
            if (!Directory.Exists(Settings.ConquestsPath)) Directory.CreateDirectory(Settings.ConquestsPath);
            foreach (ConquestGuildInfo conquest in ConquestList.Where(conquest => conquest.NeedSave || forced))
            {
                conquest.NeedSave = false;
                MemoryStream memoryStream = new();
                BinaryWriter writer = new(memoryStream);
                conquest.Save(writer);
                FileStream fileStream = new(Path.Combine(Settings.ConquestsPath, conquest.Info.Index + ".mcdn"), FileMode.Create);
                byte[] data = memoryStream.ToArray();
                fileStream.BeginWrite(data, 0, data.Length, EndSaveConquestsAsync, fileStream);
            }
        }
        
        private static void EndSaveConquestsAsync(IAsyncResult result)
        {
            FileStream? fileStream = result.AsyncState as FileStream;
            try
            {
                if (fileStream == null) return;
                
                fileStream.EndWrite(result);
                fileStream.Dispose();
                
                string oldFilename = fileStream.Name[..^1];
                string newFilename = fileStream.Name;
                if (File.Exists(oldFilename))
                    File.Move(oldFilename, oldFilename + "o");

                File.Move(newFilename, oldFilename);
                if (File.Exists(oldFilename + "o"))
                    File.Delete(oldFilename + "o");
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void BeginSaveAccounts()
        {
            if (Saving) return;

            Saving = true;
            using var memoryStream = new MemoryStream();
            if (File.Exists(AccountPath))
            {
                if (!Directory.Exists(AccountsBackUpPath))
                {
                    Directory.CreateDirectory(AccountsBackUpPath);
                }
                
                string fileName = $"Accounts {Now.Year:0000}-{Now.Month:00}-{Now.Day:00} {Now.Hour:00}-{Now.Minute:00}-{Now.Second:00}.bak";
                if (File.Exists(Path.Combine(AccountsBackUpPath, fileName)))
                {
                    File.Delete(Path.Combine(AccountsBackUpPath, fileName));
                }
                
                File.Move(AccountPath, Path.Combine(AccountsBackUpPath, fileName));
            }

            SaveAccounts(memoryStream);
            FileStream fileStream = new(AccountPath + "n", FileMode.Create);

            byte[] data = memoryStream.ToArray();
            fileStream.BeginWrite(data, 0, data.Length, EndSaveAccounts, fileStream);
        }

        private void EndSaveAccounts(IAsyncResult result)
        {
            FileStream? fileStream = result.AsyncState as FileStream;
            if (fileStream == null)
            {
                Saving = false;
                return;
            }
            try
            {
                fileStream.EndWrite(result);
                fileStream.Dispose();
                
                var oldFilename = fileStream.Name[..^1];
                var newFilename = fileStream.Name;
                if (File.Exists(oldFilename))
                    File.Move(oldFilename, oldFilename + "o");
                
                File.Move(newFilename, oldFilename);
                if (File.Exists(oldFilename + "o"))
                    File.Delete(oldFilename + "o");
            }
            catch (Exception)
            {
                // ignored
            }

            Saving = false;
        }

        public bool LoadDB()
        {
            lock (LoadLock)
            {
                if (!File.Exists(DatabasePath))
                {
                    SaveDB();
                }

                using FileStream databaseStream = File.OpenRead(DatabasePath);
                using BinaryReader databaseReader = new(databaseStream);
                LoadVersion = databaseReader.ReadInt32();
                LoadCustomVersion = databaseReader.ReadInt32();

                if (LoadVersion < MinVersion)
                {
                    MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization((ServerTextKeys.CannotLoadDatabaseMinSupported), LoadVersion, MinVersion));
                    return false;
                }

                if (LoadVersion > Version)
                {
                    MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization((ServerTextKeys.CannotLoadDatabaseMaxSupported), LoadVersion, Version));
                    return false;
                }

                MapIndex = databaseReader.ReadInt32();
                ItemIndex = databaseReader.ReadInt32();
                MonsterIndex = databaseReader.ReadInt32();

                NPCIndex = databaseReader.ReadInt32();
                QuestIndex = databaseReader.ReadInt32();

                if (LoadVersion >= 63)
                {
                    GameshopIndex = databaseReader.ReadInt32();
                }

                if (LoadVersion >= 66)
                {
                    ConquestIndex = databaseReader.ReadInt32();
                }

                if (LoadVersion >= 68)
                    RespawnIndex = databaseReader.ReadInt32();


                int count = databaseReader.ReadInt32();
                MapInfoList.Clear();
                for (var i = 0; i < count; i++)
                    MapInfoList.Add(new MapInfo(databaseReader));

                count = databaseReader.ReadInt32();
                ItemInfoList.Clear();
                for (var i = 0; i < count; i++)
                {
                    ItemInfoList.Add(new ItemInfo(databaseReader, LoadVersion, LoadCustomVersion));
                    if (ItemInfoList[i].RandomStatsId < Settings.RandomItemStatsList.Count)
                    {
                        ItemInfoList[i].RandomStats = Settings.RandomItemStatsList[ItemInfoList[i].RandomStatsId];
                    }
                }
                
                count = databaseReader.ReadInt32();
                MonsterInfoList.Clear();
                for (var i = 0; i < count; i++)
                    MonsterInfoList.Add(new MonsterInfo(databaseReader));

                count = databaseReader.ReadInt32();
                NPCInfoList.Clear();
                for (var i = 0; i < count; i++)
                    NPCInfoList.Add(new NPCInfo(databaseReader));

                count = databaseReader.ReadInt32();
                QuestInfoList.Clear();
                for (var i = 0; i < count; i++)
                    QuestInfoList.Add(new QuestInfo(databaseReader));

                DragonInfo = new DragonInfo(databaseReader);
                
                count = databaseReader.ReadInt32();
                for (var i = 0; i < count; i++)
                {
                    var m = new MagicInfo(databaseReader, LoadVersion, LoadCustomVersion);
                    if (!MagicExists(m.Spell))
                        MagicInfoList.Add(m);
                }

                FillMagicInfoList();
                if (LoadVersion <= 70)
                    UpdateMagicInfo();

                if (LoadVersion >= 63)
                {
                    count = databaseReader.ReadInt32();
                    GameShopList.Clear();
                    for (var i = 0; i < count; i++)
                    {
                        var item = new GameShopItem(databaseReader, LoadVersion, LoadCustomVersion);
                        if (BindGameShop(item))
                        {
                            GameShopList.Add(item);
                        }
                    }
                }

                if (LoadVersion >= 66)
                {
                    ConquestInfoList.Clear();
                    count = databaseReader.ReadInt32();
                    for (var i = 0; i < count; i++)
                    {
                        ConquestInfoList.Add(new ConquestInfo(databaseReader));
                    }
                }

                if (LoadVersion > 67)
                    RespawnTick = new RespawnTimer(databaseReader);
                
                Settings.LinkGuildCreationItems(ItemInfoList);
            }

            return true;
        }

        private void LoadAccounts()
        {
            //reset ranking
            for (var i = 0; i < RankClass.Length; i++)
            {
                if (RankClass[i] != null)
                {
                    RankClass[i]!.Clear();
                }
                else
                {
                    RankClass[i] = [];
                }
            }

            RankTop.Clear();

            lock (LoadLock)
            {
                if (!File.Exists(AccountPath))
                {
                    SaveAccounts();
                }

                using var accountStream = File.OpenRead(AccountPath);
                using var accountReader = new BinaryReader(accountStream);
                LoadVersion = accountReader.ReadInt32();
                LoadCustomVersion = accountReader.ReadInt32();
                NextAccountID = accountReader.ReadInt32();
                NextCharacterID = accountReader.ReadInt32();
                NextUserItemID = accountReader.ReadUInt64();
                if (LoadVersion > 98)
                    NextHeroID = accountReader.ReadInt32();

                GuildCount = accountReader.ReadInt32();
                NextGuildID = accountReader.ReadInt32();

                int count;
                if (LoadVersion > 102)
                {
                    count = accountReader.ReadInt32();

                    HeroList.Clear();

                    for (var i = 0; i < count; i++)
                        HeroList.Add(new HeroInfo(accountReader, LoadVersion, LoadCustomVersion));
                }

                count = accountReader.ReadInt32();

                AccountList.Clear();
                CharacterList.Clear();

                int trueAccount = 0;
                for (var i = 0; i < count; i++)
                {
                    AccountInfo nextAccount = new(accountReader);
                    if (i > 0 && nextAccount.Characters.Count == 0)
                        continue;
                    
                    AccountList.Add(nextAccount);
                    CharacterList.AddRange(AccountList[trueAccount].Characters);
                    if (LoadVersion is > 98 and < 103)
                        AccountList[trueAccount].Characters.ForEach(character => HeroList.AddRange(character.Heroes));
                    
                    trueAccount++;
                }

                foreach (var auction in Auctions)
                {
                    auction.SellerInfo.AccountInfo.Auctions.Remove(auction);
                }

                Auctions.Clear();

                NextAuctionID = accountReader.ReadUInt64();

                count = accountReader.ReadInt32();
                for (var i = 0; i < count; i++)
                {
                    var auction = new AuctionInfo(accountReader, LoadVersion, LoadCustomVersion);

                    if (!BindItem(auction.Item) || !BindCharacter(auction)) continue;

                    Auctions.AddLast(auction);
                    auction.SellerInfo.AccountInfo.Auctions.AddLast(auction);
                }

                NextMailID = accountReader.ReadUInt64();

                if (LoadVersion <= 80)
                {
                    count = accountReader.ReadInt32();
                    for (var i = 0; i < count; i++)
                    {
                        var mail = new MailInfo(accountReader, LoadVersion, LoadCustomVersion);

                        mail.RecipientInfo = GetCharacterInfo(mail.RecipientIndex);

                        mail.RecipientInfo?.Mail.Add(mail); //add to players inbox
                    }
                }

                if (LoadVersion >= 63)
                {
                    var logCount = accountReader.ReadInt32();
                    for (var i = 0; i < logCount; i++)
                    {
                        GameshopLog.Add(accountReader.ReadInt32(), accountReader.ReadInt32());
                    }

                    if (ResetGS) ClearGameShopLog();
                }

                if (LoadVersion >= 68)
                {
                    var saveCount = accountReader.ReadInt32();
                    for (var i = 0; i < saveCount; i++)
                    {
                        var saved = new RespawnSave(accountReader);
                        foreach (var respawn in SavedSpawns.Where(respawn => respawn.Info.RespawnIndex == saved.RespawnIndex))
                        {
                            respawn.NextSpawnTick = saved.NextSpawnTick;
                            if (!saved.Spawned || respawn.Info.Count * SpawnMultiplier <= respawn.Count)
                            {
                                continue;
                            }

                            var mobCount = respawn.Info.Count * SpawnMultiplier - respawn.Count;
                            for (var j = 0; j < mobCount; j++)
                            {
                                respawn.Spawn();
                            }
                        }
                    }
                }
            }
        }

        private void LoadGuilds()
        {
            lock (LoadLock)
            {
                GuildList.Clear();
                int count = 0;
                for (var i = 0; i < GuildCount; i++)
                {
                    if (!File.Exists(Path.Combine(Settings.GuildPath, i + ".mgd"))) continue;

                    using var stream = File.OpenRead(Path.Combine(Settings.GuildPath, i + ".mgd"));
                    using var reader = new BinaryReader(stream);
                    GuildInfo guildInfo = new(reader);
                    GuildList.Add(guildInfo);
                    GuildObject guildObject = new GuildObject(guildInfo);
                    count++;
                }

                GuildCount = count;
            }
        }

        private void LoadConquests()
        {
            lock (LoadLock)
            {
                Conquests.Clear();
                ConquestList.Clear();

                foreach (ConquestInfo conquest in ConquestInfoList)
                {
                    ConquestObject newConquest;
                    ConquestGuildInfo conquestGuildInfo;
                    Map? tempMap = GetMap(conquest.MapIndex);

                    if (tempMap == null) continue;

                    if (File.Exists(Path.Combine(Settings.ConquestsPath, conquest.Index + ".mcd")))
                    {
                        using (var stream = File.OpenRead(Path.Combine(Settings.ConquestsPath, conquest.Index + ".mcd")))
                        {
                            using var reader = new BinaryReader(stream);
                            conquestGuildInfo = new ConquestGuildInfo(reader) { Info = conquest };
                        }

                        newConquest = new ConquestObject(conquestGuildInfo)
                        {
                            ConquestMap = tempMap
                        };

                        foreach (var guild in Guilds.Where(guild => conquestGuildInfo.Owner == guild.Guildindex))
                        {
                            newConquest.Guild = guild;
                            guild.Conquest = newConquest;
                        }
                    }
                    else
                    {
                        conquestGuildInfo = new ConquestGuildInfo { Info = conquest, NeedSave = true };
                        newConquest = new ConquestObject(conquestGuildInfo)
                        {
                            ConquestMap = tempMap
                        };
                    }

                    ConquestList.Add(conquestGuildInfo);
                    Conquests.Add(newConquest);
                    tempMap.Conquest.Add(newConquest);

                    newConquest.Bind();
                }
            }
        }

        private void LoadGTInfo()
        {
            foreach (var gt in GTMapList)
            {
                GuildInfo? guild = GuildList.FirstOrDefault(x => x.GTIndex == gt.Index);
                if (guild == null)
                {
                    continue;
                }

                gt.Owner = guild.Name;
                if (guild.Ranks.Count > 0 && guild.Ranks[0] != null && guild.Ranks[0]!.Members.Count > 0 && guild.Ranks[0]!.Members[0] != null)
                    gt.Leader = guild.Ranks[0]!.Members[0].Name;
                gt.Price = 0;
                gt.Days = (Now - guild.GTRent).Days;
            }
        }

        private static void LoadDisabledChars()
        {
            DisabledCharNames.Clear();

            var path = Path.Combine(Settings.EnvirPath, "DisabledChars.txt");

            if (!File.Exists(path))
            {
                File.WriteAllText(path, "");
            }
            else
            {
                var lines = File.ReadAllLines(path);

                foreach (var line in lines)
                {
                    if (line.StartsWith(';') || string.IsNullOrWhiteSpace(line)) continue;
                    DisabledCharNames.Add(line.ToUpper());
                }
            }
        }

        public static void LoadLineMessages()
        {
            LineMessages.Clear();

            var path = Path.Combine(Settings.EnvirPath, "LineMessage.txt");

            if (!File.Exists(path))
            {
                File.WriteAllText(path, "");
            }
            else
            {
                var lines = File.ReadAllLines(path);

                for (var i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith(';') || string.IsNullOrWhiteSpace(lines[i])) continue;
                    LineMessages.Add(lines[i]);
                }
            }
        }

        private bool BindCharacter(AuctionInfo auction)
        {
            bool bound = false;

            foreach (var character in CharacterList)
            {
                if (character.Index == auction.SellerIndex)
                {
                    auction.SellerInfo = character;
                    bound = true;
                }

                else if (character.Index == auction.CurrentBuyerIndex)
                {
                    auction.CurrentBuyerInfo = character;
                    bound = true;
                }
            }

            return bound;
        }

        public void Start()
        {
            if (Running || _thread != null) return;

            Running = true;

            _thread = new Thread(WorkLoop) { IsBackground = true };
            _thread.Start();
        }

        public void Stop()
        {
            Running = false;

            lock (_locker)
            {
                // changing a blocking condition. (this makes the threads wake up!)
                Monitor.PulseAll(_locker);
            }

            //simply interrupt all the mob threads if they are running (will give an invisible error on them but fastest way of getting rid of them on shutdowns)
            if (Settings.Multithreaded)
            {
                for (var i = 1; i < MobThreading.Length; i++)
                {
                    MobThreads[i].EndTime = Time + 9999;
                    if ( MobThreading[i].ThreadState != System.Threading.ThreadState.Stopped && MobThreading[i].ThreadState != System.Threading.ThreadState.Unstarted)
                    {
                        MobThreading[i].Interrupt();
                    }
                }
            }

            http?.Stop();

            while (_thread != null)
                Thread.Sleep(1);
        }

        public void Reboot()
        {
            new Thread(() =>
            {
                MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.ServerRebooting));
                Stop();
                Start();
            }).Start();
        }

        public void UpdateIPBlock(string ipAddress, TimeSpan value)
        {
            IPBlocks[ipAddress] = Now.Add(value);
        }

        private void StartEnviron()
        {
            Players.Clear();
            StartPoints.Clear();
            StartItems.Clear();
            MapList.Clear();
            GTMapList.Clear();
            GameshopLog.Clear();
            CustomCommands.Clear();
            Heroes.Clear();
            MonsterCount = 0;

            LoadDB();

            BuffInfoList.Clear();
            foreach (var buff in BuffInfo.Load())
            {
                BuffInfoList.Add(buff);
            }

            MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization((ServerTextKeys.BuffsLoaded), BuffInfoList.Count));

            RecipeInfoList.Clear();
            foreach (var recipe in Directory.GetFiles(Settings.RecipePath, "*.txt")
                         .Select(path => Path.GetFileNameWithoutExtension(path))
                         .ToArray())
            {
                RecipeInfoList.Add(new RecipeInfo(recipe));
            }

            MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization((ServerTextKeys.RecipesLoaded), RecipeInfoList.Count));

            foreach (var mapInfo in MapInfoList)
            {
                // Call CreateMap(), which adds the map to Env.MapList
                mapInfo.CreateMap();

                // Fetch the created map from Env.MapList
                Map? map = MapList.FirstOrDefault(m => m.Info == mapInfo);

                if (map == null)
                {
                    continue;
                }

                if (!mapInfo.GT)
                {
                    continue;
                }

                GTMap? gt = GTMapList.FirstOrDefault(x => x.Index == mapInfo.GTIndex);
                if (gt != null)
                {
                    gt.Maps.Add(map);
                }
                else
                {
                    GTMap GT = new()
                    {
                        Index = mapInfo.GTIndex,
                        Name = mapInfo.Title,
                        Price = Settings.BuyGTGold,
                        Days = 0,
                        Begin = 0,
                        Leader = "None",
                        Owner = "None",
                    };
                    GT.Maps.Add(map);

                    GTMapList.Add(GT);
                }
            }

            MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization((ServerTextKeys.MapsLoaded), MapInfoList.Count));

            foreach (var itemInfo in ItemInfoList.Where(itemInfo => itemInfo.StartItem))
            {
                StartItems.Add(itemInfo);
            }

            ReloadDrops();

            LoadDisabledChars();
            LoadLineMessages();

            if (DragonInfo.Enabled)
            {
                DragonSystem = new Dragon(DragonInfo);
                if (DragonSystem.Load()) DragonSystem.Info.LoadDrops();
                MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.DragonLoaded));
            }

            DefaultNPC = NPCScript.GetOrAdd((uint)RandomProvider.Next(1000000, 1999999), Settings.DefaultNPCFilename, NPCScriptType.AutoPlayer);
            MonsterNPC = NPCScript.GetOrAdd((uint)RandomProvider.Next(2000000, 2999999), Settings.MonsterNPCFilename, NPCScriptType.AutoMonster);
            RobotNPC = NPCScript.GetOrAdd((uint)RandomProvider.Next(3000000, 3999999), Settings.RobotNPCFilename, NPCScriptType.Robot);

            MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.EnvirStarted));           
        }

        
        private void StartNetwork()
        {
            Connections.Clear();
            LoadAccounts();
            LoadGuilds();
            LoadConquests();
            LoadGTInfo();

            _listener = new TcpListener(IPAddress.Parse(Settings.IPAddress), Settings.Port);
            _listener.Start();
            _listener.BeginAcceptTcpClient(Connection, null);

            if (StatusPortEnabled)
            {
                _StatusPort = new TcpListener(IPAddress.Parse(Settings.IPAddress), 3000);
                _StatusPort.Start();
                _StatusPort.BeginAcceptTcpClient(StatusConnection, null);
            }

            MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.NetworkStarted));
        }

        private void StopEnv()
        {
            SaveGoods(true);

            MapList.Clear();
            StartPoints.Clear();
            StartItems.Clear();
            Objects.Clear();
            Players.Clear();
            Heroes.Clear();
            GTMapList.Clear();

            CleanUp();
            GC.Collect();
            MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.EnvirStopped));
        }
        private void StopNetwork()
        {
            _listener.Stop();
            lock (Connections)
            {
                for (var i = Connections.Count - 1; i >= 0; i--)
                    Connections[i].SendDisconnect(0);
            }

            if (StatusPortEnabled)
            {
                _StatusPort.Stop();
                for (var i = StatusConnections.Count - 1; i >= 0; i--)
                    StatusConnections[i].SendDisconnect();
            }

            var expire = Time + 5000;
            while (Connections.Count != 0 && Stopwatch.ElapsedMilliseconds < expire)
            {
                Time = Stopwatch.ElapsedMilliseconds;
                for (var i = Connections.Count - 1; i >= 0; i--)
                    Connections[i].Process();

                Thread.Sleep(1);
            }

            Connections.Clear();

            expire = Time + 10000;
            while (StatusConnections.Count != 0 && Stopwatch.ElapsedMilliseconds < expire)
            {
                Time = Stopwatch.ElapsedMilliseconds;
                for (var i = StatusConnections.Count - 1; i >= 0; i--)
                    StatusConnections[i].Process();

                Thread.Sleep(1);
            }

            StatusConnections.Clear();
            MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.NetworkStopped));
        }

        private void CleanUp()
        {
            foreach (var characterInfo in CharacterList)
            {
                if (characterInfo.Deleted)
                {
                    #region Mentor Cleanup
                    if (characterInfo.Mentor > 0)
                    {
                        CharacterInfo? mentor = GetCharacterInfo(characterInfo.Mentor);
                        if (mentor != null)
                        {
                            mentor.Mentor = 0;
                            mentor.MentorExp = 0;
                            mentor.IsMentor = false;
                        }

                        characterInfo.Mentor = 0;
                        characterInfo.MentorExp = 0;
                        characterInfo.IsMentor = false;
                    }
                    #endregion

                    #region Marriage Cleanup
                    if (characterInfo.Married > 0)
                    {
                        CharacterInfo? lover = GetCharacterInfo(characterInfo.Married);
                        if (lover != null)
                        {
                            characterInfo.Married = 0;
                            characterInfo.MarriedDate = Now;

                            lover.Married = 0;
                            lover.MarriedDate = Now;
                            var ring = lover.Equipment[(int)EquipmentSlot.RingL];
                            if (ring != null)
                                ring.WeddingRing = -1;
                        }
                    }
                    #endregion
                }

                if (characterInfo.Mail.Count > Settings.MailCapacity)
                {
                    for (var j = characterInfo.Mail.Count - 1 - (int)Settings.MailCapacity; j >= 0; j--)
                    {
                        if (characterInfo.Mail[j].DateOpened > Now && characterInfo.Mail[j].Collected && characterInfo.Mail[j].Items.Count == 0 && characterInfo.Mail[j].Gold == 0)
                        {
                            characterInfo.Mail.Remove(characterInfo.Mail[j]);
                        }
                    }
                }
            }
        }

        private void Connection(IAsyncResult result)
        {
            try
            {
                if (!Running || !_listener.Server.IsBound) return;
            }
            catch (Exception e)
            {
                MessageQueue.Enqueue(e.ToString());
            }

            try
            {
                TcpClient tempTcpClient = _listener.EndAcceptTcpClient(result);

                bool connected = false;
                var ipAddress = tempTcpClient.Client.RemoteEndPoint!.ToString()!.Split(':')[0];

                if (!IPBlocks.TryGetValue(ipAddress, out DateTime banDate) || banDate < Now)
                {
                    int count = Connections.Count(connection => connection.Connected && connection.IPAddress == ipAddress);

                    if (count >= Settings.MaxIP)
                    {
                        UpdateIPBlock(ipAddress, TimeSpan.FromSeconds(Settings.IPBlockSeconds));

                        MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization((ServerTextKeys.IpAddressDisconnectedTooManyConnections), ipAddress));
                    }
                    else
                    {
                        var tempConnection = new MirConnection(++_sessionID, tempTcpClient);
                        if (tempConnection.Connected)
                        {
                            connected = true;
                            lock (Connections)
                                Connections.Add(tempConnection);
                        }
                    }
                }

                if (!connected)
                    tempTcpClient.Close();
            }
            catch (Exception ex)
            {
                MessageQueue.Enqueue(ex);
            }
            finally
            {
                while (Connections.Count >= Settings.MaxUser)
                    Thread.Sleep(1);

                if (Running && _listener.Server.IsBound)
                    _listener.BeginAcceptTcpClient(Connection, null);
            }
        }

        private void StatusConnection(IAsyncResult result)
        {
            if (!Running || !_StatusPort.Server.IsBound) return;

            try
            {
                var tempTcpClient = _StatusPort.EndAcceptTcpClient(result);
                lock (StatusConnections)
                    StatusConnections.Add(new MirStatusConnection(tempTcpClient));
            }
            catch (Exception ex)
            {
                MessageQueue.Enqueue(ex);
            }
            finally
            {
                while (StatusConnections.Count >= 5) //don't allow to many status port connections it's just an abuse thing
                    Thread.Sleep(1);

                if (Running && _StatusPort.Server.IsBound)
                    _StatusPort.BeginAcceptTcpClient(StatusConnection, null);
            }
        }

        public void NewAccount(NewAccount packet, MirConnection connection)
        {
            if (!Settings.AllowNewAccount)
            {
                connection.Enqueue(new ServerPackets.NewAccount { Result = 0 });
                return;
            }


            if (ConnectionLogs.TryGetValue(connection.IPAddress, out MirConnectionLog? currentLog))
            {
                if (currentLog.AccountsMade.Count > 2)
                {
                    IPBlocks[connection.IPAddress] = Now.AddHours(24);
                    connection.Enqueue(new ServerPackets.NewAccount { Result = 0 });
                    return;
                }
                
                currentLog.AccountsMade.Add(Time);
                for (int i = 0; i < currentLog.AccountsMade.Count; i++)
                {
                    if ((currentLog.AccountsMade[i] + 60 * 60 * 1000) < Time)
                    {
                        currentLog.AccountsMade.RemoveAt(i);
                        break;
                    }
                }
            }
            else
            {
                ConnectionLogs[connection.IPAddress] = new MirConnectionLog() { IPAddress = connection.IPAddress };
            }


            if (!AccountIDReg.IsMatch(packet.AccountID))
            {
                connection.Enqueue(new ServerPackets.NewAccount { Result = 1 });
                return;
            }

            if (!PasswordReg.IsMatch(packet.Password))
            {
                connection.Enqueue(new ServerPackets.NewAccount { Result = 2 });
                return;
            }
            if (!string.IsNullOrWhiteSpace(packet.EMailAddress) && !EMailReg.IsMatch(packet.EMailAddress) ||
                packet.EMailAddress.Length > 50)
            {
                connection.Enqueue(new ServerPackets.NewAccount { Result = 3 });
                return;
            }

            if (!string.IsNullOrWhiteSpace(packet.UserName) && packet.UserName.Length > 20)
            {
                connection.Enqueue(new ServerPackets.NewAccount { Result = 4 });
                return;
            }

            if (!string.IsNullOrWhiteSpace(packet.SecretQuestion) && packet.SecretQuestion.Length > 30)
            {
                connection.Enqueue(new ServerPackets.NewAccount { Result = 5 });
                return;
            }

            if (!string.IsNullOrWhiteSpace(packet.SecretAnswer) && packet.SecretAnswer.Length > 30)
            {
                connection.Enqueue(new ServerPackets.NewAccount { Result = 6 });
                return;
            }

            lock (AccountLock)
            {
                if (AccountExists(packet.AccountID))
                {
                    connection.Enqueue(new ServerPackets.NewAccount { Result = 7 });
                    return;
                }

                AccountList.Add(new AccountInfo(packet) { Index = ++NextAccountID, CreationIP = connection.IPAddress });


                connection.Enqueue(new ServerPackets.NewAccount { Result = 8 });
            }
        }

        public int HTTPNewAccount(ClientPackets.NewAccount packet, string ip)
        {
            if (!Settings.AllowNewAccount)
            {
                return 0;
            }

            if (!AccountIDReg.IsMatch(packet.AccountID))
            {
                return 1;
            }

            if (!PasswordReg.IsMatch(packet.Password))
            {
                return 2;
            }
            if (!string.IsNullOrWhiteSpace(packet.EMailAddress) && !EMailReg.IsMatch(packet.EMailAddress) ||
                packet.EMailAddress.Length > 50)
            {
                return 3;
            }

            if (!string.IsNullOrWhiteSpace(packet.UserName) && packet.UserName.Length > 20)
            {
                return 4;
            }

            if (!string.IsNullOrWhiteSpace(packet.SecretQuestion) && packet.SecretQuestion.Length > 30)
            {
                return 5;
            }

            if (!string.IsNullOrWhiteSpace(packet.SecretAnswer) && packet.SecretAnswer.Length > 30)
            {
                return 6;
            }

            lock (AccountLock)
            {
                if (AccountExists(packet.AccountID))
                {
                    return 7;
                }

                AccountList.Add(new AccountInfo(packet) { Index = ++NextAccountID, CreationIP = ip });
                return 8;
            }
        }

        public void ChangePassword(ClientPackets.ChangePassword p, MirConnection c)
        {
            if (!Settings.AllowChangePassword)
            {
                c.Enqueue(new ServerPackets.ChangePassword { Result = 0 });
                return;
            }

            if (!AccountIDReg.IsMatch(p.AccountID))
            {
                c.Enqueue(new ServerPackets.ChangePassword { Result = 1 });
                return;
            }

            if (!PasswordReg.IsMatch(p.CurrentPassword))
            {
                c.Enqueue(new ServerPackets.ChangePassword { Result = 2 });
                return;
            }

            if (!PasswordReg.IsMatch(p.NewPassword))
            {
                c.Enqueue(new ServerPackets.ChangePassword { Result = 3 });
                return;
            }

            var account = GetAccount(p.AccountID);

            if (account == null)
            {
                c.Enqueue(new ServerPackets.ChangePassword { Result = 4 });
                return;
            }

            if (account.Banned)
            {
                if (account.ExpiryDate > Now)
                {
                    c.Enqueue(new ServerPackets.ChangePasswordBanned { Reason = account.BanReason, ExpiryDate = account.ExpiryDate });
                    return;
                }
                account.Banned = false;
            }
            account.BanReason = string.Empty;
            account.ExpiryDate = DateTime.MinValue;

            p.CurrentPassword = Utils.Crypto.HashPassword(p.CurrentPassword, account.Salt);
            if (string.CompareOrdinal(account.Password, p.CurrentPassword) != 0)
            {
                c.Enqueue(new ServerPackets.ChangePassword { Result = 5 });
                return;
            }

            account.Password = p.NewPassword;
            account.RequirePasswordChange = false;
            c.Enqueue(new ServerPackets.ChangePassword { Result = 6 });
        }
        public void Login(ClientPackets.Login p, MirConnection c)
        {
            if (!Settings.AllowLogin)
            {
                c.Enqueue(new ServerPackets.Login { Result = 0 });
                return;
            }

            if (!AccountIDReg.IsMatch(p.AccountID))
            {
                c.Enqueue(new ServerPackets.Login { Result = 1 });
                return;
            }

            if (!PasswordReg.IsMatch(p.Password))
            {
                c.Enqueue(new ServerPackets.Login { Result = 2 });
                return;
            }
            var account = GetAccount(p.AccountID);

            if (account == null)
            {
                c.Enqueue(new ServerPackets.Login { Result = 3 });
                return;
            }

            if (account.Banned)
            {
                if (account.ExpiryDate > Now)
                {
                    c.Enqueue(new ServerPackets.LoginBanned
                    {
                        Reason = account.BanReason,
                        ExpiryDate = account.ExpiryDate
                    });
                    return;
                }
                account.Banned = false;
            }
            account.BanReason = string.Empty;
            account.ExpiryDate = DateTime.MinValue;

            p.Password = Utils.Crypto.HashPassword(p.Password, account.Salt);

            if (string.CompareOrdinal(account.Password, p.Password) != 0)
            {
                if (account.WrongPasswordCount++ >= 5)
                {
                    account.Banned = true;
                    account.BanReason = GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.TooManyWrongLoginAttempts);
                    account.ExpiryDate = Now.AddMinutes(2);

                    c.Enqueue(new ServerPackets.LoginBanned
                    {
                        Reason = account.BanReason,
                        ExpiryDate = account.ExpiryDate
                    });
                    return;
                }

                c.Enqueue(new ServerPackets.Login { Result = 4 });
                return;
            }
            account.WrongPasswordCount = 0;

            if (account.RequirePasswordChange)
            {
                c.Enqueue(new ServerPackets.Login { Result = 5 });
                return;
            }

            lock (AccountLock)
            {
                account.Connection?.SendDisconnect(1);

                account.Connection = c;
            }

            c.Account = account;
            c.Stage = GameStage.Select;

            account.LastDate = Now;
            account.LastIP = c.IPAddress;

            MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization((ServerTextKeys.UserLoggedIn), account.Connection.SessionID, account.Connection.IPAddress));
            c.Enqueue(new ServerPackets.LoginSuccess { Characters = account.GetSelectInfo() });
        }

        public int HTTPLogin(string accountId, string password)
        {
            if (!Settings.AllowLogin)
            {
                return 0;
            }

            if (!AccountIDReg.IsMatch(accountId))
            {
                return 1;
            }

            if (!PasswordReg.IsMatch(password))
            {
                return 2;
            }

            var account = GetAccount(accountId);

            if (account == null)
            {
                return 3;
            }

            if (account.Banned)
            {
                if (account.ExpiryDate > Now)
                {
                    return 4;
                }
                account.Banned = false;
            }
            account.BanReason = string.Empty;
            account.ExpiryDate = DateTime.MinValue;
            if (string.CompareOrdinal(account.Password, password) != 0)
            {
                if (account.WrongPasswordCount++ >= 5)
                {
                    account.Banned = true;
                    account.BanReason = GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.TooManyWrongLoginAttempts);
                    account.ExpiryDate = Now.AddMinutes(2);
                    return 5;
                }
                return 6;
            }
            account.WrongPasswordCount = 0;
            return 7;
        }

        public void NewCharacter(ClientPackets.NewCharacter p, MirConnection c, bool isGm)
        {
            if (!Settings.AllowNewCharacter)
            {
                c.Enqueue(new ServerPackets.NewCharacter { Result = 0 });
                return;
            }

            if (ConnectionLogs.TryGetValue(c.IPAddress, out MirConnectionLog? currentLog))
            {
                if (currentLog.CharactersMade.Count > 4)
                {
                    IPBlocks[c.IPAddress] = Now.AddHours(24);
                    c.Enqueue(new ServerPackets.NewCharacter { Result = 0 });
                    return;
                }
                currentLog.CharactersMade.Add(Time);
                for (int i = 0; i < currentLog.CharactersMade.Count; i++)
                {
                    if ((currentLog.CharactersMade[i] + 60 * 60 * 1000) < Time)
                    {
                        currentLog.CharactersMade.RemoveAt(i);
                        break;
                    }
                }
            }
            else
            {
                ConnectionLogs[c.IPAddress] = new MirConnectionLog() { IPAddress = c.IPAddress };
            }

            if (!CharacterReg.IsMatch(p.Name) || !isGm && DisabledCharNames.Contains(p.Name.ToUpper()))
            {
                c.Enqueue(new ServerPackets.NewCharacter { Result = 1 });
                return;
            }

            if (p.Gender != MirGender.Male && p.Gender != MirGender.Female)
            {
                c.Enqueue(new ServerPackets.NewCharacter { Result = 2 });
                return;
            }

            if (p.Class != MirClass.Warrior && p.Class != MirClass.Wizard && p.Class != MirClass.Taoist &&
                p.Class != MirClass.Assassin && p.Class != MirClass.Archer ||
                p.Class == MirClass.Assassin && !Settings.AllowCreateAssassin ||
                p.Class == MirClass.Archer && !Settings.AllowCreateArcher)
            {
                c.Enqueue(new ServerPackets.NewCharacter { Result = 3 });
                return;
            }

            var count = 0;

            if (c.Account.Characters.Where(t => !t.Deleted).Any(t => ++count >= Globals.MaxCharacterCount))
            {
                c.Enqueue(new ServerPackets.NewCharacter { Result = 4 });
                return;
            }

            lock (AccountLock)
            {
                if (CharacterExists(p.Name))
                {
                    c.Enqueue(new ServerPackets.NewCharacter { Result = 5 });
                    return;
                }

                var info = new CharacterInfo(p, c) { Index = ++NextCharacterID, AccountInfo = c.Account };

                c.Account.Characters.Add(info);
                CharacterList.Add(info);

                c.Enqueue(new ServerPackets.NewCharacterSuccess { CharInfo = info.ToSelectInfo() });
            }
        }

        public bool CanCreateHero(ClientPackets.NewHero p, MirConnection c, bool IsGm)
        {
            if (!Settings.AllowNewHero)
            {
                c.Enqueue(new S.NewHero { Result = 0 });
                return false;
            }

            if (!CharacterReg.IsMatch(p.Name) || !IsGm && DisabledCharNames.Contains(p.Name.ToUpper()))
            {
                c.Enqueue(new S.NewHero { Result = 1 });
                return false;
            }

            if (p.Gender != MirGender.Male && p.Gender != MirGender.Female)
            {
                c.Enqueue(new S.NewHero { Result = 2 });
                return false;
            }

            if (p.Class != MirClass.Warrior && p.Class != MirClass.Wizard && p.Class != MirClass.Taoist && p.Class != MirClass.Assassin && p.Class != MirClass.Archer || p.Class == MirClass.Warrior && !Settings.Hero_CanCreateClass[0] || p.Class == MirClass.Wizard && !Settings.Hero_CanCreateClass[1] || p.Class == MirClass.Taoist && !Settings.Hero_CanCreateClass[2] || p.Class == MirClass.Assassin && !Settings.Hero_CanCreateClass[3] || p.Class == MirClass.Archer && !Settings.Hero_CanCreateClass[4])
            {
                c.Enqueue(new S.NewHero { Result = 3 });
                return false;
            }

            lock (AccountLock)
            {
                if (CharacterExists(p.Name))
                {
                    c.Enqueue(new S.NewHero { Result = 5 });
                    return false;
                }
            }

            return true;
        }

        public bool AccountExists(string accountId)
        {
            return AccountList.Any(t => string.Compare(t.AccountID, accountId, StringComparison.OrdinalIgnoreCase) == 0);
        }

        private bool CharacterExists(string name)
        {
            return CharacterList.Any(t => string.Compare(t.Name, name, StringComparison.OrdinalIgnoreCase) == 0);
        }

        public List<CharacterInfo> MatchPlayer(string playerId, bool match = false)
        {
            if (string.IsNullOrEmpty(playerId)) return [.. CharacterList];

            Func<string, bool> matcher =
                match ? name => name.Equals(playerId, StringComparison.OrdinalIgnoreCase) : name => name.Contains(playerId, StringComparison.OrdinalIgnoreCase);
            return [..CharacterList.Where(character => matcher(character.Name))];
        }
        
        public List<CharacterInfo> MatchPlayerByItem(string itemIdentifier, bool match = false)
        {
            List<CharacterInfo> list = [];

            bool isNumeric = ulong.TryParse(itemIdentifier, out ulong itemId);

            if (match)
            {
                foreach (var character in CharacterList)
                {
                    foreach (var item in character.Inventory)
                        if (item != null && 
                            ((isNumeric && item.UniqueID == itemId) ||
                             (!isNumeric && item.FriendlyName.Equals(itemIdentifier, StringComparison.OrdinalIgnoreCase))) && !list.Contains(character))
                            list.Add(character);

                    foreach (var item in character.AccountInfo.Storage)
                        if (item != null && ((isNumeric && item.UniqueID == itemId) ||
                                             (!isNumeric && item.FriendlyName.Equals(itemIdentifier, StringComparison.OrdinalIgnoreCase))) && !list.Contains(character))
                            list.Add(character);

                    foreach (var item in character.QuestInventory)
                        if (item != null && ((isNumeric && item.UniqueID == itemId) ||
                                             (!isNumeric && item.FriendlyName.Equals(itemIdentifier, StringComparison.OrdinalIgnoreCase))) && !list.Contains(character))
                            list.Add(character);

                    foreach (var item in character.Equipment)
                        if (item != null && ((isNumeric && item.UniqueID == itemId) ||
                                             (!isNumeric && item.FriendlyName.Equals(itemIdentifier, StringComparison.OrdinalIgnoreCase))) && !list.Contains(character))
                            list.Add(character);

                    foreach (var mail in character.Mail)
                    foreach (var item in mail.Items)
                        if (item != null && ((isNumeric && item.UniqueID == itemId) ||
                                             (!isNumeric && item.FriendlyName.Equals(itemIdentifier, StringComparison.OrdinalIgnoreCase))) && !list.Contains(character))
                            list.Add(character);
                }
            }
            else
            {
                foreach (var character in CharacterList)
                {
                    foreach (var item in character.Inventory)
                        if (item != null && ((isNumeric && item.UniqueID == itemId) ||
                                             (!isNumeric && item.FriendlyName.IndexOf(itemIdentifier, StringComparison.OrdinalIgnoreCase) >= 0)) &&
                            !list.Contains(character))
                            list.Add(character);

                    foreach (var item in character.QuestInventory)
                        if (item != null && ((isNumeric && item.UniqueID == itemId) ||
                                             (!isNumeric && item.FriendlyName.IndexOf(itemIdentifier, StringComparison.OrdinalIgnoreCase) >= 0)) &&
                            !list.Contains(character))
                            list.Add(character);

                    foreach (var item in character.Equipment)
                        if (item != null && ((isNumeric && item.UniqueID == itemId) ||
                                             (!isNumeric && item.FriendlyName.IndexOf(itemIdentifier, StringComparison.OrdinalIgnoreCase) >= 0)) &&
                            !list.Contains(character))
                            list.Add(character);

                    foreach (var item in character.AccountInfo.Storage)
                        if (item != null && ((isNumeric && item.UniqueID == itemId) ||
                                             (!isNumeric && item.FriendlyName.IndexOf(itemIdentifier, StringComparison.OrdinalIgnoreCase) >= 0)) &&
                            !list.Contains(character))
                            list.Add(character);
                }
            }

            return list;
        }

        public AccountInfo? GetAccount(string accountId)
        {
            return AccountList.FirstOrDefault(t => string.Compare(t.AccountID, accountId, StringComparison.OrdinalIgnoreCase) == 0);
        }

        public AccountInfo? GetAccountByCharacter(string name)
        {
            return AccountList.FirstOrDefault(account => account.Characters.Any(t => string.Compare(t.Name, name, StringComparison.OrdinalIgnoreCase) == 0));
        }

        public List<AccountInfo> MatchAccounts(string accountId, bool match = false)
        {
            if (string.IsNullOrEmpty(accountId)) return new List<AccountInfo>(AccountList);
            
            Func<string, bool> finder = match ? (s => s.Equals(accountId, StringComparison.OrdinalIgnoreCase)) : s => s.Contains(accountId, StringComparison.OrdinalIgnoreCase);
            return [.. AccountList.Where(account => finder(account.AccountID))];
        }

        public List<AccountInfo> MatchAccountsByPlayer(string playerName, bool match = false)
        {
            if (string.IsNullOrEmpty(playerName)) return new List<AccountInfo>(AccountList);
            
            Func<string, bool> matcher =
                match ? name => name.Equals(playerName, StringComparison.OrdinalIgnoreCase) : name => name.Contains(playerName, StringComparison.OrdinalIgnoreCase);

            var list = new List<AccountInfo>();
            foreach (var account in AccountList)
            {
                list.AddRange(from t in account.Characters where matcher(t.Name) select account);
            }

            return list;
        }

        public List<AccountInfo> MatchAccountsByIP(string ipAddress, bool matchLastIP = false, bool match = false)
        {
            if (string.IsNullOrEmpty(ipAddress)) return new List<AccountInfo>(AccountList);

            var list = new List<AccountInfo>();

            foreach (var account in AccountList)
            {
                string ipToMatch = matchLastIP ? account.LastIP : account.CreationIP;

                if (match)
                {
                    if (ipToMatch.Equals(ipAddress, StringComparison.OrdinalIgnoreCase))
                        list.Add(account);
                }
                else
                {
                    if (ipToMatch.Contains(ipAddress, StringComparison.OrdinalIgnoreCase))
                        list.Add(account);
                }
            }

            return list;
        }


        public void CreateAccountInfo()
        {
            AccountList.Add(new AccountInfo { Index = ++NextAccountID });
        }
        public void CreateMapInfo()
        {
            MapInfoList.Add(new MapInfo { Index = ++MapIndex });
        }
        public void CreateItemInfo(ItemType type = ItemType.Nothing)
        {
            ItemInfoList.Add(new ItemInfo { Index = ++ItemIndex, Type = type, RandomStatsId = 255 });
        }
        public void CreateMonsterInfo()
        {
            MonsterInfoList.Add(new MonsterInfo { Index = ++MonsterIndex });
        }
        public void CreateNPCInfo()
        {
            NPCInfoList.Add(new NPCInfo { Index = ++NPCIndex });
        }
        public void CreateQuestInfo()
        {
            QuestInfoList.Add(new QuestInfo { Index = ++QuestIndex });
        }

        public void AddToGameShop(ItemInfo info)
        {
            GameShopList.Add(new GameShopItem
            {
                GIndex = ++GameshopIndex,
                GoldPrice = (uint)(1000 * Settings.CredxGold),
                CreditPrice = 1000,
                ItemIndex = info.Index,
                Info = info,
                Date = Now,
                Class = "All",
                Category = info.Type.ToString()
            });
        }

        public void Remove(MapInfo info)
        {
            MapInfoList.Remove(info);
            //Desync all objects\
        }
        public void Remove(ItemInfo info)
        {
            ItemInfoList.Remove(info);
        }
        public void Remove(MonsterInfo info)
        {
            MonsterInfoList.Remove(info);
            //Desync all objects\
        }
        public void Remove(NPCInfo info)
        {
            NPCInfoList.Remove(info);
            //Desync all objects\
        }
        public void Remove(QuestInfo info)
        {
            QuestInfoList.Remove(info);
            //Desync all objects\
        }

        public void Remove(GameShopItem info)
        {
            GameShopList.Remove(info);

            if (GameShopList.Count == 0)
            {
                GameshopIndex = 0;
            }

            //Desync all objects\
        }

        public UserItem CreateFreshItem(ItemInfo info)
        {
            var item = new UserItem(info)
            {
                UniqueID = ++NextUserItemID,
                CurrentDura = info.Durability,
                MaxDura = info.Durability
            };

            UpdateItemExpiry(item);

            return item;
        }
        public UserItem? CreateDropItem(int index)
        {
            return CreateDropItem(GetItemInfo(index));
        }
        public UserItem? CreateDropItem(ItemInfo? info)
        {
            if (info == null) return null;

            var item = new UserItem(info)
            {
                UniqueID = ++NextUserItemID,
                MaxDura = info.Durability,
                CurrentDura = (ushort)Math.Min(info.Durability, RandomProvider.Next(info.Durability) + 1000)
            };

            UpgradeItem(item);

            UpdateItemExpiry(item);

            if (!info.NeedIdentify) item.Identified = true;
            return item;
        }

        public static UserItem? CreateShopItem(ItemInfo? info, ulong id)
        {
            if (info == null) return null;

            var item = new UserItem(info)
            {
                UniqueID = id,
                CurrentDura = info.Durability,
                MaxDura = info.Durability,
                IsShopItem = true,
            };

            return item;
        }

        private void UpdateItemExpiry(UserItem item)
        {
            var expiryInfo = new ExpireInfo();

            var r = new Regex(@"\[(.*?)\]");
            var expiryMatch = r.Match(item.Info.Name);

            if (expiryMatch.Success)
            {
                var parameter = expiryMatch.Groups[1].Captures[0].Value;

                var numAlpha = new Regex("(?<Numeric>[0-9]*)(?<Alpha>[a-zA-Z]*)");
                var match = numAlpha.Match(parameter);

                var alpha = match.Groups["Alpha"].Value;

                if (TryParse(match.Groups["Numeric"].Value, out int num))
                {
                    expiryInfo.ExpiryDate = alpha switch
                    {
                        "m" => Now.AddMinutes(num),
                        "h" => Now.AddHours(num),
                        "d" => Now.AddDays(num),
                        "M" => Now.AddMonths(num),
                        "y" => Now.AddYears(num),
                        _ => DateTime.MaxValue
                    };
                }
                else
                {
                    expiryInfo.ExpiryDate = DateTime.MaxValue;
                }

                item.ExpireInfo = expiryInfo;
            }
        }

        private void UpgradeItem(UserItem item)
        {
            if (item.Info.RandomStats == null) return;
            
            var stat = item.Info.RandomStats;
            if (stat.MaxDuraChance > 0 && RandomProvider.Next(stat.MaxDuraChance) == 0)
            {
                var dura = RandomRange(stat.MaxDuraMaxStat, stat.MaxDuraStatChance);
                item.MaxDura = (ushort)Math.Min(ushort.MaxValue, item.MaxDura + dura * 1000);
                item.CurrentDura = (ushort)Math.Min(ushort.MaxValue, item.CurrentDura + dura * 1000);
            }

            if (stat.MaxAcChance > 0 && RandomProvider.Next(stat.MaxAcChance) == 0) 
                item.AddedStats[Stat.MaxAC] = (byte)(RandomRange(stat.MaxAcMaxStat - 1, stat.MaxAcStatChance) + 1);
            if (stat.MaxMacChance > 0 && RandomProvider.Next(stat.MaxMacChance) == 0) 
                item.AddedStats[Stat.MaxMAC] = (byte)(RandomRange(stat.MaxMacMaxStat - 1, stat.MaxMacStatChance) + 1);
            if (stat.MaxDcChance > 0 && RandomProvider.Next(stat.MaxDcChance) == 0) 
                item.AddedStats[Stat.MaxDC] = (byte)(RandomRange(stat.MaxDcMaxStat - 1, stat.MaxDcStatChance) + 1);
            if (stat.MaxMcChance > 0 && RandomProvider.Next(stat.MaxMcChance) == 0) 
                item.AddedStats[Stat.MaxMC] = (byte)(RandomRange(stat.MaxMcMaxStat - 1, stat.MaxMcStatChance) + 1);
            if (stat.MaxScChance > 0 && RandomProvider.Next(stat.MaxScChance) == 0) 
                item.AddedStats[Stat.MaxSC] = (byte)(RandomRange(stat.MaxScMaxStat - 1, stat.MaxScStatChance) + 1);
            if (stat.AccuracyChance > 0 && RandomProvider.Next(stat.AccuracyChance) == 0) 
                item.AddedStats[Stat.Accuracy] = (byte)(RandomRange(stat.AccuracyMaxStat - 1, stat.AccuracyStatChance) + 1);
            if (stat.AgilityChance > 0 && RandomProvider.Next(stat.AgilityChance) == 0) 
                item.AddedStats[Stat.Agility] = (byte)(RandomRange(stat.AgilityMaxStat - 1, stat.AgilityStatChance) + 1);
            if (stat.HpChance > 0 && RandomProvider.Next(stat.HpChance) == 0) 
                item.AddedStats[Stat.HP] = (byte)(RandomRange(stat.HpMaxStat - 1, stat.HpStatChance) + 1);
            if (stat.MpChance > 0 && RandomProvider.Next(stat.MpChance) == 0) 
                item.AddedStats[Stat.MP] = (byte)(RandomRange(stat.MpMaxStat - 1, stat.MpStatChance) + 1);
            if (stat.StrongChance > 0 && RandomProvider.Next(stat.StrongChance) == 0) 
                item.AddedStats[Stat.Strong] = (byte)(RandomRange(stat.StrongMaxStat - 1, stat.StrongStatChance) + 1);
            if (stat.MagicResistChance > 0 && RandomProvider.Next(stat.MagicResistChance) == 0) 
                item.AddedStats[Stat.MagicResist] = (byte)(RandomRange(stat.MagicResistMaxStat - 1, stat.MagicResistStatChance) + 1);
            if (stat.PoisonResistChance > 0 && RandomProvider.Next(stat.PoisonResistChance) == 0) 
                item.AddedStats[Stat.PoisonResist] = (byte)(RandomRange(stat.PoisonResistMaxStat - 1, stat.PoisonResistStatChance) + 1);
            if (stat.HpRecovChance > 0 && RandomProvider.Next(stat.HpRecovChance) == 0) 
                item.AddedStats[Stat.HealthRecovery] = (byte)(RandomRange(stat.HpRecovMaxStat - 1, stat.HpRecovStatChance) + 1);
            if (stat.MpRecovChance > 0 && RandomProvider.Next(stat.MpRecovChance) == 0) 
                item.AddedStats[Stat.SpellRecovery] = (byte)(RandomRange(stat.MpRecovMaxStat - 1, stat.MpRecovStatChance) + 1);
            if (stat.PoisonRecovChance > 0 && RandomProvider.Next(stat.PoisonRecovChance) == 0) 
                item.AddedStats[Stat.PoisonRecovery] = (byte)(RandomRange(stat.PoisonRecovMaxStat - 1, stat.PoisonRecovStatChance) + 1);
            if (stat.CriticalRateChance > 0 && RandomProvider.Next(stat.CriticalRateChance) == 0) 
                item.AddedStats[Stat.CriticalRate] = (byte)(RandomRange(stat.CriticalRateMaxStat - 1, stat.CriticalRateStatChance) + 1);
            if (stat.CriticalDamageChance > 0 && RandomProvider.Next(stat.CriticalDamageChance) == 0) 
                item.AddedStats[Stat.CriticalDamage] = (byte)(RandomRange(stat.CriticalDamageMaxStat - 1, stat.CriticalDamageStatChance) + 1);
            if (stat.FreezeChance > 0 && RandomProvider.Next(stat.FreezeChance) == 0) 
                item.AddedStats[Stat.Freezing] = (byte)(RandomRange(stat.FreezeMaxStat - 1, stat.FreezeStatChance) + 1);
            if (stat.PoisonAttackChance > 0 && RandomProvider.Next(stat.PoisonAttackChance) == 0) 
                item.AddedStats[Stat.PoisonAttack] = (byte)(RandomRange(stat.PoisonAttackMaxStat - 1, stat.PoisonAttackStatChance) + 1);
            if (stat.AttackSpeedChance > 0 && RandomProvider.Next(stat.AttackSpeedChance) == 0) 
                item.AddedStats[Stat.AttackSpeed] = (sbyte)(RandomRange(stat.AttackSpeedMaxStat - 1, stat.AttackSpeedStatChance) + 1);
            if (stat.LuckChance > 0 && RandomProvider.Next(stat.LuckChance) == 0) 
                item.AddedStats[Stat.Luck] = (sbyte)(RandomRange(stat.LuckMaxStat - 1, stat.LuckStatChance) + 1);
            if (stat.CurseChance > 0 && RandomProvider.Next(100) <= stat.CurseChance) 
                item.Cursed = true;

            if (stat.SlotChance > 0 && RandomProvider.Next(stat.SlotChance) == 0)
            {
                var slot = (byte)(RandomRange(stat.SlotMaxStat - 1, stat.SlotStatChance) + 1);

                if (slot > item.Info.Slots)
                {
                    item.SetSlotSize(slot);
                }
            }
        }

        public int RandomRange(int count, int rate)
        {
            var x = 0;
            for (var i = 0; i < count; i++) if (RandomProvider.Next(rate) == 0) x++;
            return x;
        }
        public bool BindItem(UserItem item)
        {
            foreach (var info in ItemInfoList.Where(info => info.Index == item.ItemIndex))
            {
                item.Info = info;
                return BindSlotItems(item);
            }

            return false;
        }

        private static bool BindGameShop(GameShopItem item, bool editEnvir = true)
        {
            var itemInfo = Edit.ItemInfoList.FirstOrDefault(info => info.Index == item.ItemIndex);
            if (itemInfo == null) return false;
            
            item.Info = itemInfo;
            return true;
        }

        private bool BindSlotItems(UserItem item)
        {
            return item.Slots.Where(slot => slot != null).All(BindItem);
        }

        public bool BindQuest(QuestProgressInfo quest)
        {
            var questInfo = QuestInfoList.FirstOrDefault(info => info.Index == quest.Index);
            if (questInfo == null) return false;
            quest.Info = questInfo;
            return true;
        }

        public Map? GetMap(int index)
        {
            return MapList.FirstOrDefault(t => t.Info.Index == index);
        }

        public Map? GetMap(string name, bool strict = true)
        {
            return MapList.FirstOrDefault(t => strict ? 
                string.Equals(t.Info.Title, name, StringComparison.CurrentCultureIgnoreCase) : 
                t.Info.Title.StartsWith(name, StringComparison.CurrentCultureIgnoreCase));
        }

        public Map? GetWorldMap(string name)
        {
            return MapList.FirstOrDefault(t => t.Info.Title.StartsWith(name, StringComparison.CurrentCultureIgnoreCase) && t.Info.BigMap > 0);
        }

        public MapInfo? GetMapInfo(int index)
        {
            return MapInfoList.FirstOrDefault(t => t.Index == index);
        }

        public Map? GetMapByNameAndInstance(string name, int instanceValue = 0)
        {
            if (instanceValue < 0) instanceValue = 0;
            if (instanceValue > 0) instanceValue--;

            var instanceMapList = MapList.Where(t => string.Equals(t.Info.FileName, name, StringComparison.CurrentCultureIgnoreCase)).ToList();
            return instanceValue < instanceMapList.Count ? instanceMapList[instanceValue] : null;
        }

        public MapObject? GetObject(uint objectID)
        {
            return Objects.FirstOrDefault(e => e.ObjectID == objectID);
        }

        public List<MapObject> GetObjects(int map, ObjectType race)
        {
            return [.. Objects.Where(x => x.CurrentMapIndex == map && x.Race == race)];
        }

        public MonsterInfo? GetMonsterInfo(int index)
        {
            return MonsterInfoList.FirstOrDefault(t => t.Index == index);
        }

        public NPCInfo? GetNPCInfo(int index)
        {
            return NPCInfoList.FirstOrDefault(t => t.Index == index);
        }

        public MonsterInfo? GetMonsterInfo(int ai, int effect)
        {
            return MonsterInfoList.FirstOrDefault(t => t.AI == ai && (t.Effect == effect || effect < 0));
        }

        public NPCObject? GetNPC(string name)
        {
            return MapList.SelectMany(t1 => t1.NPCs.Where(t => t.Info.Name == name)).FirstOrDefault();
        }

        public NPCObject? GetWorldMapNPC(string name)
        {
            return MapList.SelectMany(t1 => t1.NPCs.Where(t => t.Info.GameName.StartsWith(name, StringComparison.CurrentCultureIgnoreCase) && t.Info.ShowOnBigMap)).FirstOrDefault();
        }

        public MonsterInfo? GetMonsterInfo(int id, bool strict = false)
        {
            string? monsterName = MonsterInfoList.FirstOrDefault(x => x.Index == id)?.Name;

            return monsterName == null ? null : GetMonsterInfo(monsterName, strict);
        }


        public MonsterInfo? GetMonsterInfo(string name, bool Strict = false)
        {
            if (Strict)
            {
                return MonsterInfoList.FirstOrDefault(monsterInfo => monsterInfo.Name == name);
            }

            return MonsterInfoList.FirstOrDefault(monsterInfo =>
                monsterInfo.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                monsterInfo.Name.Replace(" ", string.Empty).Equals(name.Replace(" ", string.Empty), StringComparison.OrdinalIgnoreCase));
        }


        public PlayerObject? GetPlayer(string name)
        {
            return Players.FirstOrDefault(t => string.Compare(t.Name, name, StringComparison.OrdinalIgnoreCase) == 0);
        }
        
        
        public PlayerObject? GetPlayer(uint PlayerId)
        {
            return Players.FirstOrDefault(t => t.Info.Index == PlayerId);
        }
        
        
        public CharacterInfo? GetCharacterInfo(string name)
        {
            return CharacterList.FirstOrDefault(t => string.Compare(t.Name, name, StringComparison.OrdinalIgnoreCase) == 0);
        }

        public CharacterInfo? GetCharacterInfo(int index)
        {
            return CharacterList.FirstOrDefault(t => t.Index == index);
        }
        
        public HeroInfo? GetHeroInfo(int index)
        {
            return HeroList.FirstOrDefault(x => x.Index == index);
        }

        public ItemInfo? GetItemInfo(int index)
        {
            return ItemInfoList.FirstOrDefault(info => info.Index == index);
        }

        public ItemInfo? GetItemInfo(string name)
        {
            return ItemInfoList.FirstOrDefault(info => string.Compare(info.Name.Replace(" ", ""), name, StringComparison.OrdinalIgnoreCase) == 0);
        }

        public QuestInfo? GetQuestInfo(int index)
        {
            return QuestInfoList.FirstOrDefault(info => info.Index == index);
        }

        public ItemInfo? GetBook(short Skill)
        {
            return ItemInfoList.FirstOrDefault(info => info.Type == ItemType.Book && info.Shape == Skill);
        }

        public BuffInfo? GetBuffInfo(BuffType type)
        {
            return BuffInfoList.FirstOrDefault(info => info.Type == type);
        }

        public void MessageAccount(AccountInfo account, string message, ChatType type)
        {
            var player = account.Characters.FirstOrDefault(t => t.Player != null)?.Player;
            player?.ReceiveChat(message, type);
        }


        public void MailCharacter(CharacterInfo info, UserItem? item = null, uint gold = 0, int reason = 0, string? customMessage = null)
        {
            string sender = "Bichon Administrator";

            string message = "You have been mailed due to the following reason:\r\n\r\n";

            message += reason switch
            {
                1 => "Could not return item to bag after trade.",
                99 => "Code didn't correctly handle checking inventory space.",
                _ => customMessage ?? "No reason provided."
            };

            MailInfo mail = new MailInfo(info.Index)
            {
                Sender = sender,
                Message = message,
                Gold = gold
            };

            if (item != null)
            {
                mail.Items.Add(item);
            }

            mail.Send();
        }

        public GuildObject? GetGuild(string name)
        {
            return Guilds.FirstOrDefault(t => string.Compare(t.Name.Replace(" ", ""), name, StringComparison.OrdinalIgnoreCase) == 0);
        }
        
        
        public GuildObject? GetGuild(int index)
        {
            return Guilds.FirstOrDefault(t => t.Guildindex == index);
        }

        public void ProcessNewDay()
        {
            foreach (var c in CharacterList)
            {
                ClearDailyQuests(c);

                c.NewDay = true;

                c.Player?.CallDefaultNPC(DefaultNPCType.Daily);
            }
        }

        private void ProcessRentedItems()
        {
            foreach (var characterInfo in CharacterList)
            {
                if (characterInfo.RentedItems.Count <= 0)
                {
                    continue;
                }

                foreach (var rentedItemInfo in characterInfo.RentedItems)
                {
                    if (rentedItemInfo.ItemReturnDate >= Now)
                        continue;

                    CharacterInfo? rentingPlayer = GetCharacterInfo(rentedItemInfo.RentingPlayerName);
                    if (rentingPlayer == null)
                        continue;

                    for (var i = 0; i < rentingPlayer!.Inventory.Length; i++)
                    {
                        if (rentedItemInfo.ItemId != rentingPlayer.Inventory[i]?.UniqueID)
                        {
                            continue;
                        }

                        var item = rentingPlayer.Inventory[i];

                        if (item?.RentalInformation == null)
                        {
                            continue;
                        }

                        if (Now <= item.RentalInformation.ExpiryDate)
                        {
                            continue;
                        }

                        ReturnRentalItem(item, item.RentalInformation.OwnerName, rentingPlayer, false);
                        rentingPlayer.Inventory[i] = null;
                        rentingPlayer.HasRentedItem = false;

                        if (rentingPlayer.Player == null)
                        {
                            continue;
                        }

                        rentingPlayer.Player.ReceiveChat(GameLanguage.ServerTextMap.GetLocalization((ServerTextKeys.ItemExpiredFromInventory), item.Info.FriendlyName), ChatType.Hint);
                        rentingPlayer.Player.Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });
                        rentingPlayer.Player.RefreshStats();
                    }

                    for (var i = 0; i < rentingPlayer.Equipment.Length; i++)
                    {
                        var item = rentingPlayer.Equipment[i];

                        if (item?.RentalInformation == null)
                        {
                            continue;
                        }

                        if (Now <= item.RentalInformation.ExpiryDate)
                        {
                            continue;
                        }

                        ReturnRentalItem(item, item.RentalInformation.OwnerName, rentingPlayer, false);
                        rentingPlayer.Equipment[i] = null;
                        rentingPlayer.HasRentedItem = false;

                        if (rentingPlayer.Player == null)
                        {
                            continue;
                        }

                        rentingPlayer.Player.ReceiveChat(GameLanguage.ServerTextMap.GetLocalization((ServerTextKeys.ItemExpiredInventory), item.Info.FriendlyName), ChatType.Hint);
                        rentingPlayer.Player.Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });
                        rentingPlayer.Player.RefreshStats();
                    }
                }
            }

            foreach (var characterInfo in CharacterList.Where(characterInfo => characterInfo.RentedItemsToRemove.Count > 0))
            {
                foreach (var rentalInformationToRemove in characterInfo.RentedItemsToRemove)
                {
                    characterInfo.RentedItems.Remove(rentalInformationToRemove);
                }

                characterInfo.RentedItemsToRemove.Clear();
            }
        }

        public bool ReturnRentalItem(UserItem rentedItem, string ownerName, CharacterInfo rentingCharacterInfo, bool removeNow = true)
        {
            if (rentedItem.RentalInformation == null)
            {
                return false;
            }

            CharacterInfo? owner = GetCharacterInfo(ownerName);
            if (owner == null)
            {
                return false;
            }

            var returnItems = new List<UserItem>();

            foreach (var rentalInformation in owner.RentedItems.Where(rentalInformation => rentalInformation.ItemId == rentedItem.UniqueID))
            {
                owner.RentedItemsToRemove.Add(rentalInformation);
            }

            rentedItem.RentalInformation.BindingFlags = BindMode.None;
            rentedItem.RentalInformation.RentalLocked = true;
            rentedItem.RentalInformation.ExpiryDate = rentedItem.RentalInformation.ExpiryDate.AddDays(1);

            returnItems.Add(rentedItem);

            var mail = new MailInfo(owner.Index, true)
            {
                Sender = rentingCharacterInfo.Name,
                Message = rentedItem.Info.FriendlyName,
                Items = returnItems
            };

            mail.Send();

            if (removeNow)
            {
                foreach (var rentalInformationToRemove in owner.RentedItemsToRemove)
                {
                    owner.RentedItems.Remove(rentalInformationToRemove);
                }

                owner.RentedItemsToRemove.Clear();
            }

            return true;
        }

        private void ClearDailyQuests(CharacterInfo info)
        {
            foreach (var quest in QuestInfoList.Where(quest => quest.Type == QuestType.Daily))
            {
                for (var i = 0; i < info.CompletedQuests.Count; i++)
                {
                    if (info.CompletedQuests[i] != quest.Index) continue;

                    //TODO FIX 
                    info.CompletedQuests.RemoveAt(i);
                }
            }

            info.Player?.GetCompletedQuests();
        }

        public GuildBuffInfo? FindGuildBuffInfo(int Id)
        {
            return Settings.Guild_BuffList.FirstOrDefault(t => t.Id == Id);
        }

        public void ClearGameShopLog()
        {
            Main.GameshopLog.Clear();

            foreach (var account in AccountList)
            {
                foreach (var character in account.Characters)
                {
                    character.GSpurchases.Clear();
                }
            }

            ResetGS = false;
            MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.GameshopPurchaseLogsCleared));
        }

        public void Inspect(MirConnection con, uint id)
        {
            if (ObjectID == id) return;

            PlayerObject? player = Players.SingleOrDefault(x => x.ObjectID == id || x.Pets.Any(y => y.ObjectID == id && y is HumanWizard));

            if (player == null) return;
            
            Inspect(con, player.Info.Index);
        }

        public void Inspect(MirConnection con, int id)
        {
            if (ObjectID == id) return;

            CharacterInfo? player = GetCharacterInfo(id);
            if (player == null) return;

            CharacterInfo? Lover = null;
            string loverName = "";

            if (player.Married != 0) Lover = GetCharacterInfo(player.Married);

            if (Lover != null)
            {
                loverName = Lover.Name;
            }

            foreach (var u in player.Equipment.Where(u => u != null))
            {
                con.CheckItem(u);
            }

            string guildName = "";
            string guildRankName = "";
            GuildRank? guildRank = null;
            if (player.GuildIndex != -1)
            {
                var guild = GetGuild(player.GuildIndex);
                if (guild != null)
                {
                    guildRank = guild.FindRank(player.Name);
                    if (guildRank == null)
                    {
                        guild = null;
                    }
                    else
                    {
                        guildName = guild.Name;
                        guildRankName = guildRank.Name;
                    }
                }
            }

            con.Enqueue(new S.PlayerInspect
            {
                Name = player.Name,
                Equipment = player.Equipment,
                GuildName = guildName,
                GuildRank = guildRankName,
                Hair = player.Hair,
                Gender = player.Gender,
                Class = player.Class,
                Level = player.Level,
                LoverName = loverName,
                AllowObserve = player.AllowObserve && Settings.AllowObserve
            });
        }

        public void InspectHero(MirConnection con, int id)
        {
            if (ObjectID == id)
            {
                return;
            }

            HeroObject? heroObject = Heroes.SingleOrDefault(h => h.ObjectID == id);

            if (heroObject == null)
            {
                return;
            }

            HeroInfo? heroInfo = GetHeroInfo(heroObject.Info.Index);

            if (heroInfo == null)
            {
                return;
            }

            foreach (var u in heroInfo.Equipment.Where(u => u != null))
            {
                con.CheckItem(u!);
            }

            var ownerName = heroObject.Owner.Name;

            con.Enqueue(new S.PlayerInspect
            {
                Name = GameLanguage.ServerTextMap.GetLocalization((ServerTextKeys.PlayerHero), ownerName),
                Equipment = heroInfo.Equipment,
                GuildName = string.Empty,
                GuildRank = string.Empty,
                Hair = heroInfo.Hair,
                Gender = heroInfo.Gender,
                Class = heroInfo.Class,
                Level = heroInfo.Level,
                LoverName = string.Empty,
                AllowObserve = false,
                IsHero = true
            });
        }

        public void Observe(MirConnection con, string Name)
        {
            var player = GetPlayer(Name);

            if (player == null) return;
            
            if (!player.AllowObserve || !Settings.AllowObserve) return;

            player.AddObserver(con);
        }

        public void GetRanking(MirConnection con, byte RankType, int RankIndex, bool OnlineOnly)
        {
            if (RankType > 6) return;
            
            List<RankCharacterInfo>? listings = RankType == 0 ? RankTop : RankClass[RankType - 1];

            if (RankIndex >= listings.Count || RankIndex < 0) return;

            S.Rankings p = new S.Rankings
            {
                RankType = RankType,
                Count = OnlineOnly ? OnlineRankingCount[RankType] : listings.Count
            };

            if (con.Player != null)
            {
                if (RankType == 0)
                    p.MyRank = con.Player.Info.Rank[0];
                else
                    p.MyRank = (byte)con.Player.Class == (RankType - 1) ? con.Player.Info.Rank[1] : 0;
            }

            int c = 0;
            for (int i = RankIndex; i < listings.Count; i++)
            {
                if (OnlineOnly && GetPlayer(listings[i].Name) == null) continue;

                if (!CheckListing(con, listings[i]))
                    p.ListingDetails.Add(listings[i]);
                p.Listings.Add(listings[i].PlayerId);
                c++;

                if (c > 19 || c >= p.Count) break;
            }

            con.Enqueue(p);
        }

        private static bool CheckListing(MirConnection con, RankCharacterInfo listing)
        {
            if (!con.SentRankings.TryGetValue(listing.PlayerId, out var lastUpdated))
            {
                con.SentRankings.Add(listing.PlayerId, listing.LastUpdated);
                return false;
            }

            if (lastUpdated != listing.LastUpdated)
            {
                con.SentRankings[listing.PlayerId] = lastUpdated;
                return false;
            }

            return true;
        }

        public int InsertRank(List<RankCharacterInfo> Ranking, RankCharacterInfo NewRank)
        {
            if (Ranking.Count == 0)
            {
                Ranking.Add(NewRank);
                return Ranking.Count;
            }

            for (var i = 0; i < Ranking.Count; i++)
            {
                //if level is lower
                if (Ranking[i].level < NewRank.level)
                {
                    Ranking.Insert(i, NewRank);
                    return i + 1;
                }

                //if exp is lower but level = same
                if (Ranking[i].level == NewRank.level && Ranking[i].Experience < NewRank.Experience)
                {
                    Ranking.Insert(i, NewRank);
                    return i + 1;
                }
            }

            Ranking.Add(NewRank);
            return Ranking.Count;
        }

        public bool TryAddRank(List<RankCharacterInfo> Ranking, CharacterInfo info, byte type)
        {
            var NewRank = new RankCharacterInfo() { Name = info.Name, Class = info.Class, Experience = info.Experience, level = info.Level, PlayerId = info.Index, info = info, LastUpdated = Now };
            var NewRankIndex = InsertRank(Ranking, NewRank);
            if (NewRankIndex == 0) return false;
            for (var i = NewRankIndex; i < Ranking.Count; i++)
            {
                SetNewRank(Ranking[i], i + 1, type);
            }
            info.Rank[type] = NewRankIndex;
            return true;
        }

        public int FindRank(List<RankCharacterInfo> Ranking, CharacterInfo info, byte type)
        {
            var startIndex = info.Rank[type];
            if (startIndex > 0) //if there's a previously known rank then the user can only have gone down in the ranking (or stayed the same)
            {
                for (var i = startIndex - 1; i < Ranking.Count; i++)
                {
                    if (Ranking[i].Name == info.Name)
                        return i;
                }
                info.Rank[type] = 0;//set the rank to 0 to tell future searches it's not there anymore
            }
            return -1;//index can be 0
        }

        public bool UpdateRank(List<RankCharacterInfo> Ranking, CharacterInfo info, byte type)
        {
            var CurrentRank = FindRank(Ranking, info, type);
            if (CurrentRank == -1) return false;//not in ranking list atm

            var NewRank = CurrentRank;
            //next find our updated rank
            for (var i = CurrentRank - 1; i >= 0; i--)
            {
                if (Ranking[i].level > info.Level || Ranking[i].level == info.Level && Ranking[i].Experience > info.Experience) break;
                NewRank = i;
            }

            Ranking[CurrentRank].level = info.Level;
            Ranking[CurrentRank].Experience = info.Experience;
            Ranking[CurrentRank].LastUpdated = Now;

            if (NewRank < CurrentRank)
            {//if we gained any ranks
                Ranking.Insert(NewRank, Ranking[CurrentRank]);
                Ranking.RemoveAt(CurrentRank + 1);
                for (var i = NewRank + 1; i < Math.Min(Ranking.Count, CurrentRank + 1); i++)
                {
                    SetNewRank(Ranking[i], i + 1, type);
                }
            }
            info.Rank[type] = NewRank + 1;

            return true;
        }

        public void SetNewRank(RankCharacterInfo Rank, int Index, byte type)
        {
            Rank.LastUpdated = Now;
            if (Rank.info is not CharacterInfo Player) return;
            Player.Rank[type] = Index;
        }

        public void RemoveRank(CharacterInfo info)
        {
            var rankIndex = -1;
            //first check overall top           
            var ranking = RankTop;
            rankIndex = FindRank(ranking, info, 0);
            if (rankIndex >= 0)
            {
                ranking.RemoveAt(rankIndex);
                for (var i = rankIndex; i < ranking.Count; i++)
                {
                    SetNewRank(ranking[i], i, 0);
                }
            }

            //next class based top
            ranking = RankTop;
            rankIndex = FindRank(ranking, info, 1);
            if (rankIndex >= 0)
            {
                ranking.RemoveAt(rankIndex);
                for (var i = rankIndex; i < ranking.Count; i++)
                {
                    SetNewRank(ranking[i], i, 1);
                }
            }
        }

        public void CheckRankUpdate(CharacterInfo info)
        {
            //first check overall top           
            List<RankCharacterInfo>? ranking = RankTop;
            if (!UpdateRank(ranking, info, 0))
            {
                TryAddRank(ranking, info, 0);
            }

            //now check class top

            ranking = RankClass[(byte)info.Class];
            if (!UpdateRank(ranking, info, 1))
            {
                TryAddRank(ranking, info, 1);
            }
        }


        public void ReloadNPCs()
        {
            SaveGoods(true);

            Robot.Clear();

            var keys = Scripts.Keys;

            foreach (var key in keys)
            {
                Scripts[key].Load();
            }

            MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.NpcScriptsReloaded));
        }

        public void ReloadDrops()
        {
            foreach (var monsterInfo in MonsterInfoList)
            {
                string path = Path.Combine(Settings.DropPath, monsterInfo.Name + ".txt");

                if (!string.IsNullOrEmpty(monsterInfo.DropPath))
                {
                    path = Path.Combine(Settings.DropPath, monsterInfo.DropPath + ".txt");
                }

                monsterInfo.Drops.Clear();

                DropInfo.Load(monsterInfo.Drops, monsterInfo.Name, path, 0, true);
            }

            FishingDrops.Clear();
            for (int i = 0; i < 19; i++)
            {
                var path = Path.Combine(Settings.DropPath, Settings.FishingDropFilename + ".txt");
                path = path.Replace("00", i.ToString("D2"));

                DropInfo.Load(FishingDrops, $"Fishing {i}", path, (byte)i, i < 2);
            }

            AwakeningDrops.Clear();
            DropInfo.Load(AwakeningDrops, "Awakening", Path.Combine(Settings.DropPath, Settings.AwakeningDropFilename + ".txt"));

            StrongboxDrops.Clear();
            DropInfo.Load(StrongboxDrops, "StrongBox", Path.Combine(Settings.DropPath, Settings.StrongboxDropFilename + ".txt"));

            BlackstoneDrops.Clear();
            DropInfo.Load(BlackstoneDrops, "Blackstone", Path.Combine(Settings.DropPath, Settings.BlackstoneDropFilename + ".txt"));

            MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.DropsLoaded));
        }

        public void ReloadLineMessages()
        {
            LineMessages.Clear();

            var path = Path.Combine(Settings.EnvirPath, "LineMessage.txt");

            if (!File.Exists(path))
            {
                File.WriteAllText(path, "");
            }
            else
            {
                var lines = File.ReadAllLines(path);

                foreach (string line in lines)
                {
                    if (line.StartsWith(';') || string.IsNullOrWhiteSpace(line)) continue;
                    LineMessages.Add(line);
                }

                MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization(ServerTextKeys.LineMessagesReloaded));
            }
        }

        private WorldMapIcon? ValidateWorldMap()
        {
            return (from wmi in Settings.WorldMapSetup.Icons let info = GetMapInfo(wmi.MapIndex) where info == null select wmi).FirstOrDefault();
        }

        public void DeleteGuild(GuildObject guild)
        {
            Guilds.Remove(guild);
            GuildList.Remove(guild.Info);

            GuildRefreshNeeded = true;
            MessageQueue.Enqueue(GameLanguage.ServerTextMap.GetLocalization((ServerTextKeys.GuildWillBeDeletedFromServer), guild.Info.Name));
        }
    }
}