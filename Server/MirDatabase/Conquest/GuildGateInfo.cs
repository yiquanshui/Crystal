using Server.MirDatabase;
using Server.MirEnv;
using Server.MirObjects;
using Server.MirObjects.Monsters;

namespace Server.Library.MirDatabase.Conquest;

public class GuildGateInfo
{
    protected static Env Env => Env.Main;

    public int Index;
    public int Health;

    public GateInfo? Info;
    public ConquestObject? Conquest;
    public Gate? Gate;


    public GuildGateInfo() { }

    public GuildGateInfo(BinaryReader reader)
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
        if (Gate != null) Health = Gate.HP;
        writer.Write(Index);
        writer.Write(Health);
    }

    public void Spawn(bool repair)
    {
        Gate?.Despawn();
        if (Info == null) return;

        MonsterInfo? monsterInfo = Env.GetMonsterInfo(Info.MobIndex);

        if (monsterInfo == null) return;
        if (monsterInfo.AI != 81) return;

        Gate = (Gate)MonsterObject.GetMonster(monsterInfo);

        if (Gate == null) return;

        Gate.Conquest = Conquest;
        Gate.GateIndex = Index;

        if (Conquest != null)
        {
            Gate.Spawn(Conquest.ConquestMap, Info.Location);
        }

        if (repair) Health = Gate.Stats[Stat.HP];

        if (Health == 0)
            Gate.Die();
        else
            Gate.SetHP(Health);

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
            Spawn(true);
            return;
        }

        if (Gate.Dead)
        {
            Spawn(true);
        }
        else
        {
            Gate.HP = Gate.Stats[Stat.HP];
        }

        Gate.CheckDirection();
    }
}