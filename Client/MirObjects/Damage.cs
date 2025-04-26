using Client.MirControls;

namespace Client.MirObjects
{
    public class Damage
    {
        public string Text { get; }
        public Color Colour { get; }
        public int Distance { get; }
        public long ExpireTime { get; }
        public double Factor { get; }
        public int Offset { get; set; }

        public MirLabel? DamageLabel { get; set; }

        public Damage(string text, int duration, Color colour, int distance = 50) {
            ExpireTime = CMain.Time + duration;
            Text = text;
            Distance = distance;
            Factor = 1.0f * duration / Distance;
            Colour = colour;
        }

        
        public void Draw(Point displayLocation) {
            long timeRemaining = ExpireTime - CMain.Time;

            DamageLabel ??= new MirLabel {
                AutoSize = true,
                BackColour = Color.Transparent,
                ForeColour = Colour,
                OutLine = true,
                OutLineColour = Color.Black,
                Text = Text,
                Font = new Font(Settings.FontName, 8F, FontStyle.Bold)
            };

            displayLocation.Offset(15 - (Text.Length * 3), ((int)(timeRemaining / Factor)) - Distance - 75 - Offset);

            DamageLabel.Location = displayLocation;
            DamageLabel.Draw();
        }
    }

}
