using System.Drawing;

namespace Server.Library.MirDatabase.Conquest
{

    public class FlagInfo
    {
        public int Index;
        public Point Location;
        public string Name =  string.Empty;
        public string FileName = string.Empty;

        public FlagInfo() { }

        public FlagInfo(BinaryReader reader)
        {
            Index = reader.ReadInt32();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Name = reader.ReadString();
            FileName = reader.ReadString();
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(Index);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write(Name);
            writer.Write(FileName);
        }

        public override string ToString()
        {
            return $"{Index} - {Name} ({Location})";
        }
    }
}
