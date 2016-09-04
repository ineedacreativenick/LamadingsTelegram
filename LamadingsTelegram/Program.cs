using Meebey.SmartIrc4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace LamadingsTelegram
{
    class Program
    {

        private static readonly TelegramBotClient Bot = new Telegram.Bot.TelegramBotClient("223796466:AAGbLjUUnxhDaybfieAiFEFfPrgcJQ9MUbk");
        public static IrcClient irc = new IrcClient();


        static void Main(string[] args)
        {

            #region telegram
            Bot.OnMessage += BotOnMessageReceived;

            var me = Bot.GetMeAsync().Result;

            Console.Title = me.Username;

            Bot.StartReceiving();

            #endregion


            irc.SendDelay = 200;
            irc.AutoRetry = true;
            irc.ActiveChannelSyncing = true;
            irc.OnRawMessage += new IrcEventHandler(OnRawMessage);



            string[] serverlist;
            serverlist = new string[] { "irc.german-freakz.net" };

            int port = 6667;
            irc.Connect(serverlist, port);


            irc.Login("Lamabot", "Stupid Bot");
            irc.RfcJoin("#lamadings");
            irc.SendMessage(SendType.Message, "#lamadings", "test message");

            irc.Listen();



            Console.ReadLine();
            Bot.StopReceiving();
        }

        private static void OnRawMessage(object sender, IrcEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data.Message) && !string.IsNullOrEmpty(e.Data.Nick) && e.Data.Type == ReceiveType.ChannelMessage)
            {

                var t = Bot.SendTextMessageAsync("-176245781", e.Data.Nick + "(irc): " + e.Data.Message);
            }
        }

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {

            var message = messageEventArgs.Message.Text;
            irc.SendMessage(SendType.Message, "#lamadings", messageEventArgs.Message.From.Username + " (telegram): " + message);
            //var t = await Bot.SendTextMessageAsync("-176245781", "nachricht erhalten: " + messageEventArgs.Message.From.Username + message);
        }
    }
}
