using Meebey.SmartIrc4net;
using System;
using System.Configuration;
using Telegram.Bot;
using Telegram.Bot.Args;
using System.IO;
using Newtonsoft.Json;
using System.Net;
using System.Collections.Generic;
using System.Timers;
using LamadingsTelegramDienst;

namespace LamadingsTelegram
{
    class Program
    {



        static void Main(string[] args)
        {
            Worker.Start();
            Console.ReadLine();
            Worker.Bot.StopReceiving();
            Worker.irc.Disconnect();

        }

        
    }
}
