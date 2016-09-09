using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.Text;
using System.Net;
using System.Collections.Specialized;
using Google.Apis.Sheets.v4;
using Google.Apis.Auth.OAuth2;
using System.IO;
using System.Threading;
using Google.Apis.Util.Store;
using Google.Apis.Services;
using Google.Apis.Sheets.v4.Data;
using System.Web.Services.Description;
using Microsoft.AspNet.WebHooks;
using System.Net.Http;
using System.Threading.Tasks;

namespace breakybot.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        private static string[] scopes = { SheetsService.Scope.Spreadsheets };
        public static List<developers> breaklist = new List<developers>();
        private static List<developers> temp = new List<developers>();
        private static List<developers> buuuulist = new List<developers>();
        private static string applicationName = "breakfastbot";
        private string urlWithAccessToken = "https://hooks.slack.com/services/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";
        private IList<IList<object>> values;
        private string spreadsheetId = "1YMLuQ1tJnTJs1FQN0yruMHAS41nIRm1FHT87pP3GCV0";
        private string range = "Sheet1!A2:D";
        public static ValueRange response = new ValueRange();
        private static  SheetsService service;
        private static SlackWebHookHandler fin = new SlackWebHookHandler();

        public ActionResult Index()
        {
            //variables - start
            var datewords = "";
            string[] done = new string[2];
            string name;
            int i = 0;
            string path = AppDomain.CurrentDomain.BaseDirectory;
            UserCredential credential;
            SlackSend client = new SlackSend(urlWithAccessToken);


            //fetch list of devs
            // look for and read credentials for accessing and updating dev table

            using (var stream =
                new System.IO.FileStream(path + "client_secret.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = Path.Combine(path, ".credentials/sheets.googleapis.com-dotnet-quickstart.json");
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }
            service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName,
            });

            // Define request parameters.
            SpreadsheetsResource.ValuesResource.GetRequest request =
                    service.Spreadsheets.Values.Get(spreadsheetId, range);

            // Prints the slacknames and paid dates of devs in a spreadsheet:
            // https://docs.google.com/spreadsheets/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/edit
            response = request.Execute();// fetch everything from range(in worksheet : Sheet1, with a range of A2:all of D(should be 14))
            values = response.Values; // put into indexed list of individualised objects
            // if a list of values has been found
            if (values.Count > 0)
            {
                // for each person in the list, create a new developer for them and add them to ateending list
                foreach (var row in values)
                {
                    name = (string)row[1];
                    datewords = (string)row[2];
                    done = datewords.Split('/');
                    temp.Add(new developers
                    {
                        slackname = name,
                        lastpay = new date { day = done[0], month = done[1], year = done[2] }
                    });
                }
            }

            client.PostMessage(text: "Process: Start!",
                       channel: "#breakfastmeet");

            foreach (var dev in temp)
            {
                client.PostMessage(text: "@" + temp[i].slackname + " can you make it for breakfast",
                       channel: "@" + temp[i].slackname); 
                i++;
            }

            return View();
        }

        public ActionResult finish()
        {
            //variables - end
            developers lastpayer = new developers();
            developers lastpayer2 = new developers(); // incase 2 payers are needed
            ValueRange response2 = response;
            int i = 0;
            int j = 0;
            int attending = 0;
            SlackSend client = new SlackSend(urlWithAccessToken);

            //fin.update(breaklist , buuuulist);
            breaklist = fin.getBreakList();
            buuuulist = fin.getBuuuuList();

            for (int b = 0; b < temp.Count(); b++)
            {
                i = 0;
                for (int c = 0; c < breaklist.Count(); c++)
                {
                    if (breaklist[c].slackname == temp[b].slackname)
                    {
                        breaklist[c] = temp[b];
                    }
                }
            }
            //we now have list of people going

            attending = breaklist.Count;
            i = 0;
            // find last person to pay
            if (attending != 0)
            {
                lastpayer = breaklist[i];
                foreach (var cooldev in breaklist)
                {
                    if (Int32.Parse(lastpayer.lastpay.year) >= Int32.Parse(breaklist[i].lastpay.year))
                    {
                        if (Int32.Parse(lastpayer.lastpay.month) >= Int32.Parse(breaklist[i].lastpay.month))
                        {
                            if (Int32.Parse(lastpayer.lastpay.day) > Int32.Parse(breaklist[i].lastpay.day))
                            {
                                lastpayer = breaklist[i];
                            }
                        }
                    }
                    i++;
                }// end foreach
                // if there more than 10 people attending find another person to help pay
                // the next person would be the next person who would pay

                if (breaklist.Count > 10)
                {
                    foreach (var otherdev in breaklist)
                    {
                        j = 0;
                        if (Int32.Parse(lastpayer2.lastpay.year) <= Int32.Parse(breaklist[j].lastpay.year))
                        {
                            if (Int32.Parse(lastpayer2.lastpay.month) <= Int32.Parse(breaklist[j].lastpay.month))
                            {
                                if (Int32.Parse(lastpayer2.lastpay.day) < Int32.Parse(breaklist[j].lastpay.day))
                                {
                                    if (lastpayer.slackname != breaklist[j].slackname)
                                    {
                                        lastpayer2 = breaklist[j];
                                    }
                                }
                            }
                        }
                        j++;
                    }// end foreach otherdev
                }

                // posting messages to channel on results of proccess
                client.PostMessage(text: "It is @" + lastpayer.slackname + " turn to pay",
                    channel: "#breakfastmeet");
                if (!(lastpayer2.slackname == null))
                {
                    client.PostMessage(text: "and @" + lastpayer2.slackname + " has to pay aswell\n because of too many people!",
                        channel: "#breakfastmeet");
                }
                client.PostMessage(text: "there are a total of " + attending + " people attending breakfast!",
                    channel: "#breakfastmeet");
            }
            else
            {
                client.PostMessage(text: "no breakky :(",
                    channel: "#breakfastmeet");
            }
            //start updating dates for google sheets docs
            for (i = 0; i < response.Values.Count(); i++)
            {
                if (response.Values[i][1].ToString() == lastpayer.slackname)
                {
                    response.Values[i][2] = DateTime.Now.ToString("dd/MM/yyyy");
                }
                if (lastpayer2.slackname != "")
                {
                    if (response.Values[i][1].ToString() == lastpayer2.slackname)
                    {
                        response.Values[i][2] = DateTime.Now.ToString("dd/MM/yyyy");
                    }
                }
            }
            // update table with new last pay date of devs who just payed for breakfast
            string spreadsheetId2 = "1YMLuQ1tJnTJs1FQN0yruMHAS41nIRm1FHT87pP3GCV0";
            string range2 = "Sheet1!A2:D14";
            SpreadsheetsResource.ValuesResource.UpdateRequest request3 =
                service.Spreadsheets.Values.Update(response, spreadsheetId2, range2);
            // execute order 666
            request3.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            request3.Execute();

            client.PostMessage(text: "better luck next time",
                channel: "#breakfastmeet");
            client.PostMessage(text: "Process: Stop",
                channel: "#breakfastmeet");
            return View();
        }
    }
    //dat weebhook
    public class SlackWebHookHandler : WebHookHandler
    {
        private static List<developers> breaklist = new List<developers>();
        private static List<developers> temp2 = new List<developers>();
        private static List<developers> tempbuuuulist = new List<developers>();
        private developers a = new developers { };
        public override Task ExecuteAsync(string generator, WebHookHandlerContext context)
        {
            NameValueCollection nvc;
            if (context.TryGetData<NameValueCollection>(out nvc))
            {
                 a = new developers
                { 
                    slackname = nvc["user_name"],
                    lastpay = { }
                };
                string question = nvc["subtext"];
                string msg = "";

                if (question == "yes")
                {
                    if (!(breaklist.Contains(a)) == true)
                    {
                        breaklist.Add(a);
                        if (tempbuuuulist.Contains(a) == true)
                        {
                            tempbuuuulist.Remove(a);
                        }
                        msg = "added " + breaklist.Count() + " " + tempbuuuulist.Count();
                    }
                }
                else if (question == "no")
                {
                    if (!(tempbuuuulist.Contains(a) == true))
                    {
                        tempbuuuulist.Add(a);
                        if (breaklist.Contains(a) == true)
                        {
                            breaklist.Remove(a);
                        }
                        msg = "removed " + breaklist.Count() + " " + tempbuuuulist.Count();
                    }
                }
                else if (question == "status")
                {
                    for (int i = 0; i < breaklist.Count(); i++)
                    {
                        msg += " " + breaklist[i].slackname + "\n";
                    }
                } else
                {
                    Random rnd = new Random();
                    int num = rnd.Next(1, 7);
                    if (num == 1) {
                        msg = "pls .... Could you not";
                    }
                    if (num == 2)
                    {
                        msg = "don't break me";
                    }
                    if (num == 3)
                    {
                        msg = "I'm sorry Dave, i can't let you do that.";
                    }
                    if (num == 4)
                    {
                        msg = "leave breaky bot alone! :(";
                    }
                    if (num == 5)
                    {
                        msg = "Just what do you think you're doing, Dave?";
                    }
                    if (num == 6)
                    {
                        msg = "we can talk about this";
                    }
                }
               SlackResponse reply = new SlackResponse(msg);
               context.Response = context.Request.CreateResponse(reply);
           }
           return Task.FromResult(true);
       }
        public void update(List<developers> breaky, List<developers> buuuu)
        {
            breaky = breaklist;
            buuuu = tempbuuuulist;
        }
        public List<developers> getBreakList()
        {
            return breaklist;
        }
        public List<developers> getBuuuuList()
        {
            return tempbuuuulist;
        }
   }

    //This class serializes into the Json payload required by Slack Incoming WebHooks
    public class Payload
    {
        [JsonProperty("channel")]
        public String Channel { get; set; }

        [JsonProperty("text")]
        public String Text { get; set; }
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

    //slack send
    public class SlackSend
    {
        private readonly Uri _uri;
        private readonly Encoding _encoding = new UTF8Encoding();

        public SlackSend(string urlWithAccessToken)
        {
            _uri = new Uri(urlWithAccessToken);
        }

        //Post a message using simple strings
        public void PostMessage(string text, string channel)
        {
            Payload payload = new Payload()
            {
                Channel = channel,
                Text = text
            };
            PostMessage(payload);
        }

        //Post a message using a Payload object
        public void PostMessage(Payload payload)
        {
            string payloadJson = JsonConvert.SerializeObject(payload);

            using (WebClient client = new WebClient())
            {
                NameValueCollection data = new NameValueCollection();
                data["payload"] = payloadJson;
                var response = client.UploadValues(_uri, "POST", data);
                string responseText = _encoding.GetString(response);
            }
        }
    }
}