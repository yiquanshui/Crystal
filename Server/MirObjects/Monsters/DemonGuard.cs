using Server.MirDatabase;
using Server.MirEnv;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    public class DemonGuard : ZumaMonster
    {
        public uint RevivalCount;
        public int LifeCount;
        public long RevivalTime, DieTime;

        public override uint Experience
        {
            get { return Info.Experience * (100 - (25 * RevivalCount)) / 100; }
        }

        protected internal DemonGuard(MonsterInfo info)
            : base(info)
        {
            RevivalCount = 0;
            LifeCount = Env.Random.Next(3);
        }

        protected override void Attack()
        {
            if (!Target.IsAttackTarget(this))
            {
                Target = null;
                return;
            }

            ShockTime = 0;

            ActionTime = Env.Time + 300;
            AttackTime = Env.Time + AttackSpeed;

            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);

            if (Env.Random.Next(3) > 0)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });
                int damage = GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]);
                if (damage == 0) return;

                DelayedAction action = new DelayedAction(DelayedType.Damage, Env.Time + 300, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                int damage = GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]);
                if (damage == 0) return;

                DelayedAction action = new DelayedAction(DelayedType.Damage, Env.Time + 300, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);

            }

        }

        public override void Die()
        {
            DieTime = Env.Time;
            RevivalTime = (4 + Env.Random.Next(20)) * 1000;
            base.Die();
        }

        protected override void ProcessAI()
        {
            if (Dead && Env.Time > DieTime + RevivalTime && RevivalCount < LifeCount)
            {
                RevivalCount++;

                int newhp = (int)(Stats[Stat.HP] * (100 - (25 * RevivalCount)) / 100);
                Revive(newhp, false);
            }

            base.ProcessAI();
        }
    }
}
