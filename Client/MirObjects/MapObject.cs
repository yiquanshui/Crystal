using Client.MirControls;
using Client.MirGraphics;
using Client.MirScenes;
using Client.MirScenes.Dialogs;
using Client.MirSounds;
using SlimDX;

namespace Client.MirObjects
{
    public abstract class MapObject
    {
        private static readonly Font ChatFont = new Font(Settings.FontName, 10F);
        protected static List<MirLabel> LabelList { get;} = [];

        public static UserObject User { get; set; } = null!;
        public static UserHeroObject? Hero { get; set; }
        public static HeroObject? HeroObject { get; set; }
        public static MapObject? MouseObject { get; private set; }
        public static MapObject? TargetObject { get; private set; }
        public static MapObject? MagicObject { get; private set; }

        private static uint mouseObjectID;
        public static uint MouseObjectID
        {
            get => mouseObjectID;
            set {
                if (mouseObjectID == value) return;
                
                mouseObjectID = value;
                MouseObject = MapControl.Objects.GetValueOrDefault(value);
            }
        }

        private static uint lastTargetObjectId;
        
        private static uint targetObjectID;
        public static uint TargetObjectID {
            get => targetObjectID;
            set {
                if (targetObjectID == value) return;
                lastTargetObjectId = value;
                targetObjectID = value;
                TargetObject = MapControl.Objects.GetValueOrDefault(value);
            }
        }

        private static uint magicObjectID;
        public static uint MagicObjectID {
            get => magicObjectID;
            set {
                if (magicObjectID == value) return;
                magicObjectID = value;
                MagicObject = MapControl.Objects.GetValueOrDefault(value);
            }
        }

        public abstract ObjectType Race { get; }
        public abstract bool Blocking { get; }

        public uint ObjectID;
        public string Name = string.Empty;
        public Point CurrentLocation { get; set; }
        public Point MapLocation { get; set; }
        public MirDirection Direction { get; set; }
        public bool Dead { get; set; }
        public bool Hidden { get; set; }
        public bool SitDown { get; set; }
        public bool Sneaking { get; set; }
        public PoisonType Poison { get; set; }
        public long DeadTime { get; set; }
        public byte AI { get; set; }
        public bool InTrapRock { get; set; }
        public int JumpDistance { get; set; }

        public bool Blend { get; set; } = true;
        public long BlindTime { get; set; }
        public byte BlindCount { get; set; }

        public byte PercentHealth { get; set; }

        public long HealthTime { get; set; }

        public byte PercentMana { get; set; }

        public static uint LastTargetObjectId => lastTargetObjectId;

        public List<QueuedAction> ActionFeed { get; set; } = [];
        public QueuedAction? NextAction => ActionFeed.Count > 0 ? ActionFeed[0] : null;

        public List<Effect> Effects { get; set; } = [];
        public List<BuffType> Buffs { get; set; } = [];

        public MLibrary? BodyLibrary { get; set; }
        public Color DrawColour { get; set; } = Color.White;
        public Color NameColour { get; set; } = Color.White;
        public Color LightColour { get; } = Color.White;
        public MirLabel? NameLabel { get; set; }
        public MirLabel? ChatLabel { get; set; }
        public MirLabel GuildLabel { get; set; }
        public long ChatTime { get; set; }
        public int DrawFrame { get; set; }
        public int DrawWingFrame { get; set; }
        public Point DrawLocation, Movement, FinalDrawLocation, OffSetMove;
        public Rectangle DisplayRectangle;
        public int Light { get; set; }
        public int DrawY { get; set; }
        public long NextMotion { get; set; }
        public long NextMotion2 { get; set; }
        public MirAction CurrentAction { get; set; }
        public byte CurrentActionLevel { get; set; }
        public bool SkipFrames { get; set; }
        public FrameLoop? Loop { get; set; }

        //Sound
        public int StruckWeapon { get; set; }

        public MirLabel TempLabel { get; set; }

        public static List<MirLabel> DamageLabelList { get; } = [];
        public List<Damage> Damages { get; } = [];

        protected Point GlobalDisplayLocationOffset => new(0, 0);

        protected MapObject() { }

