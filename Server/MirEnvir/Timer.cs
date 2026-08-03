namespace Server.MirEnvir
{
    public class Timer
    {
        private static Env Env
        {
            get { return Env.Main; }
        }

        public string Key;
        public byte Type;
        public int Seconds;

        public long RelativeTime;

        public Timer(string key, int seconds, byte type)
        {
            Key = key;
            Seconds = seconds;
            Type = type;

            RelativeTime = Env.Time + (seconds * Settings.Second);
        }
    }
}
