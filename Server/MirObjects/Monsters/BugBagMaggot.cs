using Server.MirDatabase;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    public class BugBagMaggot : MonsterObject
    {
        protected override bool CanMove { get { return false; } }

        protected internal BugBagMaggot(MonsterInfo info) : base(info)
        {
            Direction = MirDirection.Up;
        }

        public override void Turn(MirDirection dir)
        {
        }
        public override bool Walk(MirDirection dir) { return false; }

        protected override bool InAttackRange()
        {
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, Globals.DataRange);
        }

        protected override void Attack()
        {
            ShockTime = 0;

            if (!Target.IsAttackTarget(this))
            {
                Target = null;
                return;
            }

            if (SlaveList.Count >= 20) return;       
            
            MonsterObject spawn = GetMonster(Env.GetMonsterInfo(Settings.BugBatName));

            if (spawn == null) return;

            Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });

            ActionTime = Env.Time + 300;
            AttackTime = Env.Time + 3000;

            spawn.Target = Target;
            spawn.ActionTime = Env.Time + 1000;
            CurrentMap.ActionList.Add(new DelayedAction(DelayedType.Spawn, Env.Time + 500, spawn, CurrentLocation, this));
        }

        protected override void ProcessRoam() { }
    }
}
