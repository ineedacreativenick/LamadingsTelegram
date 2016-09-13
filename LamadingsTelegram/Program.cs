using Meebey.SmartIrc4net;
using System;
using System.Configuration;
using Telegram.Bot;
using Telegram.Bot.Args;
using System.IO;
using Newtonsoft.Json;
using System.Net;
using System.Collections.Generic;

namespace LamadingsTelegram
{
    class Program
    {

        private static readonly TelegramBotClient Bot = new Telegram.Bot.TelegramBotClient(ConfigurationManager.AppSettings["TelegramBotId"]);
        public static IrcClient irc = new IrcClient();
        public static string IrcChan = ConfigurationManager.AppSettings["IrcChannel"];
        public static string ImaegeSavePath = ConfigurationManager.AppSettings["ImageSavePath"];

        public static string ImaegeUrlPath = ConfigurationManager.AppSettings["ImageUrlPath"];


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


            //bilder in ein dir speichern. dieses wird per webserver freigegeben. das letzte ist immer das origninalbild, der rest sind thumbnails. was ein scheiss
            if (messageEventArgs.Message.Type == Telegram.Bot.Types.Enums.MessageType.PhotoMessage)
            {
                try
                {
                    Console.WriteLine("image received");
                    var item = messageEventArgs.Message.Photo[messageEventArgs.Message.Photo.Length - 1];

                    //get image path via json from api
                    var fileid = Bot.GetFile(item.FileId);
                    string json;
                    using (WebClient wc = new WebClient())
                    {
                        var tempurl = "https://api.telegram.org/bot" + ConfigurationManager.AppSettings["TelegramBotId"] + "/getFile?file_id=" + item.FileId;
                        json = wc.DownloadString(tempurl);
                    }
                    dynamic stuff = JsonConvert.DeserializeObject(json);
                    var path = stuff.result.file_path;

                    //actual file is here!
                    var pathurl = @"https://api.telegram.org/file/bot" + ConfigurationManager.AppSettings["TelegramBotId"] + @"/" + path.Value;

                    //download file
                    using (WebClient wc = new WebClient())
                    {
                        string FullImageUrlPath = ImaegeUrlPath + "\\" + path.Value.Replace("photo/", "");
                        wc.DownloadFile(pathurl, ImaegeSavePath + "\\" + path.Value.Replace("photo/", ""));
                        irc.SendMessage(SendType.Message, IrcChan, messageEventArgs.Message.From.Username + " (telegram): " + FullImageUrlPath.Replace(@"\", "/"));
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine(ex);

                }
            }


            if (messageEventArgs.Message.Type == Telegram.Bot.Types.Enums.MessageType.TextMessage)
            {

                var message = messageEventArgs.Message.Text;
                irc.SendMessage(SendType.Message, IrcChan, messageEventArgs.Message.From.Username + " (telegram): " + message);
            }

        }
    }
}
