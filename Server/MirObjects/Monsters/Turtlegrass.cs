using Server.MirDatabase;
using Server.MirEnv;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    public class TurtleGrass : ZumaMonster
    {
        protected internal TurtleGrass(MonsterInfo info)
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
            ShockTime = 0;

            if (!Target.IsAttackTarget(this))
            {
                Target = null;
                return;
            }

            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);

            ActionTime = Env.Time + 300;
            AttackTime = Env.Time + AttackSpeed;

            if (RandomProvider.Next(4) > 0)
            {
                base.Attack();
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });

                int damage = GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]);

                SinglePushAttack(damage, DefenceType.AC);
            }
        }   
    }
}

