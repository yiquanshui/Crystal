using Server.MirDatabase;
using Server.MirEnvir;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    public class DarkDevil : MonsterObject
    {
        private long _areaTime;

        protected internal DarkDevil(MonsterInfo info) : base(info)
        {
        }

        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;
            if (Target.CurrentLocation == CurrentLocation) return false;

            return Functions.InRange(CurrentLocation, Target.CurrentLocation, Env.Time > _areaTime ? 3 : 1);
        }

        protected override void Attack()
        {
            if (!Target.IsAttackTarget(this))
            {
                Target = null;
                return;
            }

            if (Env.Time < _areaTime)
            {
                base.Attack();
                return;
            }

            _areaTime = Env.Time + 2000 + Env.Random.Next(3)*1000;

            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
            Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });
            ActionList.Add(new DelayedAction(DelayedType.RangeDamage, Env.Time + 500));

            ActionTime = Env.Time + 300;
            AttackTime = Env.Time + AttackSpeed;
            ShockTime = 0;
        }

        protected override void CompleteRangeAttack(IList<object> data)
        {
            int damage = GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]) * 3;
            if (damage == 0) return;

            List<MapObject> targets = FindAllTargets(1, Functions.PointMove(CurrentLocation, Direction, 2));
            if (targets.Count == 0) return;

            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].Attacked(this, damage, DefenceType.MACAgility);
            }
        }
    }
}
