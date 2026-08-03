using Server.MirDatabase;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    public class AssassinBird : MonsterObject
    {
        protected internal AssassinBird(MonsterInfo info)
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

            ShockTime = 0;

            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);

            ActionTime = Env.Time + 300;
            AttackTime = Env.Time + AttackSpeed;

            if (MirEnv.RandomProvider.Next(9) > 0)
            {
                if (MirEnv.RandomProvider.Next(4) > 0)
                {
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                    int damage = GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]);
                    if (damage == 0) return;

                    DelayedAction action = new DelayedAction(DelayedType.Damage, Env.Time + 400, Target, damage, DefenceType.ACAgility);
                    ActionList.Add(action);
                }
                else
                {
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                    int damage = GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]);
                    if (damage == 0) return;

                    DelayedAction action = new DelayedAction(DelayedType.Damage, Env.Time + 700, Target, damage, DefenceType.ACAgility);
                    ActionList.Add(action);
                }
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });

                int damage = GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]);

                SinglePushAttack(damage, delay: 500);
            }
        }
    }
}
