using Server.MirDatabase;
using Server.MirEnvir;

namespace Server.MirObjects.Monsters
{
    public class Jar1 : MonsterObject
    {
        protected virtual byte AttackRange
        {
            get
            {
                return 1;
            }
        }

        protected override bool CanMove { get { return false; } }
        protected override bool CanRegen { get { return false; } }

        protected override bool InAttackRange()
        {
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, AttackRange);
        }

        protected internal Jar1(MonsterInfo info)
            : base(info)
        {

        }

        public override void Die()
        {
            ActionList.Add(new DelayedAction(DelayedType.Die, Env.Time + 1000));

            base.Die();
        }

        protected override void CompleteDeath(IList<object> data)
        {
            SpawnSlave();
        }

        private void SpawnSlave()
        {
            ActionTime = Env.Time + 300;
            AttackTime = Env.Time + AttackSpeed;

            List<int> conquestAIs = new()
            {
                72, // Siege Gate
                73, // Gate West
                80, // Archer
                81, // Gate
                82  // Wall
            };

            var validMonsters = Env.MonsterInfoList
                .Where(x => x.Level <= Level && x.Level >= (Level - 10) && !x.IsBoss && !conquestAIs.Contains(x.AI))
                .ToList();

            if (validMonsters.Count > 0)
            {
                var idx = Env.Random.Next(validMonsters.Count);
                var monster = validMonsters[idx];

                var mob = GetMonster(monster);
                if (mob == null) return;

                mob.Spawn(CurrentMap, CurrentLocation);
                mob.Target = Target;
                mob.ActionTime = Env.Time + 2000;
            }
        }
    }
}