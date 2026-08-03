using Server.MirEnv;
using Server.MirObjects;
using Server.MirObjects.Monsters;

namespace Server.MirDatabase.Conquest;

public class GuildArcherInfo
{
    protected static Env Env
    {
        get { return Env.Main; }
    }

    public int Index;
    public bool Alive;

    public ConquestArcherInfo Info;

    public ConquestObject Conquest;

    public ConquestArcher ArcherMonster;


    public GuildArcherInfo() { }

    public GuildArcherInfo(BinaryReader reader)
    {
        Index = reader.ReadInt32();
        Alive = reader.ReadBoolean();
    }

    public void Save(BinaryWriter writer)
    {
        if (ArcherMonster == null || ArcherMonster.Dead)
        {
            Alive = false;
        }
        else
        {
            Alive = true;
        }

        writer.Write(Index);
        writer.Write(Alive);
    }

    public void Spawn(bool Revive = false)
    {
        if (Revive) Alive = true;

        MonsterInfo monsterInfo = Env.GetMonsterInfo(Info.MobIndex);

        if (monsterInfo == null) return;
        if (monsterInfo.AI != 80) return;

        ArcherMonster = (ConquestArcher)MonsterObject.GetMonster(monsterInfo);

        if (ArcherMonster == null) return;

        ArcherMonster.Conquest = Conquest;
        ArcherMonster.ArcherIndex = Index;

        if (Alive)
        {
            ArcherMonster.Spawn(Conquest.ConquestMap, Info.Location);
        }
        else
        {
            ArcherMonster.Spawn(Conquest.ConquestMap, Info.Location);
            ArcherMonster.Die();
            ArcherMonster.DeadTime = Env.Time;
        }
    }

    public uint GetRepairCost()
    {
        uint cost = 0;

        if (ArcherMonster == null || ArcherMonster.Dead)
        {
            cost = Info.RepairCost;
        }

        return cost;
    }
}