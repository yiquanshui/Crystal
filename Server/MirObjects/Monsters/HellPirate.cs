using Server.MirDatabase;
using Server.MirEnv;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    public class HellPirate : MonsterObject
    {
        protected internal HellPirate(MonsterInfo info)
            : base(info)
        {
        }

        protected override void Attack()
        {
            if (!Target.IsAttackTarget(this))
            {
                Target = null;
                return;
            }
            
            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);

            if (RandomProvider.Next(3) > 0)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                base.Attack();
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });

                int damage = GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]);
                if (damage == 0) return;

                FullmoonAttack(damage);
            }

            ShockTime = 0;
            ActionTime = Env.Time + 300;
            AttackTime = Env.Time + AttackSpeed;
        }   
    }
}
