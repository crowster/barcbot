/*
* Author: Jesús González Martínez
* Date created: November 16, 2017
* Description: This class is a implements an save form flow, and this is in charge of save a new customer in QFlow
* the form flow will guide the user and ask him about his information, then the bot will save and show a Welcome message
* Version 1.0 - Sept 20, 2017 - Initial version
* Version 2.0 - November 11, 2017 - Bot for Get inline and manage appointments
*/


using BarclayBankBot.Models;
using BarclayBankBot.Services;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.FormFlow.Advanced;
using OTempus.Library.Class;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace BarclayBankBot.Forms
{
    [Serializable]
    public class SaveCustomerFormApp
    {
        #region Properties
        [Prompt("Please type your name: {||} ")]
        public string name;
        public string customerId;
        [Prompt("Please type your {&}:{||} ")]
         public string LastName;
        [Prompt("Please type your phone number (with no dashes): {||} ")]
        public string PhoneNumber;
        public static IDialogContext context { get; set; }
        public static int savedImageId { get; set; }
        public static string imageName { get; set; }
        #endregion
        #region Creation of IForm
        /// <summary>
        /// This method create IForm for save the customer information
        /// </summary>
        /// <returns></returns>
        public static IForm<SaveCustomerFormApp> BuildForm()
        {
            OnCompletionAsyncDelegate<SaveCustomerFormApp> processOrder = async (context, state) =>
            {
                ACFCustomer customer = new ACFCustomer();
            try
            {
                customer.CustomerId = 0;//It is setted to 0, beacuse we will create new user, in other hand we can pass the id for update the record

                    if (!string.IsNullOrEmpty(state.name)&& !string.IsNullOrEmpty(state.LastName)) {
                        try
                        {
                            customer.FirstName = state.name;
                            customer.LastName = state.LastName;
                        }
                        catch (Exception)
                        {
                        }
                    }
                    customer.PhoneNumber = state.PhoneNumber;
                    int customerId = AppoinmentService.SaveCustomer(customer.PhoneNumber, customer.FirstName,
                    customer.LastName,  customer.PhoneNumber, customer.CustomerId);
                    state.customerId = customerId.ToString();
                    if (customerId > 0)
                    {
                        FRService frService = new FRService();
                        //Get the idImage Saved
                        int idImageSaved = 0;
                        if (!context.UserData.TryGetValue<int>("idImageSaveId", out idImageSaved)) { idImageSaved = 0; }
                        //Set the FaceRecognition Model, 
                        FaceRecognitionModel faceRecognitionModel = new FaceRecognitionModel();
                        faceRecognitionModel.ObjectId = Utilities.Util.generateObjectId(customer.FirstName, customer.LastName, customer.PhoneNumber);
                        faceRecognitionModel.PhotoId = idImageSaved.ToString();
                        faceRecognitionModel.Name = imageName;
                        faceRecognitionModel.FileName = imageName;
                        context.UserData.SetValue<FaceRecognitionModel>("FaceRecognitionModel", faceRecognitionModel);
                        bool uploadImage = await frService.uploadImage(customer.FirstName, customer.LastName, customer.PhoneNumber
                        , imageName, idImageSaved);
                        await context.PostAsync("Congratulations, your account was created!");
                        /* Create the userstate */
                        //Instance of the object ACFCustomer for keept the customer
                        ACFCustomer customerState = new ACFCustomer();
                        customerState.CustomerId = customerId;
                        customerState.FirstName = customer.FirstName;
                        customerState.PhoneNumber = customer.PhoneNumber;
                        customerState.PersonaId = customer.PhoneNumber;
                        context.UserData.SetValue<ACFCustomer>("customerState", customerState);
                        context.UserData.SetValue<bool>("SignIn", true);
                    }
                }
                catch (Exception ex)
                {
                    await context.PostAsync("We have an error: " + ex.Message);
                }
            };
            CultureInfo ci = new CultureInfo("en");
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
            var culture = Thread.CurrentThread.CurrentUICulture;
            var form = new FormBuilder<SaveCustomerFormApp>();
            var yesTerms = form.Configuration.Yes.ToList();
            var noTerms = form.Configuration.No.ToList();
            yesTerms.Add("Yes");
            noTerms.Add("No");
            form.Configuration.Yes = yesTerms.ToArray();
            return form//.Message("Fill the next information, please")
                       .Field(nameof(name))
                       .Field(nameof(LastName))
                       .Field(nameof(PhoneNumber),validate:async(state, response) =>
                       {
                           //This source code appear in warning beacuse not have a method async implemented, but works good without these method
                           Regex rgx = new Regex(@"^[1-9][0-9]{7,14}$");
                           //Regex rgx = new Regex(@"^(\+44\s?7\d{3}|\(?07\d{3}\)?)\s?\d{3}\s?\d{3}$");
                           var result = new ValidateResult { IsValid = true,Value= response };
                           if (!string.IsNullOrEmpty(result.Value.ToString()))
                           {
                               if (rgx.IsMatch(result.Value.ToString()))
                               {
                                   result.IsValid = true;
                               }
                               else
                               {
                                   result.Feedback = "Invalid phone number format";
                                   result.IsValid = false;
                               }
                           }
                           return result;
                       })
                       .Field(new FieldReflector<SaveCustomerFormApp>(nameof(customerId)).SetActive(inActiveField))
                     .AddRemainingFields()
                     .OnCompletion(processOrder)
                     .Build();
        }
        /// <summary>
        /// This method is inside a delegate that inactive the specific field when form flow is running, through the bool property
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private static bool inActiveField(SaveCustomerFormApp state)
        {
            return false;
        }
    };
    #endregion
}
