using Meebey.SmartIrc4net;
using System;
using System.Configuration;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace LamadingsTelegram
{
    class Program
    {

        private static readonly TelegramBotClient Bot = new Telegram.Bot.TelegramBotClient(ConfigurationManager.AppSettings["TelegramBotId"]);
        public static IrcClient irc = new IrcClient();
        public static string IrcChan = ConfigurationManager.AppSettings["IrcChannel"];

        static void Main(string[] args)
        {

            #region telegram
            Bot.OnMessage += BotOnMessageReceived;
            var me = Bot.GetMeAsync().Result;
            Bot.StartReceiving();
            #endregion

            #region irc
            irc.SendDelay = 200;
            irc.AutoRetry = true;
            irc.ActiveChannelSyncing = true;
            irc.OnRawMessage += new IrcEventHandler(OnRawMessage);
            irc.OnDisconnected += Irc_OnDisconnected;
            irc.AutoReconnect = true;
            JoinIrc();



            //irc.SendMessage(SendType.Message, IrcChan, "#makeircgreatagain");

            irc.Listen();
            #endregion



            Console.ReadLine();
            irc.Disconnect();
            Bot.StopReceiving();
        }

        private static void Irc_OnDisconnected(object sender, EventArgs e)
        {
            while (!irc.IsConnected)
            {
                System.Threading.Thread.Sleep(2000);
                JoinIrc();
            }
           
        }

        /// <summary>
        /// event that fires when irc bot receives a message
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnRawMessage(object sender, IrcEventArgs e)
        {
            var x = Bot.GetMeAsync().Result;
            //only chan messages from users 
            if (!string.IsNullOrEmpty(e.Data.Message) && !string.IsNullOrEmpty(e.Data.Nick) && e.Data.Type == ReceiveType.ChannelMessage)
            {
                
                var t = Bot.SendTextMessageAsync(ConfigurationManager.AppSettings["TelegramChanId"], e.Data.Nick + "(irc): " + e.Data.Message);
            }
        }

        private static void JoinIrc()
        {
            int port = 6667;
            string[] serverlist;
            serverlist = new string[] { ConfigurationManager.AppSettings["IrcServer"] };

            irc.Connect(serverlist, port);

            irc.UseSsl = true;
            irc.EnableUTF8Recode = true;
            irc.Encoding = System.Text.Encoding.UTF8;

            irc.Login("Lamadingbot", "Stupid Bot");
            irc.RfcJoin(IrcChan);
        }


        /// <summary>
        /// event that fires when telegram bot receives a message
        /// pastes text into irc 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="messageEventArgs"></param>
        private static void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {

            var message = messageEventArgs.Message.Text;
            irc.SendMessage(SendType.Message, IrcChan, messageEventArgs.Message.From.Username + " (telegram): " + message);
        }
    }
}
