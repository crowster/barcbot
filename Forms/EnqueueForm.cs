/*
 * Author: Jesús González Martínez
 * Date created: November 16, 2017
 * Description: This class is a implements an enqueue form flow, and this is in charge of enqueue customer cases 
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
using System.Threading;

namespace BarclayBankBot.Forms
{
    [Serializable]
    public class EnqueueForm
    {
        #region Properties
        //This property is for save the office
        public string UserId;
        public static IDialogContext context { get; set; }
        #endregion
        #region Methods
        /// <summary>
        /// Get a list of the actual services, optional we can filter by the selected unit
        /// </summary>
        /// <returns></returns>
        /// 
        static List<OTempus.Library.Class.Service> GetServicesOtempues(int unitId)
        {
            return AppoinmentService.GetListServices(unitId);
        }
        /// <summary>
        /// Get a list with the actual units configured
        /// </summary>
        /// <returns></returns>
        static List<OTempus.Library.Class.Unit> GetListUnitsConfigured()
        {
            return AppoinmentService.GetListUnitsConfigured();
        }
        #endregion
        #region Creation of IForm
        public static IForm<EnqueueForm> BuildForm()
        {
            OnCompletionAsyncDelegate<EnqueueForm> processOrder = async (context, state) =>
            {

                //Get the actual state of the FaceRecognitionModel , for then get the object id, photoId, name and file name
                FaceRecognitionModel faceRecognitionState = new FaceRecognitionModel();
                AppoinmentService appointmentService = new AppoinmentService();
                try
                {
                    if (!context.UserData.TryGetValue<FaceRecognitionModel>("FaceRecognitionModel", out faceRecognitionState)) { faceRecognitionState = new FaceRecognitionModel(); }

                    FRService frService = new FRService();
                    int caseId = await frService.enqueueCustomer(faceRecognitionState.ObjectId, faceRecognitionState.PhotoId, faceRecognitionState.Name, faceRecognitionState.FileName);

                    //Get the actual user state of the customer
                    ACFCustomer customerState = new ACFCustomer();
                    try
                    {
                        if (!context.UserData.TryGetValue<ACFCustomer>("customerState", out customerState)) { customerState = new ACFCustomer(); }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Not exists a user session");
                    }
                    //In this case I have the case, that has been enqueue, but the middleware don´thave a method for get by case id,
                    //but have yes a method for get a list of cases of customer id
                    if (caseId > 0)
                    {
                        Case _case = AppoinmentService.GetCaseById(customerState.CustomerId);
                        await context.PostAsync($"Your ticket was created, your number is: " + _case.QCode + _case.QNumber);
                    }
                    else
                    {
                        throw new Exception("Error: The case Id = 0");
                    }
                }
                catch (Exception ex)
                {
                    await context.PostAsync($"Failed with message: {ex.Message}");
                }
            };
            CultureInfo ci = new CultureInfo("en");
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
            FormBuilder<EnqueueForm> form = new FormBuilder<EnqueueForm>();
            return form   //Message("Wait a moment please")
                          .Field(new FieldReflector<EnqueueForm>(nameof(UserId)).SetActive(InactiveField))
                          .AddRemainingFields()
                          .OnCompletion(processOrder)
                          .Build();
        }
        /// <summary>
        /// This method is inside a delegate that inactive the specific field when form flow is running, through the bool property
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private static bool InactiveField(EnqueueForm state)
        {
            bool setActive = false;
            return setActive;
        }

        #endregion

    };
}