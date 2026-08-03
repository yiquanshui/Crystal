using Server.MirDatabase;
using Server.MirEnvir;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    public class Shinsu : MonsterObject
    {
        public bool Mode = false;
        public bool Summoned;
        public long ModeTime;

        protected override bool CanAttack
        {
            get
            {
                return base.CanAttack && Mode;
            }
        }

        protected internal Shinsu(MonsterInfo info) : base(info)
        {
            ActionTime = Env.Time + 1000;
        }

        protected override void ProcessAI()
        {
            if (!Dead && Env.Time > ActionTime)
            {
                if (Target != null) ModeTime = Env.Time + 30000;

                if (!Mode && Env.Time < ModeTime)
                {
                    Mode = true;
                    Broadcast(new S.ObjectShow { ObjectID = ObjectID });
                    ActionTime = Env.Time + 1000;
                }
                else if (Mode && Env.Time > ModeTime)
                {
                    Mode = false;
                    Broadcast(new S.ObjectHide { ObjectID = ObjectID });
                    ActionTime = Env.Time + 1000;
                }
            }

            base.ProcessAI();
        }

        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;
            if (Target.CurrentLocation == CurrentLocation) return false;

            int x = Math.Abs(Target.CurrentLocation.X - CurrentLocation.X);
            int y = Math.Abs(Target.CurrentLocation.Y - CurrentLocation.Y);

            if (x > 2 || y > 2) return false;


            return (x <= 1 && y <= 1) || (x == y || x % 2 == y % 2);
        }

        protected override void Attack()
        {
            if (!Target.IsAttackTarget(this))
            {
                Target = null;
                return;
            }

            ActionTime = Env.Time + 300;
            AttackTime = Env.Time + AttackSpeed;
            ShockTime = 0;

            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
            Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });

            int damage = GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]);
            if (damage == 0) return;

            LineAttack(damage, 2);
        }

        public override void Spawned()
        {
            base.Spawned();

            Summoned = true;
        }

        public override Packet GetInfo()
        {
            var packet = (S.ObjectMonster)base.GetInfo();
            packet.Image = Mode ? Monster.Shinsu1 : Monster.Shinsu;
            packet.Extra = Summoned;
            return packet;
        }
    }
}
