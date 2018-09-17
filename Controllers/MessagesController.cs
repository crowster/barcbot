/*
 * Author: Jesús González Martínez
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

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            bool session = false;
            int idImageSaveIdProperty = 0;
            bool attachmentImage = false;
            #region Initialize addresskey for the channel 
            //Initialize addresskey for the channel
            var key = new AddressKey()
            {
                BotId = activity.Recipient.Id,
                ChannelId = activity.ChannelId,
                UserId = activity.From.Id,
                ConversationId = activity.Conversation.Id,
                ServiceUrl = activity.ServiceUrl
            };
            #endregion
            using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, activity))
            {
                #region Initialize bot data Store for Cosmo Db
                var botDataStore = scope.Resolve<IBotDataStore<BotData>>();
                var userData = await botDataStore.LoadAsync(key, BotStoreType.BotUserData, CancellationToken.None);

                #endregion
                #region Initialization of variables
                ObjectResultRecognition result = new ObjectResultRecognition();
                string objectId = string.Empty;
                int idImageSaved = 0;
                string name = "default";
                byte[] fileNormal = null;
                int customerId = 0;
                var message = " ";
                bool messageDefault = false;
                //Instance of the object ACFCustomer for keept  customer data
                Models.ACFCustomer customerState = new Models.ACFCustomer();
                #endregion  
                if (activity.Type == ActivityTypes.Message)
                {
                    var client = new ConnectorClient(new Uri(activity.ServiceUrl), new MicrosoftAppCredentials());
                    var reply = activity.CreateReply();
                    try
                    {
                        userData.SetProperty<bool>("creatingUser", false);
                        message = activity.Text;
                        #region Handle if user try to start a new session 
                        session = userData.GetProperty<bool>("SignIn");
                        attachmentImage = userData.GetProperty<bool>("AttachmentImage");

                        if (activity.Attachments.Count > 0)
                        {
                            if (session && attachmentImage)
                            {
                                reply.Text = "You are already signed in, if you want to schedule an appointment or get an ETicket with a different account, please sign out first using the Exit command and then sign in again ";
                                activity = new Activity();

                                //Reset the custom properties
                                using (var scopes = DialogModule.BeginLifetimeScope(Conversation.Container, activity))
                                {
                                    var botData = scopes.Resolve<IBotDataStore<BotData>>();

                                    userData = await botDataStore.LoadAsync(key, BotStoreType.BotUserData, CancellationToken.None);
                                    var privateData = await botDataStore.LoadAsync(key, BotStoreType.BotPrivateConversationData, CancellationToken.None);
                                    var conversationData = await botDataStore.LoadAsync(key, BotStoreType.BotConversationData, CancellationToken.None);
                                    //Here we set the user data properties to initial value . So the bot can register another user session
                                    userData.SetProperty<bool>("SignIn", false);
                                    userData.SetProperty<bool>("AttachmentImage", false);

                                    userData.SetProperty<int>("idImageSaveId", idImageSaved);
                                    userData.SetProperty<ACFCustomer>("customerState", new ACFCustomer());
                                    userData.SetProperty<FaceRecognitionModel>("FaceRecognitionModel", new FaceRecognitionModel());
                                    //With this code, we remove the actual userstate to avoid previous form flow in cache
                                    privateData.RemoveProperty("ResumptionContext");
                                    privateData.RemoveProperty("DialogState");
                                    await botData.SaveAsync(key, BotStoreType.BotUserData, userData, CancellationToken.None);
                                    await botData.SaveAsync(key, BotStoreType.BotPrivateConversationData, privateData, CancellationToken.None);
                                    await botData.FlushAsync(key, CancellationToken.None);
                                }
                                objectId = "";

                                attachmentImage = false;
                                messageDefault = true;
                                await client.Conversations.ReplyToActivityAsync(reply);
                            }
                        }
                        #endregion
                        #region Action to take when user input is "exit"
                        //Actions to take if the user input exit
                        if (message.ToLower().Contains("exit"))
                        {
                            activity = new Activity();

                            //Reset the custom properties
                            using (var scopes = DialogModule.BeginLifetimeScope(Conversation.Container, activity))
                            {
                                var botData = scopes.Resolve<IBotDataStore<BotData>>();

                                userData = await botDataStore.LoadAsync(key, BotStoreType.BotUserData, CancellationToken.None);
                                var privateData = await botDataStore.LoadAsync(key, BotStoreType.BotPrivateConversationData, CancellationToken.None);
                                var conversationData = await botDataStore.LoadAsync(key, BotStoreType.BotConversationData, CancellationToken.None);
                                //Here we set the user data properties to initial value . So the bot can register another user session
                                userData.SetProperty<bool>("SignIn", false);
                                userData.SetProperty<bool>("AttachmentImage", false);

                                userData.SetProperty<int>("idImageSaveId", idImageSaved);
                                userData.SetProperty<ACFCustomer>("customerState", new ACFCustomer());
                                userData.SetProperty<FaceRecognitionModel>("FaceRecognitionModel", new FaceRecognitionModel());
                                //With this code, we remove the actual userstate to avoid previous form flow in cache
                                privateData.RemoveProperty("ResumptionContext");
                                privateData.RemoveProperty("DialogState");
                                await botData.SaveAsync(key, BotStoreType.BotUserData, userData, CancellationToken.None);
                                await botData.SaveAsync(key, BotStoreType.BotPrivateConversationData, privateData, CancellationToken.None);
                                await botData.FlushAsync(key, CancellationToken.None);
                            }
                            objectId = "";
                            reply.Text = $"Your session has been restarted";
                            attachmentImage = false;
                            await client.Conversations.ReplyToActivityAsync(reply);
                        }
                        #endregion
                        #region Action to take when user input is "clean"
                        if (message.ToLower().Contains("clean"))
                        {
                            for (int i = 0; i < 50; i++)
                            {
                                reply.Text = reply.Text + " \n | ";

                            }
                            await client.Conversations.ReplyToActivityAsync(reply);
                        }
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        string error = ex.Message;
                    }
                    #region Attachment image process
                    if (activity.Attachments.Count > 0)
                    {
                        Activity activityRes = new Activity();
                        activityRes = activity;
                        attachmentImage = true;
                        userData.SetProperty<bool>("AttachmentImage", true);
                        reply.Text = $"One moment please... ";
                        await client.Conversations.ReplyToActivityAsync(reply);


                        //Reset the custom properties
                        #region reset customet properties
                        using (var scopes = DialogModule.BeginLifetimeScope(Conversation.Container, activity))
                        {
                            botDataStore = scope.Resolve<IBotDataStore<BotData>>();

                            userData = await botDataStore.LoadAsync(key, BotStoreType.BotUserData, CancellationToken.None);
                            userData.SetProperty<bool>("SignIn", true);

                            userData.SetProperty<int>("idImageSaveId", idImageSaved);
                            userData.SetProperty<ACFCustomer>("customerState", new ACFCustomer());
                            userData.SetProperty<FaceRecognitionModel>("FaceRecognitionModel", new FaceRecognitionModel());
                            await botDataStore.SaveAsync(key, BotStoreType.BotUserData, userData, CancellationToken.None);
                            await botDataStore.FlushAsync(key, CancellationToken.None);
                        }
                        #endregion
                        #region initialize variables
                        objectId = "";
                        IEnumerable<byte[]> array = null;
                        List<byte[]> listArrayBytes = null;
                        #endregion
                        //The method GetAttachmentsAsync is the encharged for obtain the array bytes, this pass a jwt to the Content url, for get the image
                        #region Get attachments
                        try
                        {
                            array = await GetAttachmentsAsync(activityRes);
                            listArrayBytes = array.ToList();
                            name = activityRes.Attachments[0].Name;
                            //Get the actual attachment inside the bot 
                        }
                        catch (Exception)
                        {
                        }
                        try
                        {
                            if (listArrayBytes == null)
                            {
                                //Instance of web Client

                                WebClient webClient = new WebClient();

                                //Get the name of the attachment

                                name = activityRes.Attachments[0].Name;

                                //Get the url where the file is saved for the bot framework

                                var url = activityRes.Attachments[0].ContentUrl;

                                //Get the array bytes from an url, this url is generated for the botFramework, we can recovery a file withuot extensions

                                fileNormal = webClient.DownloadData(activityRes.Attachments[0].ContentUrl);
                            }
                        }
                        catch (Exception)
                        {
                        }
                        byte[] file = null;
                        if (fileNormal != null)
                            file = fileNormal;
                        else
                        {
                            file = listArrayBytes[0];
                        }
                        #endregion

                        //Creation of a stream through the array bytes
                        Stream stream = new MemoryStream(file);
                        //url to conect api
                        //make an instance of FRService , this instance already has setted the URL base, urlRecognitionGroup and  urlSaveImage 
                        FRService frService = new FRService();

                        #region Save the image
                        //First save image
                        try
                        {
                            //idImageSaved is the id in the database , when attach an image , is saved in the database too . 
                            using (var scopes = DialogModule.BeginLifetimeScope(Conversation.Container, activity))
                            {
                                botDataStore = scope.Resolve<IBotDataStore<BotData>>();
                                userData = await botDataStore.LoadAsync(key, BotStoreType.BotUserData, CancellationToken.None);
                                idImageSaved = await frService.saveImage(file);
                                userData.SetProperty<int>("idImageSaveId", idImageSaved);
                                await botDataStore.SaveAsync(key, BotStoreType.BotUserData, userData, CancellationToken.None);
                                await botDataStore.FlushAsync(key, CancellationToken.None);
                            }
                        }
                        catch (Exception ex)
                        {
                            reply.Text = $"Error  " + ex.Message.ToString() + "...";
                            await client.Conversations.ReplyToActivityAsync(reply);
                        }
                        #endregion
                        #region Assign Default name
                        //If the name is null or empty I asign a defaultname
                        if (string.IsNullOrEmpty(name))
                        {
                            name = Utilities.Util.GetRandomImageName();
                        }
                        #endregion
                        #region Validate image with face recognition api
                        objectId = await frService.validateFaceRecognition(idImageSaved);
                        #endregion
                        #region Process after find the user in the face regnition group
                        if (!objectId.Contains("not found"))
                        {

                            //Divide the object Id in: name, last name and phone number
                            ObjectIdModel objectIdModel = Utilities.Util.DescomposeObjectId(objectId);
                            #region save userstate
                            using (var scopes = DialogModule.BeginLifetimeScope(Conversation.Container, activity))
                            {
                                botDataStore = scope.Resolve<IBotDataStore<BotData>>();

                                userData = await botDataStore.LoadAsync(key, BotStoreType.BotUserData, CancellationToken.None);
                                userData.SetProperty<bool>("SignIn", true);
                                //Set the FaceRecognition Model, 
                                FaceRecognitionModel faceRecognitionModel = new FaceRecognitionModel();
                                faceRecognitionModel.ObjectId = objectId;
                                faceRecognitionModel.PhotoId = idImageSaved.ToString();
                                faceRecognitionModel.Name = name;
                                faceRecognitionModel.FileName = name;

                                userData.SetProperty<FaceRecognitionModel>("FaceRecognitionModel", faceRecognitionModel);
                                await botDataStore.SaveAsync(key, BotStoreType.BotUserData, userData, CancellationToken.None);
                                await botDataStore.FlushAsync(key, CancellationToken.None);
                            }
                            #endregion
                            //Get the personal id in this case is the phone number
                            string personalId = objectIdModel.PhoneNumber;
                            #region Get the user data
                            //Get the user Data
                            WebAppoinmentsClientLibrary.Customers customerLibrary = new WebAppoinmentsClientLibrary.Customers();
                            Customer customer = new Customer();
                            try
                            {
                                customer = customerLibrary.GetCustomerByPersonalId(personalId, 0).Customer;

                            }
                            catch (Exception ex)
                            {
                                reply.Text = $"Error:  " + ex.Message.ToString();
                                await client.Conversations.ReplyToActivityAsync(reply);
                            }
                            #endregion

                            #region Create the userstate
                            /* Create the userstate */
                            using (var scopes = DialogModule.BeginLifetimeScope(Conversation.Container, activity))
                            {
                                botDataStore = scope.Resolve<IBotDataStore<BotData>>();

                                userData = await botDataStore.LoadAsync(key, BotStoreType.BotUserData, CancellationToken.None);

                                try
                                { customerId = customer.Id; }
                                catch (Exception) { }
                                customerState.CustomerId = customer.Id;
                                customerState.FirstName = customer.FirstName;
                                customerState.PhoneNumber = customer.TelNumber1;
                                // customerState.Sex = Convert.ToInt32(customer.Sex);
                                customerState.PersonaId = customer.PersonalId;
                                userData.SetProperty<ACFCustomer>("customerState", customerState);
                                await botDataStore.SaveAsync(key, BotStoreType.BotUserData, userData, CancellationToken.None);
                                await botDataStore.FlushAsync(key, CancellationToken.None);
                            }
                            #endregion
                            if (!string.IsNullOrEmpty(customer.FirstName))
                            {
                                reply.Text = $"Welcome " + customer.FirstName + " " + customer.LastName;
                                await client.Conversations.ReplyToActivityAsync(reply);
                            }

                        }
                        #endregion

                    }
                    #endregion
                    try
                    {
                        //Get the actual id ImageSaveId
                        #region Initialize variables to save idImageSaveId and customer information
                        idImageSaveIdProperty = 0;
                        idImageSaveIdProperty = userData.GetProperty<int>("idImageSaveId");
                        ACFCustomer currentACFCustomer = new ACFCustomer();
                        string customerName = "";
                        string firstName = "";
                        #endregion
                        #region Get properties of customer state
                        try
                        {
                            firstName = userData.GetProperty<ACFCustomer>("customerState").FirstName;
                        }
                        catch (Exception)
                        {

                        }

                        if (!string.IsNullOrEmpty(firstName))
                        {
                            currentACFCustomer = userData.GetProperty<ACFCustomer>("customerState");

                            customerName = currentACFCustomer.FirstName;
                        }
                        #endregion
                        session = userData.GetProperty<bool>("SignIn");
                        #region Handle the bot direction
                        if ((objectId.Contains("not found") || String.IsNullOrEmpty(objectId) || customerId == 0) && idImageSaveIdProperty > 0 && String.IsNullOrEmpty(customerName))
                        {
                            await Conversation.SendAsync(activity, () => new RegisterUserDialog(idImageSaved, name));

                        }
                        else if (!session && String.IsNullOrEmpty(objectId) && !objectId.ToLower().Contains("not found") && idImageSaveIdProperty == 0 && !messageDefault)
                        {
                            reply.Text = $"Please upload a selfie, so we can identify you";
                            await client.Conversations.ReplyToActivityAsync(reply);
                        }
                        else if (session)
                        {
                            if (!objectId.Contains("not found") && !String.IsNullOrEmpty(customerName))
                            {
                                await Conversation.SendAsync(activity, () => new RootDialog());
                            }
                        }
                        #endregion

                    }
                    catch (HttpOperationException err)
                    {
                        // handle error with HTTP status code 412 Precondition Failed
                    }
                }
                else
                {
                    HandleSystemMessage(activity);
                }
                var response = Request.CreateResponse(HttpStatusCode.OK);
                return response;
            }
        }


        private async Task<IEnumerable<byte[]>> GetAttachmentsAsync(Activity activity)
        {
            var attachments = activity?.Attachments?
           .Where(attachment => attachment.ContentUrl != null)
           .Select(c => Tuple.Create(c.ContentType, c.ContentUrl));
            if (attachments != null && attachments.Any())
            {
                var contentBytes = new List<byte[]>();
                using (var connectorClient = new ConnectorClient(new Uri(activity.ServiceUrl)))
                {
                    var token = await (connectorClient.Credentials as MicrosoftAppCredentials).GetTokenAsync();
                    foreach (var content in attachments)
                    {
                        var uri2 = new Uri(content.Item2);
                        using (var httpClient = new HttpClient())
                        {
                            try
                            {
                                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);//Bearer
                                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
                                contentBytes.Add(await httpClient.GetByteArrayAsync(uri2));
                            }
                            catch (Exception ex)
                            {

                                throw ex;
                            }

                        }
                    }
                }
                return contentBytes;
            }
            return null;
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

                IConversationUpdateActivity update = message;
                var client = new ConnectorClient(new Uri(message.ServiceUrl), new MicrosoftAppCredentials());
                if (update.MembersAdded != null && update.MembersAdded.Any())
                {
                    foreach (var newMember in update.MembersAdded)
                    {
                        if (newMember.Id != message.Recipient.Id)
                        {
                            var reply = message.CreateReply();
                            reply.Text = $"Welcome {newMember.Name}, this appointmentBot will help you to manage your appointment or get in line";
                            client.Conversations.ReplyToActivityAsync(reply);
                        }
                    }
                }
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
        public async Task saveProperty(object value, String propertyKey, Activity activity)
        {
            using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, activity))
            {
                var botDataStore = scope.Resolve<IBotDataStore<BotData>>();
                var key = new AddressKey()
                {
                    BotId = activity.Recipient.Id,
                    ChannelId = activity.ChannelId,
                    UserId = activity.From.Id,
                    ConversationId = activity.Conversation.Id,
                    ServiceUrl = activity.ServiceUrl
                };
                var userData = await botDataStore.LoadAsync(key, BotStoreType.BotUserData, CancellationToken.None);
                var privateData = await botDataStore.LoadAsync(key, BotStoreType.BotPrivateConversationData, CancellationToken.None);

                userData.SetProperty<Object>(propertyKey, value);

                await botDataStore.SaveAsync(key, BotStoreType.BotUserData, userData, CancellationToken.None);
                await botDataStore.FlushAsync(key, CancellationToken.None);
            }
        }
    }
}