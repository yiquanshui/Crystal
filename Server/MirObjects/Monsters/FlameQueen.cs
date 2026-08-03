using Server.MirDatabase;
using Server.MirEnv;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    public class FlameQueen : MonsterObject
    {
        public long FearTime;
        public byte AttackRange = 3;
        private long MassAttackTime;

        protected internal FlameQueen(MonsterInfo info)
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

            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);

            if ((HP * 100 / Stats[Stat.HP]) < 20 && MassAttackTime < Env.Time)
            {
                ShockTime = 0;
                ActionTime = Env.Time + 500;
                AttackTime = Env.Time + (AttackSpeed);

                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });

                List<MapObject> targets = FindAllTargets(7, CurrentLocation, false);

                if (targets.Count == 0) return;

                int damage = GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]);

                for (int i = 0; i < targets.Count; i++)
                {
                    int delay = Functions.MaxDistance(CurrentLocation, targets[i].CurrentLocation) * 50 + 750; //50 MS per Step

                    DelayedAction action = new DelayedAction(DelayedType.RangeDamage, Env.Time + delay, Target, damage, DefenceType.ACAgility);
                    ActionList.Add(action);
                }

                MassAttackTime = Env.Time + 2000 + (Env.Random.Next(5) * 1000);
                ActionTime = Env.Time + 800;
                AttackTime = Env.Time + (AttackSpeed);
                return;
            }

            if (!Functions.InRange(CurrentLocation, Target.CurrentLocation, 1) || Env.Random.Next(3) == 0)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });

                int damage = GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]);

                DelayedAction action = new DelayedAction(DelayedType.Damage, Env.Time + 300, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });

                int damage = GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]);

                DelayedAction action = new DelayedAction(DelayedType.RangeDamage, Env.Time + 300, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }

            ShockTime = 0;
            ActionTime = Env.Time + 500;
            AttackTime = Env.Time + (AttackSpeed);
        }
       
    }
}
