using Server.MirDatabase;
using Server.MirEnv;
using Server.MirObjects;
using Server.MirObjects.Monsters;

namespace Server.Library.MirDatabase.Conquest;

public class GuildWallInfo
{
    protected static Env Env => Env.Main;

    public int Index;
    public int Health;

    public WallInfo? Info;

    public ConquestObject? Conquest;

    public Wall? Wall;


    public GuildWallInfo() { }
    public GuildWallInfo(BinaryReader reader)
    {
        Index = reader.ReadInt32();

        if (Env.LoadVersion <= 84)
        {
            Health = (int)reader.ReadUInt32();
        }
        else
        {
            Health = reader.ReadInt32();
        }
    }

    public void Save(BinaryWriter writer)
    {
        if (Wall != null) Health = Wall.HP;
        writer.Write(Index);
        writer.Write(Health);
    }

    public void Spawn(bool repair)
    {
        Wall?.Despawn();
        if (Info == null) return;

        MonsterInfo? monsterInfo = Env.GetMonsterInfo(Info.MobIndex);

        if (monsterInfo == null) return;

        if (monsterInfo.AI != 82) return;

        Wall = (Wall)MonsterObject.GetMonster(monsterInfo);

        if (Wall == null) return;

        Wall.Conquest = Conquest;
        Wall.WallIndex = Index;

        if (Conquest != null)
            Wall.Spawn(Conquest.ConquestMap, Info.Location);

        if (repair) Health = Wall.Stats[Stat.HP];

        if (Health == 0)
            Wall.Die();
        else
            Wall.SetHP(Health);

        Wall.CheckDirection();
    }

    public int GetRepairCost()
    {
        int cost = 0;

        if (Wall == null) return 0;

        if (Wall.Stats[Stat.HP] == Wall.HP) return cost;

        if (Info != null && Info.RepairCost != 0)
        {
            cost = Info.RepairCost / (Wall.Stats[Stat.HP] / (Wall.Stats[Stat.HP] - Wall.HP));
        }

        return cost;
    }

    public void Repair()
    {
        if (Wall == null)
        {
            Spawn(true);
            return;
        }

        if (Wall.Dead)
            Spawn(true);
        else
            Wall.HP = Wall.Stats[Stat.HP];

        Wall.CheckDirection();
    }
}