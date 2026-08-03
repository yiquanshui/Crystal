using Server.MirDatabase;
using Server.MirEnv;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    public class CharmedSnake : MonsterObject
    {
        public bool Summoned;
        public long AliveTime;
        public MapObject MasterTotem;

        protected internal CharmedSnake(MonsterInfo info) : base(info)
        {
            ActionTime = Env.Time + 1000;
        }

        public override string Name
        {
            get { return Master == null ? Info.GameName : (Dead ? Info.GameName : string.Format("{0}({1})", Info.GameName, Master.Name)); }
            set { throw new NotSupportedException(); }
        }

        public override void Process()
        {
            if (!Dead && Summoned)
            {
                bool selfDestruct = false;
                if (Master != null)
                {
                    if (FindObject(Master.ObjectID, 15) == null) selfDestruct = true;
                    if (Summoned && Env.Time > AliveTime) selfDestruct = true;
                    if (selfDestruct && Master != null) Die();
                }
            }
            base.Process();
        }

        public override void Process(DelayedAction action)
        {
            switch (action.Type)
            {
                case DelayedType.Damage:
                    CompleteAttack(action.Params);
                    break;
            }
        }

        // protected override void ProcessAI()
        // {
        // ProcessSearch();
        // ProcessRoam();
        // ProcessTarget();
        //}

        protected override void ProcessAI()
        {
            if (Dead) return;

            ProcessSearch();
            //todo ProcessRoaming(); needs no master follow just target roaming
            ProcessTarget();
        }

        protected override void ProcessSearch()
        {
            if (Env.Time < SearchTime) return;
            SearchTime = Env.Time + SearchDelay;

            //Stacking or Infront of master - Move
            bool stacking = false;

            Cell cell = CurrentMap.GetCell(CurrentLocation);

            if (cell.Objects != null)
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    MapObject ob = cell.Objects[i];
                    if (ob == this || !ob.Blocking) continue;
                    stacking = true;
                    break;
                }

            if (CanMove && ((MasterTotem != null && MasterTotem.Front == CurrentLocation) || stacking))
            {
                //Walk Randomly
                if (!Walk(Direction))
                {
                    MirDirection dir = Direction;

                    switch (RandomProvider.Next(3)) // favour Clockwise
                    {
                        case 0:
                            for (int i = 0; i < 7; i++)
                            {
                                dir = Functions.NextDir(dir);

                                if (Walk(dir))
                                    break;
                            }
                            break;
                        default:
                            for (int i = 0; i < 7; i++)
                            {
                                dir = Functions.PreviousDir(dir);

                                if (Walk(dir))
                                    break;
                            }
                            break;
                    }
                }
            }

            if (Target == null || RandomProvider.Next(3) == 0) FindTarget();
        }

        protected override void ProcessRoam()
        {
            if (Target != null || Env.Time < RoamTime) return;

            if (ProcessRoute()) return;

            if (MasterTotem != null)
            {
                MoveTo(MasterTotem.Back);
                return;
            }

            RoamTime = Env.Time + RoamDelay;
            if (RandomProvider.Next(10) != 0) return;

            switch (RandomProvider.Next(3)) //Face Walk
            {
                case 0:
                    Turn((MirDirection)RandomProvider.Next(8));
                    break;
                default:
                    Walk(Direction);
                    break;
            }
        }

        protected override void ProcessTarget()
        {
            if (Target == null || !CanAttack) return;

            if (InAttackRange())
            {
                Attack();
                if (Target.Dead)
                    FindTarget();

                return;
            }

            if (Env.Time < ShockTime)
            {
                Target = null;
                return;
            }

            MoveTo(Target.CurrentLocation);
        }

        protected override void Attack()
        {
            if (!Target.IsAttackTarget(this))
            {
                Target = null;
                return;
            }

            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
            Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });

            int damage = GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]);
            if (damage > 0)
            {
                DelayedAction action = new DelayedAction(DelayedType.Damage, Env.Time + 300, Target, damage, DefenceType.MAC);
                ActionList.Add(action);
            }

            ActionTime = Env.Time + 300;
            AttackTime = Env.Time + AttackSpeed;
            ShockTime = 0;
        }

        protected override void CompleteAttack(IList<object> data)
        {
            MapObject target = (MapObject)data[0];
            int damage = (int)data[1];
            DefenceType defence = (DefenceType)data[2];

            if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;

            if (target.Attacked(this, damage, defence) <= 0) return;

            PoisonTarget(target, 10 - PetLevel, 4 + PetLevel, PoisonType.Paralysis, 1000);
        }

        public override void Die()
        {
            base.Die();

            //Explosion
            for (int y = CurrentLocation.Y - 1; y <= CurrentLocation.Y + 1; y++)
            {
                if (y < 0) continue;
                if (y >= CurrentMap.Height) break;

                for (int x = CurrentLocation.X - 1; x <= CurrentLocation.X + 1; x++)
                {
                    if (x < 0) continue;
                    if (x >= CurrentMap.Width) break;

                    Cell cell = CurrentMap.GetCell(x, y);

                    if (!cell.Valid || cell.Objects == null) continue;

                    for (int i = 0; i < cell.Objects.Count; i++)
                    {
                        MapObject target = cell.Objects[i];
                        switch (target.Race)
                        {
                            case ObjectType.Monster:
                            case ObjectType.Player:
                                //Only targets
                                if (!target.IsAttackTarget(this) || target.Dead) break;
                                target.Attacked(this, 10 * PetLevel, DefenceType.MACAgility);
                                break;
                        }
                    }
                }
            }
        }

        public override void Spawned()
        {
            base.Spawned();
            Summoned = true;
        }

        public override Packet GetInfo()
        {
            var packet = (S.ObjectMonster)base.GetInfo();
            packet.Image = Monster.CharmedSnake;
            packet.Extra = Summoned;
            return packet;
        }
    }
}
