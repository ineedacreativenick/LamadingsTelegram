using Meebey.SmartIrc4net;
using System;
using System.Configuration;
using System.ServiceProcess;
using Telegram.Bot;
using Telegram.Bot.Args;
using System.Timers;
using System.Net;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace LamadingsTelegramDienst
{




    public partial class Service1 : ServiceBase
    {



        public Service1()
        {
            Worker.Start();
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {

            


        }

        protected override void OnStop()
        {
            Worker.Bot.StopReceiving();
            Worker.irc.Disconnect();
        }


       

    }
}

