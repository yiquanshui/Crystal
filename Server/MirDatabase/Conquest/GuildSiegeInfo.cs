using Server.MirDatabase;
using Server.MirEnv;
using Server.MirObjects;
using Server.MirObjects.Monsters;

namespace Server.Library.MirDatabase.Conquest;

public class GuildSiegeInfo
{
    protected static Env Env
    {
        get { return Env.Main; }
    }

    public int Index;
    public int Health;

    public ConquestSiegeInfo Info;
    public ConquestObject Conquest;
    public Gate Gate;

    public GuildSiegeInfo() { }

    public GuildSiegeInfo(BinaryReader reader)
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
        //if (Gate != null) Health = Gate.HP; - needs adding
        writer.Write(Index);
        writer.Write(Health);
    }


    public void Spawn()
    {
        if (Gate != null) Gate.Despawn();

        MonsterInfo monsterInfo = Env.GetMonsterInfo(Info.MobIndex);

        if (monsterInfo == null) return;
        if (monsterInfo.AI != 72) return;

        if (monsterInfo.AI == 72)
        {
            Gate = (Gate)MonsterObject.GetMonster(monsterInfo);
        }
        else if (monsterInfo.AI == 73)
        {
            //Gate = (GateWest)MonsterObject.GetMonster(monsterInfo);
        }

        if (Gate == null) return;

        Gate.Conquest = Conquest;
        Gate.GateIndex = Index;

        Gate.Spawn(Conquest.ConquestMap, Info.Location);

        if (Health == 0)
        {
            Gate.Die();
        }
        else
        {
            Gate.SetHP(Health);
        }

        Gate.CheckDirection();
    }

    public int GetRepairCost()
    {
        int cost = 0;

        if (Gate == null) return 0;

        if (Gate.Stats[Stat.HP] == Gate.HP) return cost;

        if (Info.RepairCost != 0)
        {
            cost = Info.RepairCost / (Gate.Stats[Stat.HP] / (Gate.Stats[Stat.HP] - Gate.HP));
        }

        return cost;
    }

    public void Repair()
    {
        if (Gate == null)
        {
            Spawn();
            return;
        }

        if (Gate.Dead)
        {
            Spawn();
        }
        else
        {
            Gate.HP = Gate.Stats[Stat.HP];
        }

        Gate.CheckDirection();
    }
}
