using System.Drawing;
﻿using Server.MirDatabase;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    /// Attack1 - Green Poison attack
    /// Attack2 - Purple Poison attack
    /// AttackRange1 - Ice Tornado (big range)
    /// AttackRange2 - Blizzard (big range)
    /// </summary>

    public class FlyingStatue : MonsterObject
    {
        public long _BlizzardCooldown;

        protected internal FlyingStatue(MonsterInfo info)
            : base(info)
        {
        }

        protected override bool InAttackRange()
        {
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, Info.ViewRange);
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
            bool ranged = CurrentLocation == Target.CurrentLocation || !Functions.InRange(CurrentLocation, Target.CurrentLocation, 1);

            ActionTime = Env.Time + 600;
            AttackTime = Env.Time + AttackSpeed;

            if (!ranged)
            {
                if (MirEnv.RandomProvider.Next(6) != 0)
                {
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                    int damage = GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]);
                    if (damage == 0) return;

                    DelayedAction action = new DelayedAction(DelayedType.Damage, Env.Time + 1100, Target, damage, DefenceType.MACAgility);
                    ActionList.Add(action);
                }
                else
                {
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                    int damage = GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]);
                    if (damage == 0) return;

                    DelayedAction action = new DelayedAction(DelayedType.Damage, Env.Time + 1100, Target, damage, DefenceType.MACAgility);
                    ActionList.Add(action);
                }
            }
            else
            {
                SpawnIceTornado();
            }
        }

        private void SpawnIceTornado()
        {
            Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, TargetID = Target.ObjectID, Type = 0 });

            List<MapObject> targets = FindAllTargets(Info.ViewRange, CurrentLocation);

            var count = targets.Count;

            if (count == 0) return;

            var target = targets[MirEnv.RandomProvider.Next(count)];

            var location = target.CurrentLocation;

            for (int y = location.Y - 1; y <= location.Y + 1; y++)
            {
                if (y < 0) continue;
                if (y >= CurrentMap.Height) break;

                for (int x = location.X - 1; x <= location.X + 1; x++)
                {
                    if (x < 0) continue;
                    if (x >= CurrentMap.Width) break;

                    if (x == CurrentLocation.X && y == CurrentLocation.Y) continue;

                    var cell = CurrentMap.GetCell(x, y);

                    if (!cell.Valid) continue;

                    int damage = GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]);

                    var start = 500;

                    SpellObject ob = new SpellObject
                    {
                        Spell = Spell.FlyingStatueIceTornado,
                        Value = damage,
                        ExpireTime = Env.Time + 1500 + start,
                        TickSpeed = 3000,
                        CurrentLocation = new Point(x, y),
                        CastLocation = location,
                        Show = location.X == x && location.Y == y,
                        CurrentMap = CurrentMap,
                        Caster = this
                    };

                    DelayedAction action = new DelayedAction(DelayedType.Spawn, Env.Time + start, ob);
                    CurrentMap.ActionList.Add(action);
                }
            }
        }

        public override void Spawned()
        {
            base.Spawned();
        }

        protected override void ProcessTarget()
        {
            if (Target == null) return;

            if (InAttackRange() && CanAttack)
            {
                Attack();
                return;
            }

            if (Env.Time < ShockTime)
            {
                Target = null;
                return;
            }

            var hpPercent = (HP * 100) / Stats[Stat.HP];
            bool halfHealth = hpPercent <= 50;

            int dist = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);

            if (dist >= Info.ViewRange || !halfHealth)
                MoveTo(Target.CurrentLocation);
            else
            {
                MirDirection dir = Functions.DirectionFromPoint(Target.CurrentLocation, CurrentLocation);

                if (Walk(dir)) return;

                switch (MirEnv.RandomProvider.Next(2)) //No favour
                {
                    case 0:
                        for (int i = 0; i < 7; i++)
                        {
                            dir = Functions.NextDir(dir);

                            if (Walk(dir))
                                return;
                        }
                        break;
                    default:
                        for (int i = 0; i < 7; i++)
                        {
                            dir = Functions.PreviousDir(dir);

                            if (Walk(dir))
                                return;
                        }
                        break;
                }

            }
        }
    }
}
