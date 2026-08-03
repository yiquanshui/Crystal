using Server.MirDatabase;
using Server.MirEnv;
using Server.MirObjects;
using Server.MirObjects.Monsters;

namespace Server.Library.MirDatabase.Conquest;

public class GuildArcherInfo
{
    protected static Env Env => Env.Main;

    public int Index;
    public bool Alive;

    public ArcherInfo? Info;

    public ConquestObject? Conquest;

    public ConquestArcher? ArcherMonster;


    public GuildArcherInfo() { }

    public GuildArcherInfo(BinaryReader reader)
    {
        Index = reader.ReadInt32();
        Alive = reader.ReadBoolean();
    }


    public ArcherInfo? ArcherInfo
    {
        get => Info;
        set => Info = value;
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

        if (ArcherInfo == null || Conquest == null)
        {
            return;
        }

        MonsterInfo? monsterInfo = Env.GetMonsterInfo(ArcherInfo.MobIndex);

        if (monsterInfo == null) return;
        if (monsterInfo.AI != 80) return;

        ArcherMonster = (ConquestArcher)MonsterObject.GetMonster(monsterInfo);

        if (ArcherMonster == null) return;

        ArcherMonster.Conquest = Conquest;
        ArcherMonster.ArcherIndex = Index;

        if (Alive)
        {
            ArcherMonster.Spawn(Conquest.ConquestMap, ArcherInfo.Location);
        }
        else
        {
            ArcherMonster.Spawn(Conquest.ConquestMap, ArcherInfo.Location);
            ArcherMonster.Die();
            ArcherMonster.DeadTime = Env.Time;
        }
    }

    public uint GetRepairCost()
    {
        uint cost = 0;

        if (ArcherMonster == null || ArcherMonster.Dead)
        {
            cost = ArcherInfo!.RepairCost;
        }

        return cost;
    }
}