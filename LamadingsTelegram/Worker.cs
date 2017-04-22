using Meebey.SmartIrc4net;
using System;
using System.Configuration;
using Telegram.Bot;
using Telegram.Bot.Args;
using System.Timers;
using System.Net;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace LamadingsTelegramDienst
{


    static class Worker
    {

        public static readonly TelegramBotClient Bot = new Telegram.Bot.TelegramBotClient(ConfigurationManager.AppSettings["TelegramBotId"]);
        public static IrcClient irc = new IrcClient();
        public static string IrcChan = ConfigurationManager.AppSettings["IrcChannel"];
        public static string ImaegeSavePath = ConfigurationManager.AppSettings["ImageSavePath"];

        public static string ImaegeUrlPath = ConfigurationManager.AppSettings["ImageUrlPath"];
        //public static Timer t = new Timer();

        public static void Start()
        {
            #region telegram
            Bot.OnMessage += BotOnMessageReceived;
            var me = Bot.GetMeAsync().Result;
            Bot.StartReceiving();
            #endregion

            #region irc
            irc.SendDelay = 200;
            //irc.AutoRetry = true;
            irc.ActiveChannelSyncing = true;
            irc.OnRawMessage += new IrcEventHandler(OnRawMessage);
            
            irc.AutoReconnect = true;
            irc.PingTimeout = 99999999;
            //irc.SocketReceiveTimeout = 999999;
            JoinIrc();


            //t.Interval = 60000;
            //t.Elapsed += CheckIrc;
            //t.Start();

            //irc.SendMessage(SendType.Message, IrcChan, "#makeircgreatagain");

            irc.Listen();

            #endregion
        }


        private static void CheckIrc(object sender, ElapsedEventArgs e)
        {

            log("checkirc triggered");
            if (irc.GetChannel(IrcChan) == null)
            {
                JoinIrc();
            }
        }

        private static void Irc_OnDisconnected(object sender, EventArgs e)
        {

            log("disconnected triggered");
            while (!irc.IsConnected || irc.GetChannel(IrcChan) == null)
            {
                System.Threading.Thread.Sleep(30000);
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
            try
            {
                log(e.Data.Message);
                var x = Bot.GetMeAsync().Result;
            }
            catch (Exception exception)
            {
                log(exception.ToString());
                
            }
           
            //only chan messages from users 
            if (!string.IsNullOrEmpty(e.Data.Message) && !string.IsNullOrEmpty(e.Data.Nick) && e.Data.Type == ReceiveType.ChannelMessage)
            {

                try
                {
                    var t = Bot.SendTextMessageAsync(ConfigurationManager.AppSettings["TelegramChanId"], e.Data.Nick + ": " + e.Data.Message);
                }
                catch (Exception ex )
                {

                    log(ex.ToString());
                }

            }
        }

        private static void JoinIrc()
        {

            log("joinirc triggered");
            try
            {
                int port = 6697;
                string[] serverlist;
                serverlist = new string[] { ConfigurationManager.AppSettings["IrcServer"] };


                irc.UseSsl = true;
                irc.EnableUTF8Recode = true;
                irc.Encoding = System.Text.Encoding.UTF8;
                irc.OnDisconnected += Irc_OnDisconnected;

                if (!irc.IsConnected)
                {
                    irc.Connect(serverlist, port);
                }




                irc.Login("Lamadingbot", "Stupid Bot");
                irc.RfcJoin(IrcChan);
            }
            catch (Exception ex)
            {
                log(ex.ToString());
            }
           
        }
            
        private static void log(string logmessage)
        {
            using (StreamWriter writer = new StreamWriter("log" + DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Day.ToString() + ".txt", true))
            {
                writer.WriteLine(DateTime.Now + "   " + logmessage);
            }

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
            if (messageEventArgs.Message.Type == Telegram.Bot.Types.Enums.MessageType.PhotoMessage || messageEventArgs.Message.Type == Telegram.Bot.Types.Enums.MessageType.DocumentMessage)
            {
                try
                {
                    Console.WriteLine("image received");

                    string fileid = "";
                    if (messageEventArgs.Message.Photo != null)
                    {
                        fileid = messageEventArgs.Message.Photo[messageEventArgs.Message.Photo.Length - 1].FileId;
                    }
                    if (messageEventArgs.Message.Document != null)
                    {
                        fileid = messageEventArgs.Message.Document.FileId;
                    }
                    if (messageEventArgs.Message.Sticker != null)
                    {
                        fileid = messageEventArgs.Message.Sticker.FileId;
                    }

                    //get image path via json from api
                    var file = Bot.GetFile(fileid);
                    string json;
                    using (WebClient wc = new WebClient())
                    {
                        var tempurl = "https://api.telegram.org/bot" + ConfigurationManager.AppSettings["TelegramBotId"] + "/getFile?file_id=" + fileid;
                        json = wc.DownloadString(tempurl);
                    }
                    dynamic stuff = JsonConvert.DeserializeObject(json);
                    var path = stuff.result.file_path;

                    //actual file is here!
                    var pathurl = @"https://api.telegram.org/file/bot" + ConfigurationManager.AppSettings["TelegramBotId"] + @"/" + path.Value;

                    //download file
                    //using (WebClient wc = new WebClient())
                    //{
                    //    string FullImageUrlPath = ImaegeUrlPath + "\\" + path.Value.Replace("photo/", "").Replace("document/","");
                    //    wc.DownloadFile(pathurl, ImaegeSavePath + "\\" + path.Value.Replace("photo/", "").Replace("document/",""));
                    //    irc.SendMessage(SendType.Message, IrcChan, messageEventArgs.Message.From.Username + ": " + FullImageUrlPath.Replace(@"\", "/"));
                    //}

                    //noooo post the file
                    irc.SendMessage(SendType.Message, IrcChan, messageEventArgs.Message.From.Username + ": " + pathurl);

                }

                catch (Exception ex)
                {
                    Console.WriteLine(ex);

                }
            }


            if (messageEventArgs.Message.Type == Telegram.Bot.Types.Enums.MessageType.TextMessage)
            {

                var message = messageEventArgs.Message.Text;


                if (message == "!nicklist" || message == "!Nicklist" || message == "!Nickliste")
                {
                    try
                    {
                        var userslist = new List<string> { "" };
                        var chan = irc.GetChannel(IrcChan);

                        if (chan != null)
                        {

                            userslist = new List<string>();
                            foreach (var item in chan.Users.Keys)
                            {
                                userslist.Add(item.ToString());
                            }

                            Bot.SendTextMessageAsync(ConfigurationManager.AppSettings["TelegramChanId"], "Mein Herr und Gebieter, hier ist deine Nicklist: " + string.Join(", ", userslist));
                        }
                        return;
                    }
                    catch (Exception ex)
                    {
                    }
                }

                if (message == "!Topic" || message == "!topic")
                {
                    try
                    {
                        var userslist = new List<string> { "" };
                        var chan = irc.GetChannel(IrcChan);

                        if (chan != null)
                        {
                            Bot.SendTextMessageAsync(ConfigurationManager.AppSettings["TelegramChanId"], "Mein Herr und Gebieter, die Nuttem im Irc sprechen über: " + chan.Topic);
                        }
                        return;
                    }
                    catch (Exception ex)
                    {
                    }
                }
                irc.SendMessage(SendType.Message, IrcChan, messageEventArgs.Message.From.Username + ": " + message);
            }

        }

    }
}
