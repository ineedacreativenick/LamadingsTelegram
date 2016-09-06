using Meebey.SmartIrc4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace LamadingsTelegramDienst
{




    public partial class Service1 : ServiceBase
    {
        private static readonly TelegramBotClient Bot = new Telegram.Bot.TelegramBotClient(ConfigurationManager.AppSettings["TelegramBotId"]);
        public static IrcClient irc = new IrcClient();
        public static string IrcChan = ConfigurationManager.AppSettings["IrcChannel"];


        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {


            #region telegram
            Bot.OnMessage += BotOnMessageReceived;

            var me = Bot.GetMeAsync().Result;

            
            Bot.StartReceiving();

            #endregion

            irc.SendDelay = 200;
            irc.AutoRetry = true;
            irc.ActiveChannelSyncing = true;
            irc.OnRawMessage += new IrcEventHandler(OnRawMessage);

            string[] serverlist;
            serverlist = new string[] { ConfigurationManager.AppSettings["IrcServer"] };

            int port = 6667;
            irc.Connect(serverlist, port);
            //irc.UseSsl = true;
            irc.EnableUTF8Recode = true;
            irc.Encoding = System.Text.Encoding.UTF8;

            irc.Login("Lamabot", "Stupid Bot");
            irc.RfcJoin(IrcChan);
            irc.SendMessage(SendType.Message, IrcChan, "#makeircgreatagain");

            irc.Listen();

        }

        protected override void OnStop()
        {
            Bot.StopReceiving();
            irc.Disconnect();
        }


        private static void OnRawMessage(object sender, IrcEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data.Message) && !string.IsNullOrEmpty(e.Data.Nick) && e.Data.Type == ReceiveType.ChannelMessage)
            {

                var t = Bot.SendTextMessageAsync(ConfigurationManager.AppSettings["TelegramBotId"], e.Data.Nick + "(irc): " + e.Data.Message);
            }
        }

        private static  void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {

            var message = messageEventArgs.Message.Text;
            irc.SendMessage(SendType.Message, IrcChan, messageEventArgs.Message.From.Username + " (telegram): " + message);
            //var t = await Bot.SendTextMessageAsync("-176245781", "nachricht erhalten: " + messageEventArgs.Message.From.Username + message);
        }
    }
}
