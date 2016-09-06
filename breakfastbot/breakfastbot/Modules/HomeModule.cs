using System;
using System.Web;
using Nancy;
using System.Web.Services.Description;
using ServiceStack;
using Nancy.ModelBinding;
using Slack.Webhooks;
using System.Collections.Generic;

namespace breakfastbot.Modules
{
    public class HomeModule : NancyModule
    {
        /// <summary>
        /// You will need to configure this module in the Web.config file of your
        /// web and register it with IIS before being able to use it. For more information
        /// see the following link: http://go.microsoft.com/?linkid=8101007
        /// </summary>
        /// 
        private static List<developers> breaklist = new List<developers>();
        private static List<developers> temp = new List<developers>();
        private static List<developers> buuuulist = new List<developers>();


        public HomeModule ()
        {
            Post["/"] = _ =>
            {
                var model = this.Bind<HookMessage>();
                var message = string.Empty;
                Console.WriteLine(model.text.ToLower());
                if (model.token != "YWrKxANIXAOvx4NrEzJAIgqU")
                { //checks the token of incoming message if found
                    Console.WriteLine("Invalid Token\n Ignored!");
                    return null;//ignored if not recognised
                }
                if (model.text == "breaky yes")
                {// if the trigger word was found - written in correct format and response was yes
                    message = string.Format("@" + model.user_name + " Recieved! Added to breakfast list");
                    breaklist.Add(new developers// add them to yeslist -> goes into breakfastList in update method
                    {
                        slackname = model.user_name,
                        lastpay = { }
                    });
                    Console.WriteLine("'" + message + "' sent back to " + model.user_name);
                }
                if (model.text == "breaky no")
                {// if response is no
                    message = string.Format("@" + model.user_name + " Recieved! removed from this weeks breky list");
                    string name = model.user_name;
                    buuuulist.Add(new developers// adds them to nolist -> goes into buuuuList in update method
                    {
                        slackname = model.user_name,
                        lastpay = { }
                    });
                    Console.WriteLine("'" + message + "' sent back to " + model.user_name);
                }
                if (!string.IsNullOrWhiteSpace(message))// if message is not empty
                    return new SlackMessage { Text = message, Username = "breaky bot" };
                return null;
            };
        }




        public void OnLogRequest(Object source, EventArgs e)
        {
            //custom logging logic can go here
        }
    }
    public class HookMessage
    {
        public string token { get; set; }
        public string team_id { get; set; }
        public string channel_id { get; set; }
        public string channel_name { get; set; }
        public string user_id { get; set; }
        public string user_name { get; set; }
        public string command { get; set; }
        public string text { get; set; }
        public string response_url { get; set; }
        public string trigger_word { get; set; }
    }
    public class date
    {
        public string day { get; set; }
        public string month { get; set; }
        public string year { get; set; }
    }
    public class developers
    {
        public string slackname { get; set; }
        public date lastpay { get; set; }
    }
}
