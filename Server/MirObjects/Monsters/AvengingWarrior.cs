using Server.MirDatabase;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    public class AvengingWarrior : MonsterObject
    {
        public byte AttackRange = 6;

        protected internal AvengingWarrior(MonsterInfo info)
            : base(info)
        {
        }

        protected override bool InAttackRange()
        {
            return CurrentMap == Target.CurrentMap && CanFly(Target.CurrentLocation) && Functions.InRange(CurrentLocation, Target.CurrentLocation, AttackRange);
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
            bool ranged = CurrentLocation == Target.CurrentLocation || !Functions.InRange(CurrentLocation, Target.CurrentLocation, 1);

            ActionTime = Env.Time + 300;
            AttackTime = Env.Time + AttackSpeed;

            if (!ranged && MirEnv.RandomProvider.Next(5) > 0)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });
                int damage = GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]);
                
                DelayedAction action = new DelayedAction(DelayedType.Damage, Env.Time + 400, Target, damage, DefenceType.ACAgility, false);
                ActionList.Add(action);
            }
            else
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, TargetID = Target.ObjectID, Type = 0 });
                int damage = GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]);
                if (damage == 0) return;

                int delay = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation) * 50 + 500; //50 MS per Step
                DelayedAction action = new DelayedAction(DelayedType.RangeDamage, Env.Time + delay, Target, damage, DefenceType.MACAgility);
                ActionList.Add(action);
            }
        }

        protected override void CompleteRangeAttack(IList<object> data)
        {
            MapObject target = (MapObject)data[0];
            int damage = (int)data[1];
            DefenceType defence = (DefenceType)data[2];

            if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;

            if (target.Attacked(this, damage, defence) <= 0) return;

            PoisonTarget(target, 5, 8, PoisonType.Red);
        }

        protected override void ProcessTarget()
        {
            if (Target == null) return;

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
    }
}
