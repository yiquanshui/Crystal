using System.Drawing;
﻿using Server.MirDatabase;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    public class DigOutZombie : MonsterObject
    {
        public bool Visible, DoneDigOut;
        public long VisibleTime, DigOutTime;
        public Point DigOutLocation;
        public MirDirection DigOutDirection;

        protected override bool CanAttack
        {
            get
            {
                return Visible && base.CanAttack;
            }
        }
        protected override bool CanMove
        {
            get
            {
                return Visible && base.CanMove;
            }
        }
        public override bool Blocking
        {
            get
            {
                return Visible && base.Blocking;
            }
        }

        protected internal DigOutZombie(MonsterInfo info)
            : base(info)
        {
            Visible = false;
        }

        protected override void ProcessAI()
        {
            if (!Dead && Env.Time > VisibleTime)
            {
                VisibleTime = Env.Time + 2000;

                bool visible = FindNearby(3);

                if (!Visible && visible)
                {
                    Visible = true;
                    CellTime = Env.Time + 500;
                    Broadcast(GetInfo());
                    Broadcast(new S.ObjectShow { ObjectID = ObjectID });
                    ActionTime = Env.Time + 2000;
                    DigOutTime = Env.Time;
                    DigOutLocation = CurrentLocation;
                    DigOutDirection = Direction;
                }
            }

            SpawnDigOutEffect();         

            base.ProcessAI();
        }

        protected virtual void SpawnDigOutEffect()
        {
            if (Visible && Env.Time > DigOutTime + 1000 && !DoneDigOut)
            {
                SpellObject ob = new SpellObject
                {
                    Spell = Spell.DigOutZombie,
                    Value = 1,
                    ExpireTime = Env.Time + (5 * 60 * 1000),
                    TickSpeed = 2000,
                    Caster = null,
                    CurrentLocation = DigOutLocation,
                    CurrentMap = this.CurrentMap,
                    Direction = DigOutDirection
                };
                CurrentMap.AddObject(ob);
                ob.Spawned();
                DoneDigOut = true;
            }
        }

        public override bool Walk(MirDirection dir)
        {
            return Visible && base.Walk(dir);
        }

        public override bool IsAttackTarget(MonsterObject attacker)
        {
            return Visible && base.IsAttackTarget(attacker);
        }
        public override bool IsAttackTarget(HumanObject attacker)
        {
            return Visible && base.IsAttackTarget(attacker);
        }

        protected override void ProcessSearch()
        {
            if (Visible)
            {
                base.ProcessSearch();
            }
        }

        public override Packet GetInfo()
        {
            return !Visible ? null : base.GetInfo();
        }
    }
}