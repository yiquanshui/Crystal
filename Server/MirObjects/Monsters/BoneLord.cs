using Server.MirDatabase;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    public class BoneLord : MonsterObject
    {
        public byte AttackRange = 7;
        public byte _stage = 3;

        protected internal BoneLord(MonsterInfo info)
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
            bool range = CurrentLocation == Target.CurrentLocation || !Functions.InRange(CurrentLocation, Target.CurrentLocation, 1);

            AttackTime = Env.Time + AttackSpeed;
            ActionTime = Env.Time + 300;

            if (range)
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, TargetID = Target.ObjectID, Type = 0 });

                int damage = GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]);
                if (damage == 0) return;

                int delay = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation) * 50 + 500; //50 MS per Step
                DelayedAction action = new DelayedAction(DelayedType.RangeDamage, Env.Time + delay, Target, damage, DefenceType.MACAgility);
                ActionList.Add(action);
            }
            else
            {
                base.Attack();
            }
        }

        protected override void ProcessTarget()
        {
            if (Target == null) return;

            if (Stats[Stat.HP] >= 3)
            {
                byte stage = (byte)(HP / (Stats[Stat.HP] / 3));

                if (stage < _stage)
                {
                    SpawnSlaves();
                    _stage = stage;
                    return;
                    }
            }

            if (InAttackRange() && CanAttack)
            {
                Attack();
                return;
            }

            if (Env.Time < ShockTime)
            {
                Target = null;
                return;
            }

            MoveTo(Target.CurrentLocation);
        }

        private void SpawnSlaves()
        {
            int count = Math.Min(8, 40 - SlaveList.Count);

            Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
            ActionTime = Env.Time + 300;
            AttackTime = Env.Time + AttackSpeed;

            for (int i = 0; i < count; i++)
            {
                MonsterObject mob = null;
                switch (Env.Random.Next(4))
                {
                    case 0:
                        mob = GetMonster(Env.GetMonsterInfo(Settings.BoneMonster1));
                        break;
                    case 1:
                        mob = GetMonster(Env.GetMonsterInfo(Settings.BoneMonster2));
                        break;
                    case 2:
                        mob = GetMonster(Env.GetMonsterInfo(Settings.BoneMonster3));
                        break;
                    case 3:
                        mob = GetMonster(Env.GetMonsterInfo(Settings.BoneMonster4));
                        break;
                }

                if (mob == null) continue;

                if (!mob.Spawn(CurrentMap, Front))
                    mob.Spawn(CurrentMap, CurrentLocation);

                mob.Target = Target;
                mob.ActionTime = Env.Time + 2000;
                SlaveList.Add(mob);
            }
        }
    }
}
