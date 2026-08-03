using System.Drawing;
﻿using Server.MirDatabase;
using Server.MirEnv;

namespace Server.MirObjects.Monsters
{
    public class Doe : MonsterObject
    {
        public long FearTime;
        public long teleportTime = 5000;

        protected internal Doe(MonsterInfo info)
            : base(info)
        {
        }

        protected override void ProcessTarget()
        {
            if (Target == null || !CanAttack) return;

            if (Env.Time < FearTime)
            {
                Attack();
                return;
            }

            FearTime = Env.Time + 5000;


            var hpPercent = (HP * 100) / Stats[Stat.HP];
            bool halfHealth = hpPercent <= 50;

            if(halfHealth == true && Env.Time > teleportTime)
            {
                TeleportRandom(1, 5, CurrentMap);
            }

            if (Env.Time < ShockTime)
            {
                Target = null;
                return;
            }

            int dist = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);

            if (dist >= Info.ViewRange)
                MoveTo(Target.CurrentLocation);
            else
            {
                MirDirection dir = Functions.DirectionFromPoint(Target.CurrentLocation, CurrentLocation);

                if (Walk(dir)) return;

                switch (Env.Random.Next(2)) //No favour
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
        }


        public override bool TeleportRandom(int attempts, int distance, Map temp = null)
        {
            for (int i = 0; i < attempts; i++)
            {
                Point location;

                if (distance <= 0)
                    location = new Point(Env.Random.Next(CurrentMap.Width), Env.Random.Next(CurrentMap.Height));
                else
                    location = new Point(CurrentLocation.X + Env.Random.Next(-distance, distance + 1),
                                         CurrentLocation.Y + Env.Random.Next(-distance, distance + 1));

                if (Teleport(CurrentMap, location, true, 9)) return true;
            }

            return false;
        }

    }
}
