using System.Drawing;
using Server.MirDatabase;
using Server.MirEnv;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    public class HornedMage : AxeSkeleton
    {
        protected internal HornedMage(MonsterInfo info)
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

            bool ranged = CurrentLocation != Target.CurrentLocation && !Functions.InRange(CurrentLocation, Target.CurrentLocation, 3);

            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);

            ShockTime = 0;

            if (!ranged)
            {
                int damage = GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]);
                if (damage == 0) return;

                ActionTime = Env.Time + 500;
                AttackTime = Env.Time + AttackSpeed;

                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Env.Time + 500, Target, damage, DefenceType.MACAgility);
                ActionList.Add(action);
            }
            else
            {
                if (RandomProvider.Next(5) > 0)
                {
                    int damage = GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]);
                    if (damage == 0) return;

                    ActionTime = Env.Time + 500;
                    AttackTime = Env.Time + AttackSpeed;

                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, TargetID = Target.ObjectID, Type = 0 });
                    DelayedAction action = new DelayedAction(DelayedType.RangeDamage, Env.Time + 800, Target, damage, DefenceType.AC);
                    ActionList.Add(action);
                }
                else
                {
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, TargetID = Target.ObjectID, Type = 1 });

                    TeleportTarget(4, 4);

                    Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
                }
            }
        }


        public bool TeleportTarget(int attempts, int distance)
        {
            for (int i = 0; i < attempts; i++)
            {
                Point location;

                if (distance <= 0)
                    location = new Point(RandomProvider.Next(CurrentMap.Width), RandomProvider.Next(CurrentMap.Height));
                else
                    location = new Point(CurrentLocation.X + RandomProvider.Next(-distance, distance + 1),
                                         CurrentLocation.Y + RandomProvider.Next(-distance, distance + 1));

                if (Target.Teleport(CurrentMap, location, true, Info.Effect)) return true;
            }

            return false;
        }

        protected override void CompleteRangeAttack(IList<object> data)
        {
            MapObject target = (MapObject)data[0];
            int damage = (int)data[1];
            DefenceType defence = (DefenceType)data[2];

            if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;

            target.Attacked(this, damage, defence);
        }

        protected override void CompleteAttack(IList<object> data)
        {
            MapObject target = (MapObject)data[0];
            int damage = (int)data[1];
            DefenceType defence = (DefenceType)data[2];

            if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;

            List<MapObject> targets = FindAllTargets(3, CurrentLocation);

            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].Attacked(this, damage, defence);
            }
            return;
        }
    }
}