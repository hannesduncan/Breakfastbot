




using System.Web.Http;

namespace breakfastbot
{
    public static class WebHookConfig
    {
        public static void Register(HttpConfiguration config)
        {

			config.InitializeReceiveSlackWebHooks();

        }
    }
}
