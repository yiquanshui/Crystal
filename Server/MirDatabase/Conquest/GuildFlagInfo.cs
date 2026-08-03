using Server.MirDatabase;
using Server.MirObjects;

namespace Server.Library.MirDatabase.Conquest;

public class GuildFlagInfo
{
    public int Index;

    public ConquestFlagInfo Info;

    public ConquestObject Conquest;
    public GuildObject Guild;

    public NPCObject Flag;

    public GuildFlagInfo() { }

    public void Spawn()
    {
        NPCInfo npcInfo = new NPCInfo
        {
            Name = Info.Name,
            FileName = Info.FileName,
            Location = Info.Location,
            Image = 1000
        };

        if (Conquest.Guild != null)
        {
            Guild = Conquest.Guild;
            npcInfo.Image = Guild.Info.FlagImage;
            npcInfo.Colour = Guild.Info.FlagColour;
        }

        Flag = new NPCObject(npcInfo)
        {
            CurrentMap = Conquest.ConquestMap
        };

        Flag.CurrentMap.AddObject(Flag);

        Flag.Spawned();
    }

    public void ChangeOwner(GuildObject guild)
    {
        Guild = guild;

        UpdateImage();
        UpdateColour();
    }

    public void UpdateImage()
    {
        if (Guild != null)
        {
            Flag.Info.Image = Guild.Info.FlagImage;

            Flag.Broadcast(Flag.GetUpdateInfo());
        }
    }

    public void UpdateColour()
    {
        if (Guild != null)
        {
            Flag.Info.Colour = Guild.Info.FlagColour;

            Flag.Broadcast(Flag.GetUpdateInfo());
        }
    }
}