        protected MapObject(uint objectID) {
            ObjectID = objectID;
            if (MapControl.Objects.TryGetValue(ObjectID, out var existingObject))
                existingObject.Remove();

            MapControl.Objects[ObjectID] = this;
            MapControl.ObjectsList.Add(this);
            RestoreTargetStates();
        }

        
        public void Remove() {
            if (MouseObject == this) MouseObjectID = 0;
            
            if (TargetObject == this) {
                TargetObjectID = 0;
                lastTargetObjectId = ObjectID;
            }
            
            if (MagicObject == this) MagicObjectID = 0;

            if (this == User.NextMagicObject)
                User.ClearMagic();

            MapControl.Objects.Remove(ObjectID);
            MapControl.ObjectsList.Remove(this);
            GameScene.Scene.MapControl.RemoveObject(this);

            if (ObjectID == Hero?.ObjectID)
                HeroObject = null;

            if (ObjectID != GameScene.NPCID) return;

            GameScene.NPCID = 0;
            GameScene.Scene.NPCDialog.Hide();
        }

        public abstract void Process();
        public abstract void Draw();
        public abstract bool MouseOver(Point p);

        private void RestoreTargetStates() {
            if (MouseObjectID == ObjectID)
                MouseObject = this;

            if (TargetObjectID == ObjectID)
                TargetObject = this;

            if (MagicObjectID == ObjectID)
                MagicObject = this;

            if (!Dead && TargetObject == null && LastTargetObjectId == ObjectID) {
                if (Race is ObjectType.Player or ObjectType.Monster or ObjectType.Hero) {
                    targetObjectID = ObjectID;
                    TargetObject = this;
                }
            }
        }

        
        public void AddBuffEffect(BuffType type) {
            foreach (Effect effect in Effects) {
                if (effect is BuffEffect buff && buff.BuffType == type) return;
            }

            PlayerObject? ob = null;

            if (Race == ObjectType.Player) {
                ob = (PlayerObject)this;
            }

            switch (type)
            {
                case BuffType.Fury:
                    Effects.Add(new BuffEffect(Libraries.Magic3, 190, 7, 1400, this, true, type) { Repeat = true });
                    break;
                case BuffType.ImmortalSkin:
                    Effects.Add(new BuffEffect(Libraries.Magic3, 570, 5, 1400, this, true, type) { Repeat = true });
                    break;
                case BuffType.SwiftFeet:
                    if (ob != null) ob.Sprint = true;
                    break;
                case BuffType.MoonLight:
                case BuffType.DarkBody:
                    if (ob != null) ob.Sneaking = true;
                    break;
                case BuffType.VampireShot:
                    Effects.Add(new BuffEffect(Libraries.Magic3, 2110, 6, 1400, this, true, type) { Repeat = false });
                    break;
                case BuffType.PoisonShot:
                    Effects.Add(new BuffEffect(Libraries.Magic3, 2310, 7, 1400, this, true, type) { Repeat = false });
                    break;
                case BuffType.EnergyShield:
                    BuffEffect effect;

                    Effects.Add(effect = new BuffEffect(Libraries.Magic2, 1880, 9, 900, this, true, type) { Repeat = false });
                    SoundManager.PlaySound(20000 + (ushort)Spell.EnergyShield * 10 + 0);

                    effect.Complete += (o, e) =>
                    {
                        Effects.Add(new BuffEffect(Libraries.Magic2, 1900, 2, 800, this, true, type) { Repeat = true });
                    };
                    break;
                case BuffType.MagicBooster:
                    Effects.Add(new BuffEffect(Libraries.Magic3, 90, 6, 1200, this, true, type) { Repeat = true });
                    break;
                case BuffType.PetEnhancer:
                    Effects.Add(new BuffEffect(Libraries.Magic3, 230, 6, 1200, this, true, type) { Repeat = true });
                    break;
                case BuffType.GameMaster:
                    Effects.Add(new BuffEffect(Libraries.CHumEffect[5], 0, 1, 1200, this, true, type) { Repeat = true });
                    break;
                case BuffType.GeneralMeowMeowShield:
                    Effects.Add(new BuffEffect(Libraries.Monsters[(ushort)Monster.GeneralMeowMeow], 529, 7, 700, this, true, type) { Repeat = true, Light = 1 });
                    SoundManager.PlaySound(8322);
                    break;
                case BuffType.PowerBeadBuff:
                    Effects.Add(new BuffEffect(Libraries.Monsters[(ushort)Monster.PowerUpBead], 64, 6, 600, this, true, type) { Blend = true, Repeat = true });
                    break;
                case BuffType.HornedArcherBuff:
                    Effects.Add(effect = new BuffEffect(Libraries.Monsters[(ushort)Monster.HornedArcher], 468, 6, 600, this, true, type) { Repeat = false });
                    effect.Complete += (o, e) =>
                    {
                        Effects.Add(new BuffEffect(Libraries.Monsters[(ushort)Monster.HornedArcher], 474, 3, 1000, this, true, type) { Blend = true, Repeat = true });
                    };
                    break;
                case BuffType.ColdArcherBuff:
                    Effects.Add(effect = new BuffEffect(Libraries.Monsters[(ushort)Monster.HornedArcher], 477, 7, 700, this, true, type) { Repeat = false });
                    effect.Complete += (o, e) =>
                    {
                        Effects.Add(new BuffEffect(Libraries.Monsters[(ushort)Monster.HornedArcher], 484, 3, 1000, this, true, type) { Blend = true, Repeat = true });
                    };
                    break;
                case BuffType.HornedWarriorShield:
                    Effects.Add(new BuffEffect(Libraries.Monsters[(ushort)Monster.HornedWarrior], 912, 18, 1800, this, true, type) { Repeat = true });
                    break;
                case BuffType.HornedCommanderShield:
                    Effects.Add(effect = new BuffEffect(Libraries.Monsters[(ushort)Monster.HornedCommander], 1173, 1, 100, this, true, type) { Repeat = false, Light = 1 });
                    effect.Complete += (o, e) =>
                    {
                        Effects.Add(new BuffEffect(Libraries.Monsters[(ushort)Monster.HornedCommander], 1174, 16, 1600, this, true, type) { Repeat = true, Light = 1 });
                    };
                    break;
            }
        }
        
        
        public void RemoveBuffEffect(BuffType type) {
            PlayerObject? ob = null;

            if (Race == ObjectType.Player) {
                ob = (PlayerObject)this;
            }

            foreach (Effect effect in Effects) {
                if (effect is BuffEffect buff && buff.BuffType == type) {
                    buff.Repeat = false;
                }
            }

            if (ob is null) {
                return;
            }
            
            switch (type)
            {
                case BuffType.SwiftFeet:
                    ob.Sprint = false;
                    break;
                case BuffType.MoonLight:
                case BuffType.DarkBody:
                    ob.Sneaking = false;
                    break;
            }
        }

        
        public Color ApplyDrawColour() {
            Color drawColour = DrawColour;
            if (drawColour == Color.Gray) {
                drawColour = Color.White;
                DXManager.SetGrayscale(true);
            }
            
            return drawColour;
        }

        
        public virtual Missile? CreateProjectile(int baseIndex, MLibrary library, bool blend, int count, int interval, int skip, int lightDistance = 6, bool direction16 = true, Color? lightColour = null, uint targetID = 0) {
            return null;
        }

        
        public void Chat(string text) {
            if (ChatLabel is { IsDisposed: false }) {
                ChatLabel.Dispose();
                ChatLabel = null;
            }

            const int chatWidth = 200;
            List<string> chat = [];

            int index = 0;
            for (int i = 1; i < text.Length; i++) {
                if (TextRenderer.MeasureText(CMain.Graphics, text.AsSpan(index, i - index), ChatFont).Width > chatWidth) {
                    chat.Add(text.Substring(index, i - index - 1));
                    index = i - 1;
                }
            }
            
            chat.Add(text.Substring(index, text.Length - index));

            text = chat[0];
            for (int i = 1; i < chat.Count; i++)
                text += $"\n{chat[i]}";

            ChatLabel = new MirLabel {
                AutoSize = true,
                BackColour = Color.Transparent,
                ForeColour = Color.White,
                OutLine = true,
                OutLineColour = Color.Black,
                DrawFormat = TextFormatFlags.HorizontalCenter,
                Text = text,
            };
            ChatTime = CMain.Time + 5000;
        }
        
        
        public virtual void DrawChat() {
            if (ChatLabel == null || ChatLabel.IsDisposed) return;

            if (CMain.Time > ChatTime) {
                ChatLabel.Dispose();
                ChatLabel = null;
                return;
            }

            ChatLabel.ForeColour = Dead ? Color.Gray : Color.White;
            ChatLabel.Location = new Point(DisplayRectangle.X + (48 - ChatLabel.Size.Width) / 2, DisplayRectangle.Y - (60 + ChatLabel.Size.Height) - (Dead ? 35 : 0));
            ChatLabel.Draw();
        }

        
        public virtual void CreateLabel() {
            NameLabel = null;

            foreach (MirLabel label in LabelList) {
                if (label.Text == Name && label.ForeColour == NameColour) {
                    NameLabel = label;
                    break;
                }
            }

            if (NameLabel is { IsDisposed: false }) return;

            NameLabel = new MirLabel {
                AutoSize = true,
                BackColour = Color.Transparent,
                ForeColour = NameColour,
                OutLine = true,
                OutLineColour = Color.Black,
                Text = Name,
            };
            NameLabel.Disposing += (o, e) => LabelList.Remove(NameLabel);
            LabelList.Add(NameLabel);
        }
        
        
        public virtual void DrawName() {
            CreateLabel();

            if (NameLabel == null) return;
            
            NameLabel.Text = Name;
            NameLabel.Location = new Point(DisplayRectangle.X + (50 - NameLabel.Size.Width) / 2, DisplayRectangle.Y - (32 - NameLabel.Size.Height / 2) + (Dead ? 35 : 8)); //was 48 -
            NameLabel.Draw();
        }
        
        
        public virtual void DrawBlend() {
            DXManager.SetBlend(true, 0.3F); //0.8
            Draw();
            DXManager.SetBlend(false);
        }
        
        
        public void DrawDamages() {
            for (int i = Damages.Count - 1; i >= 0; i--) {
                Damage info = Damages[i];
                if (CMain.Time > info.ExpireTime) {
                    info.DamageLabel?.Dispose();
                    Damages.RemoveAt(i);
                }
                else {
                    info.Draw(DisplayRectangle.Location);
                }
            }
        }
        
        
        public virtual bool ShouldDrawHealth() {
            return false;
        }
        
        
        public void DrawHealth() {
            if (Dead) return;
            
            if (CMain.Time >= HealthTime) {
                if (!ShouldDrawHealth()) return;
            }
            
            if (Race != ObjectType.Player && Race != ObjectType.Monster && Race != ObjectType.Hero) return;

            int bracket = Name.IndexOf("(", StringComparison.Ordinal);
            string name = bracket < 0 ? Name : Name.Substring(bracket + 1, Name.Length - bracket - 2);

            Libraries.Prguse2.Draw(0, DisplayRectangle.X + 8, DisplayRectangle.Y - 64);
            int index = 1;
            switch (Race) {
                case ObjectType.Player:
                    if (GroupDialog.GroupList.Contains(name)) index = 10;
                    break;
                case ObjectType.Monster:
                    if (GroupDialog.GroupList.Contains(name) || name == User.Name) index = 11;
                    break;
                case ObjectType.Hero:
                    // Fails but not game breaking
                    if (HeroObject != null && GroupDialog.GroupList.Contains(HeroObject.OwnerName)) {
                        index = 11; 
                    }
                    
                    if (HeroObject != null && HeroObject.OwnerName == User.Name) {
                        index = 1; 
                        if ((HeroObject.Class != MirClass.Warrior && HeroObject.Level > 7) || HeroObject is { Class: MirClass.Warrior, Level: > 25 }) {
                            Libraries.Prguse2.Draw(10, new Rectangle(0, 0, (int)(32 * PercentMana / 100F), 4), new Point(DisplayRectangle.X + 8, DisplayRectangle.Y - 60), Color.White, false);
                        }
                    }
                    break;
            }

            Libraries.Prguse2.Draw(index, new Rectangle(0, 0, (int)(32 * PercentHealth / 100F), 4), new Point(DisplayRectangle.X + 8, DisplayRectangle.Y - 64), Color.White, false);
        }

        
        public void DrawPoison() {
            if (Poison is PoisonType.None) return;
            byte poison_count = 0;
            if (Poison.HasFlag(PoisonType.Green)) {
                DXManager.Draw(DXManager.PoisonDotBackground, new Rectangle(0, 0, 6, 6), new Vector3(DisplayRectangle.X + 7 , DisplayRectangle.Y - 21, 0.0F), Color.Black);
                DXManager.Draw(DXManager.RadarTexture, new Rectangle(0, 0, 4, 4), new Vector3(DisplayRectangle.X + 8, DisplayRectangle.Y - 20, 0.0F), Color.Green);
                poison_count++;
            }
            
            if (Poison.HasFlag(PoisonType.Red)) {
                DXManager.Draw(DXManager.PoisonDotBackground, new Rectangle(0, 0, 6, 6), new Vector3(DisplayRectangle.X + 7 + (poison_count * 5), DisplayRectangle.Y - 21, 0.0F), Color.Black);
                DXManager.Draw(DXManager.RadarTexture, new Rectangle(0, 0, 4, 4), new Vector3(DisplayRectangle.X + 8 + (poison_count * 5), DisplayRectangle.Y - 20, 0.0F), Color.Red);
                poison_count++;
            }
            
            if (Poison.HasFlag(PoisonType.Bleeding)) {
                DXManager.Draw(DXManager.PoisonDotBackground, new Rectangle(0, 0, 6, 6), new Vector3(DisplayRectangle.X + 7 + (poison_count * 5), DisplayRectangle.Y - 21, 0.0F), Color.Black);
                DXManager.Draw(DXManager.RadarTexture, new Rectangle(0, 0, 4, 4), new Vector3(DisplayRectangle.X + 8 + (poison_count * 5), DisplayRectangle.Y - 20, 0.0F), Color.DarkRed);
                poison_count++;
            }
            
            if (Poison.HasFlag(PoisonType.Slow)) {
                DXManager.Draw(DXManager.PoisonDotBackground, new Rectangle(0, 0, 6, 6), new Vector3(DisplayRectangle.X + 7 + (poison_count * 5), DisplayRectangle.Y - 21, 0.0F), Color.Black);
                DXManager.Draw(DXManager.RadarTexture, new Rectangle(0, 0, 4, 4), new Vector3(DisplayRectangle.X + 8 + (poison_count * 5), DisplayRectangle.Y - 20, 0.0F), Color.Purple);
                poison_count++;
            }
            
            if (Poison.HasFlag(PoisonType.Stun) || Poison.HasFlag(PoisonType.Dazed)) {
                DXManager.Draw(DXManager.PoisonDotBackground, new Rectangle(0, 0, 6, 6), new Vector3(DisplayRectangle.X + 7 + (poison_count * 5), DisplayRectangle.Y - 21, 0.0F), Color.Black);
                DXManager.Draw(DXManager.RadarTexture, new Rectangle(0, 0, 4, 4), new Vector3(DisplayRectangle.X + 8 + (poison_count * 5), DisplayRectangle.Y - 20, 0.0F), Color.Yellow);
                poison_count++;
            }
            
            if (Poison.HasFlag(PoisonType.Blindness)) {
                DXManager.Draw(DXManager.PoisonDotBackground, new Rectangle(0, 0, 6, 6), new Vector3(DisplayRectangle.X + 7 + (poison_count * 5), DisplayRectangle.Y - 21, 0.0F), Color.Black);
                DXManager.Draw(DXManager.RadarTexture, new Rectangle(0, 0, 4, 4), new Vector3(DisplayRectangle.X + 8 + (poison_count * 5), DisplayRectangle.Y - 20, 0.0F), Color.MediumVioletRed);
                poison_count++;
            }
            
            if (Poison.HasFlag(PoisonType.Frozen)) {
                DXManager.Draw(DXManager.PoisonDotBackground, new Rectangle(0, 0, 6, 6), new Vector3(DisplayRectangle.X + 7 + (poison_count * 5), DisplayRectangle.Y - 21, 0.0F), Color.Black);
                DXManager.Draw(DXManager.RadarTexture, new Rectangle(0, 0, 4, 4), new Vector3(DisplayRectangle.X + 8 + (poison_count * 5), DisplayRectangle.Y - 20, 0.0F), Color.Blue);
                poison_count++;
            }
            
            if (Poison.HasFlag(PoisonType.Paralysis) || Poison.HasFlag(PoisonType.LRParalysis)) {
                DXManager.Draw(DXManager.PoisonDotBackground, new Rectangle(0, 0, 6, 6), new Vector3(DisplayRectangle.X + 7 + (poison_count * 5), DisplayRectangle.Y - 21, 0.0F), Color.Black);
                DXManager.Draw(DXManager.RadarTexture, new Rectangle(0, 0, 4, 4), new Vector3(DisplayRectangle.X + 8 + (poison_count * 5), DisplayRectangle.Y - 20, 0.0F), Color.Gray);
                poison_count++;
            }
            
            if (Poison.HasFlag(PoisonType.DelayedExplosion)) {
                DXManager.Draw(DXManager.PoisonDotBackground, new Rectangle(0, 0, 6, 6), new Vector3(DisplayRectangle.X + 7 + (poison_count * 5), DisplayRectangle.Y - 21, 0.0F), Color.Black);
                DXManager.Draw(DXManager.RadarTexture, new Rectangle(0, 0, 4, 4), new Vector3(DisplayRectangle.X + 8 + (poison_count * 5), DisplayRectangle.Y - 20, 0.0F), Color.Orange);
                // poison_count++;
            }
        }

        
        public abstract void DrawBehindEffects(bool effectsEnabled);

        public abstract void DrawEffects(bool effectsEnabled);

        protected void LoopFrame(int start, int frameCount, int frameInterval, int duration) {
            Loop ??= new FrameLoop {
                Start = start,
                End = start + frameCount - 1,
                Loops = (duration / (frameInterval * frameCount)) - 1 //Remove 1 count as we've already done a loop before this is checked
            };
        }
    }

    
    public class FrameLoop {
        public MirAction Action { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
        public int Loops { get; set; }

        public int CurrentCount { get; set; }
    }

}