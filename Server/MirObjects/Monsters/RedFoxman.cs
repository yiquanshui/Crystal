using System.Drawing;
﻿using Server.MirDatabase;
using Server.MirEnv;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    public class RedFoxman : MonsterObject
    {
        public long FearTime, TeleportTime;
        public byte AttackRange = 6;

        protected internal RedFoxman(MonsterInfo info)
            : base(info)
        {
        }

        protected override bool InAttackRange()
        {
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, AttackRange);
        }

        protected override void Attack()
        {
            if (!Target.IsAttackTarget(this))
            {
                Target = null;
                return;
            }

            ShockTime = 0;

            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);

            byte spelltype = Env.Random.Next(2) == 0 ? (byte)0 : (byte)1;
            Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, TargetID = Target.ObjectID, Type = spelltype });

            ActionTime = Env.Time + 300;
            AttackTime = Env.Time + AttackSpeed;

            int damage = GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]);
            if (damage == 0) return;

            DelayedAction action = new DelayedAction(DelayedType.RangeDamage, Env.Time + 500, Target, damage, DefenceType.MAC);
            ActionList.Add(action);
        }

        protected override void ProcessTarget()
        {
            if (Target == null || !CanAttack) return;

            if (InAttackRange() && (Env.Time < FearTime))
            {
                if (Functions.InRange(CurrentLocation, Target.CurrentLocation, 1) && Env.Time > TeleportTime && Env.Random.Next(1) == 0)
                {
                    TeleportTime = Env.Time + 10000;
                    TeleportRandom(40, 14);
                    return;
                }
                else
                {
                    Attack();
                    return;
                }
            }

            FearTime = Env.Time + 5000;

            if (Env.Time < ShockTime)
            {
                Target = null;
                return;
            }

            int dist = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);

            if (dist >= AttackRange)
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

                if (Teleport(CurrentMap, location, true, 2)) return true;
            }

            return false;
        }
    }
}
