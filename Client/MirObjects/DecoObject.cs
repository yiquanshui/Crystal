﻿using Client.MirGraphics;
using Client.MirScenes;
using S = ServerPackets;

namespace Client.MirObjects
{
    public class DecoObject : MapObject
    {
        public override ObjectType Race => ObjectType.Deco;

        public override bool Blocking => false;

        public int Image1 { get; set; }


        public DecoObject(uint objectID)
            : base(objectID)
        {
        }

        public void Load(S.ObjectDeco info) {
            CurrentLocation = info.Location;
            MapLocation = info.Location;
            GameScene.Scene.MapControl.AddObject(this);
            Image1 = info.Image;
            BodyLibrary = Libraries.Deco;
        }
        
        
        public override void Process() {
            DrawLocation = new Point((CurrentLocation.X - User!.Movement.X + MapControl.OffSetX) * MapControl.CellWidth, (CurrentLocation.Y - User.Movement.Y + MapControl.OffSetY) * MapControl.CellHeight);
            DrawLocation.Offset(GlobalDisplayLocationOffset);
            DrawLocation.Offset(User.OffSetMove);
        }

        
        public override void Draw() {
            BodyLibrary.Draw(Image1, DrawLocation, DrawColour, true);
        }

        public override bool MouseOver(Point p) {
            return false;
        }

        public override void DrawBehindEffects(bool effectsEnabled)
        {
        }

        public override void DrawEffects(bool effectsEnabled)
        {
        }
    }
}
