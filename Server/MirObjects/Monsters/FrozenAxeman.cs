using Server.MirDatabase;
using Server.MirEnv;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    public class FrozenAxeman : MonsterObject
    {
        public long PullTime;
        protected internal FrozenAxeman(MonsterInfo info)
            : base(info)
        {
        }

        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;
            if (Target.CurrentLocation == CurrentLocation) return false;

            int x = Math.Abs(Target.CurrentLocation.X - CurrentLocation.X);
            int y = Math.Abs(Target.CurrentLocation.Y - CurrentLocation.Y);

            if (x > 2 || y > 2) return false;


            return (x <= 1 && y <= 1) || (x == y || x % 2 == y % 2);
        }

        protected override void Attack()
        {

            if (!Target.IsAttackTarget(this))
            {
                Target = null;
                return;
            }

            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);

            int damage = GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]);
            if (damage == 0) return;

            bool range = !Functions.InRange(CurrentLocation, Target.CurrentLocation, 1);

            if (!range && RandomProvider.Next(3) > 0)
            {
                if (Env.Time >= PullTime)
                {
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });
                    PullTime = Env.Time + 10000;
                    AttackTime = Env.Time + AttackSpeed;
                    ActionTime = Env.Time + 300;

                    Target.Pushed(this, Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation), 2 + RandomProvider.Next(3));
                    DelayedAction action = new DelayedAction(DelayedType.Damage, Env.Time + 500, Target, damage, DefenceType.AC);
                    ActionList.Add(action);

                }
                else
                {
                    base.Attack();
                }
            }
            else
            {

                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Env.Time + 500, Target, damage * 2, DefenceType.AC);
                ActionList.Add(action);

            }


            ActionTime = Env.Time + 300;
            AttackTime = Env.Time + AttackSpeed;
            ShockTime = 0;


        }


    }
}