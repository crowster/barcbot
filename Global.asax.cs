using System;
using Autofac;
using System.Web.Http;
using System.Configuration;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using System.Security.Claims;
using System.Web;
using System.Reflection;

namespace BarclayBankBot
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
          
            Conversation.UpdateContainer(
               builder =>
               {
                   builder.RegisterModule(new AzureModule(Assembly.GetExecutingAssembly()));
                   var uri = new Uri(ConfigurationManager.AppSettings["DocumentDbUrl"]);
                   var key = ConfigurationManager.AppSettings["DocumentDbKey"];
                   var store = new DocumentDbBotDataStore(uri, key);

                   builder.Register(c => store)
                       .Keyed<IBotDataStore<BotData>>(AzureModule.Key_DataStore)
                       .AsSelf()
                       .SingleInstance();



               });
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
