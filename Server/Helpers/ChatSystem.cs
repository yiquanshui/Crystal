using Server.MirEnv;
namespace Server.Helpers
{
    internal static class ChatSystem
    {

         static Env Env => Env.Main;


         public static void SystemMessage(string chatMessage, bool triggerBroadcastInfo = false)
        {
            if (string.IsNullOrEmpty(chatMessage))
            {
                return;
            }

            foreach (var pl in Env.Players)
            {
                pl.ReceiveChat(chatMessage, ChatType.System);

                if (triggerBroadcastInfo) 
                {
                    pl.BroadcastInfo();
                }
            }

        }
    }
}
