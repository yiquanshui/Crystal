using System.Drawing;
﻿using Server.MirDatabase;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    public class DarkCaptain : MonsterObject
    {
        protected long _ThunderTime;
        protected long _MassThunderTime;
        protected long _OrbTime;

        protected internal DarkCaptain(MonsterInfo info)
            : base(info)
        {
            _ThunderTime = Env.Time + 10000;
            _MassThunderTime = Env.Time + 20000;
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

            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);

            if (_ThunderTime < Env.Time)
            {
                AttackTime = Env.Time + AttackSpeed;

                _ThunderTime = Env.Time + 10000 + Env.Random.Next(0, 10000);

                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });

                int damage = GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]);
                if (damage == 0) return;

                DelayedAction action = new DelayedAction(DelayedType.Damage, Env.Time + 300, Target, damage, DefenceType.MACAgility, 2);
                ActionList.Add(action);
                return;
            }

            if (_MassThunderTime < Env.Time)
            {
                AttackTime = Env.Time + AttackSpeed;

                _MassThunderTime = Env.Time + 20000 + Env.Random.Next(0, 30000);

                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });

                int damage = GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]);
                if (damage == 0) return;

                DelayedAction action = new DelayedAction(DelayedType.Damage, Env.Time + 300, Target, damage, DefenceType.MACAgility, 5);
                ActionList.Add(action);
                return;
            }

            if (_OrbTime < Env.Time)
            {
                AttackTime = Env.Time + AttackSpeed;

                _OrbTime = Env.Time + 30000 + Env.Random.Next(0, 10000);

                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });

                var attempts = 4;
                var distance = 4;

                for (int i = 0; i < attempts; i++)
                {
                    var location = new Point(CurrentLocation.X + Env.Random.Next(-distance, distance + 1),
                                         CurrentLocation.Y + Env.Random.Next(-distance, distance + 1));

                    if (PowerBead.SpawnRandom(this, location)) break;
                }           

                return;
            }

            if (Env.Random.Next(5) == 0)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });

                TeleportBehindWeakerTarget();
                return;
            }

            if (Env.Random.Next(5) > 0)
            {
                AttackTime = Env.Time + AttackSpeed;

                //Sword Attack
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });

                int damage = GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]);
                if (damage == 0) return;

                LineAttack(damage, 2, 300, DefenceType.ACAgility, Env.Random.Next(5) == 0);
            }
            else
            {
                AttackTime = Env.Time + AttackSpeed;

                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });

                //PushAttack
                int damage = GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]);
                if (damage == 0) return;

                FullmoonAttack(damage, 500, DefenceType.ACAgility, Env.Random.Next(1, 3));
            }
        }

        protected override void CompleteRangeAttack(IList<object> data)
        {
            MapObject target = (MapObject)data[0];
            int damage = (int)data[1];
            DefenceType defence = (DefenceType)data[2];
            int range = (int)data[3];

            if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;

            var targets = FindAllTargets(range, CurrentLocation, false);

            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].Attacked(this, damage, defence);
            }
        }

        private void TeleportBehindWeakerTarget()
        {
            List<MapObject> targets = FindAllTargets(Info.ViewRange, CurrentLocation);

            var newTarget = Target;

            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].Stats[Stat.MinDC] > Target.Stats[Stat.MinDC]) continue;

                newTarget = targets[i];
                break;
            }

            Target = newTarget;

            MirDirection reverse = Functions.ReverseDirection(Target.Direction);

            Point location = Functions.PointMove(Target.CurrentLocation, reverse, 1);

            Direction = Functions.DirectionFromPoint(location, Target.CurrentLocation);

            Teleport(CurrentMap, location, true, Info.Effect);
        }

        public override void Die()
        {
            base.Die();

            //Kill Minions
            for (int i = SlaveList.Count - 1; i >= 0; i--)
            {
                if (!SlaveList[i].Dead && SlaveList[i].Node != null)
                {
                    SlaveList[i].Die();
                }
            }
        }
    }
}
