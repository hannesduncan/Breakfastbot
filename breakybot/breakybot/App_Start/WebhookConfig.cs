using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;

namespace breakybot.App_Start
{
    public class WebhookConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.InitializeReceiveSlackWebHooks();
        }
    }
}