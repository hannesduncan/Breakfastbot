﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.Text;
using System.Net;
using System.Collections.Specialized;
using System.Collections.Generic;
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
        private static List<developers> breaklist = new List<developers>();
        private static List<developers> temp = new List<developers>();
        private static List<developers> buuuulist = new List<developers>();
        private static string applicationName = "breakfastbot";
        private string urlWithAccessToken = "https://hooks.slack.com/services/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";
        private IList<IList<object>> values;
        private string spreadsheetId = "1YMLuQ1tJnTJs1FQN0yruMHAS41nIRm1FHT87pP3GCV0";
        private string range = "Sheet1!A2:D";
        private ValueRange response = new ValueRange();
        private SheetsService service;

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
            //variables - end
            int j = 0;
            int attending = 0;
            developers lastpayer = new developers();
            developers lastpayer2 = new developers(); // incase 2 payers are needed
            ValueRange response2 = response;

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
                       channel: "#breakytest");

            foreach (var dev in temp)
            {
                client.PostMessage(text: "@" + temp[i].slackname + " can you make it for breakfast",
                       channel: "@hannes"); //+ temp[i].slackname);
                i++;
            }





            return View();
        }
    }
    //dat weebhook
    public class SlackWebHookHandler : WebHookHandler
    {
        public override Task ExecuteAsync(string generator, WebHookHandlerContext context)
        {
            NameValueCollection nvc;
            if (context.TryGetData<NameValueCollection>(out nvc))
            {
                string msg = "echo";
               SlackResponse reply = new SlackResponse(msg);
               context.Response = context.Request.CreateResponse(reply);
           }
           return Task.FromResult(true);
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