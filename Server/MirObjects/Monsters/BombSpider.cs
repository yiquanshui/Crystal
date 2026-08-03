using Server.MirDatabase;
using Server.MirEnvir;

namespace Server.MirObjects.Monsters
{
    public class BombSpider : MonsterObject
    {
        public long ExplosionTime;

        protected internal BombSpider(MonsterInfo info) 
            : base(info)
        {
            ExplosionTime = Env.Time + 1000 * 60 * 5;
        }

        protected override void ProcessTarget()
        {
            if (Target == null) { Die(); return; }
            if (InAttackRange()) { Die(); return; }
            if (Env.Time > ExplosionTime) { Die(); return; }

            if (Env.Time < ShockTime)
            {
                Target = null;
                return;
            }

            MoveTo(Target.CurrentLocation);
        }

        public override void Die()
        {
            ActionList.Add(new DelayedAction(DelayedType.Die, Env.Time + 500));
            base.Die();
        }

        protected override void CompleteDeath(IList<object> data)
        {
            List<MapObject> targets = FindAllTargets(1, CurrentLocation, false);
            if (targets.Count == 0) return;

            for (int i = 0; i < targets.Count; i++)
            {
                int damage = GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]);
                if (damage == 0) return;

                if (targets[i].Attacked(this, damage, DefenceType.ACAgility) <= 0) continue;

                if (Env.Random.Next(Settings.PoisonResistWeight) >= targets[i].Stats[Stat.PoisonResist])
                {
                    if (Env.Random.Next(5) == 0)
                    {
                        targets[i].ApplyPoison(new Poison { Owner = this, Duration = 5, PType = PoisonType.Green, Value = GetAttackPower(Stats[Stat.MinSC], Stats[Stat.MaxSC]), TickSpeed = 2000 }, this);
                    }
                }
            }
        }
    }
}
