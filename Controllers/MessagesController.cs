/*
 * Author: Jes�s Gonz�lez Mart�nez
 * Date created: November 16, 2017
 * Description: This controller is the firt step in the flow of the bot, and it determines the direction of the flow
 * This controller wait for attachment input for recognize the user, if there are a record with the picture attachment
 * the bot will show your session, in other hand you will create a new user
 * Version 1.0 - Sept 20, 2017 - Initial version
 * Version 2.0 - November 11, 2017 - Bot for Get inline and manage appointments
 * Version 3.0 - December 13, 2017 - Implementation of CosmoDB to storage the user state instead the state client of bot framework
 * 
 */

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using BarclayBankBot.Dialogs;
using BarclayBankBot.Services;
using BarclayBankBot.Models;
using System.Collections.Generic;
using Microsoft.Rest;
using Face_RecognitionLibrary;
using System.IO;
using OTempus.Library.Class;
using System.Net.Http.Headers;
using Microsoft.Bot.Builder.Internals.Fibers;
using Microsoft.Bot.Builder.Dialogs.Internals;
using System.Threading;
using Autofac;

namespace BarclayBankBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {

        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                await Conversation.SendAsync(activity, () => new Dialogs.RootDialog());
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}
