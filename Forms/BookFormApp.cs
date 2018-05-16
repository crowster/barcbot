/*
* Author: Jesús González Martínez
* Date created: November 16, 2017
* Description: This class is a implements an book form flow, and this is in charge of schedule and appointment
* the form flow will guide to the user ask him information as the office, service, date and hour
* Version 1.0 - Sept 20, 2017 - Initial version
* Version 2.0 - November 11, 2017 - Bot for Get inline and manage appointments
*/
using BarclayBankBot.Models;
using BarclayBankBot.Services;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.FormFlow.Advanced;
using OTempus.Library.Class;
using OTempus.Library.Result;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BarclayBankBot.Forms
{
    public enum DayPeriod
    {
        Morning,
        Noon,
        Afternoon,
    }

    [Serializable]
    public class BookFormApp
    {

        #region Properties
        // The prompt tool allow us create a customized message for send to the user in the form flow

        // This property is for save the office

        [Prompt("Please select the office: {||} ")]
        public string Office;
        // This property will keep the service of the office

        [Prompt("Please select the service: {||} ")]
        public string Service;
        [Prompt("Please select the date: {||} ")]
        public string Date;
        //This is the hour of the selected slot

        [Prompt("Please select an option: {||} ")]
        public DayPeriod? dayPeriod;
        [Prompt("Please select your preferred time: {||} ")]
        public string Hour;
       
        #region public properties that you will not see in the flow
        //This properties are used for keep values, but you will not see in the form flow
        public string CalendarId;
        public string OrdinalSlot;
        /// <summary>
        /// This property allow us get or set the context data from an specific Dialog 
        /// </summary>
        public static IDialogContext context { get; set; }
        #endregion

        #endregion

        #region Methods

        /// <summary>
        /// Get a list of the actual services, optional we can filtee by the unit selected
        /// </summary>
        /// <returns></returns>
        /// 
        static List<OTempus.Library.Class.Service> GetServicesOtempues(int unitId)
        {
            Service _Service = new Service();
            _Service.Name = "No services to select, write 'quit' for continue...";
            List<Service> listServices = new List<OTempus.Library.Class.Service>();
            listServices = AppoinmentService.GetListServices(unitId);
            if (listServices.Count > 0)
            {
                return AppoinmentService.GetListServices(unitId);
            }
            else
            {
                listServices.Add(_Service);
                return listServices;
            }
        }
        /// <summary>
        /// Get a list of the actual units configured
        /// </summary>
        /// <returns></returns>
        static List<OTempus.Library.Class.Unit> GetListUnitsConfigured()
        {
            return AppoinmentService.GetListUnitsConfigured();
        }
        /// <summary>
        /// Get calendars by service id and an specific date
        /// </summary>
        /// <param name="serviceId"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        static List<OTempus.Library.Class.Calendar> GetCalendar(string serviceId, DateTime date)
        {
            try
            {
                return AppoinmentService.GetCalendars(serviceId, date);
            }
            catch (Exception ex)
            {
                throw new Exception("error in GetCalendar: " + ex.Message);
            }
        }

        #endregion

        #region Creation of IForm

        /// <summary>
        /// Create a IForm(Form flow) based in BookFormApp object
        /// </summary>
        /// <returns></returns>
        /// 
        public static IForm<BookFormApp> BuildForm()
        {
            #region processOrder
            OnCompletionAsyncDelegate<BookFormApp> processOrder = async (context, state) =>
            {
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
                try
                {
                    ResultObjectBase resultObjectBase = new ResultObjectBase();
                    int serviceID = 0;
                    int unitId = 0;
                    unitId = Utilities.Util.GetUnitIdFromBotOption(state.Office);
                    string ordinalNumber = state.Hour.ToString();

                    try
                    {
                        List<Service> listService = AppoinmentService.listServicesByName(state.Service, 1, "en", false, unitId);
                        serviceID = listService[0].Id;
                    }
                    catch (Exception)
                    {
                        throw new Exception("I don't found appointments with the service: " + state.Service);
                    }

                    try
                    {

                        resultObjectBase = AppoinmentService.SetAppoinment(0, serviceID, customerState.CustomerId, Convert.ToInt32(ordinalNumber), Convert.ToInt32(
                        state.CalendarId));
                        //Get the appoinment by appoinment id
                        AppointmentGetResults _appointment = AppoinmentService.GetAppointmentById(resultObjectBase.Id);
                        await context.PostAsync("Your appointment has been scheduled, Ticket: " +_appointment.QCode+_appointment.QNumber);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("I can't book the appointment, error: " + ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    await context.PostAsync($"Failed with message: {ex.Message}");
                }
            };
            #endregion
            #region Define language and FormBuilder
            CultureInfo ci = new CultureInfo("en");
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
            FormBuilder<BookFormApp> form = new FormBuilder<BookFormApp>();
            return form
            #endregion
            #region office
           .Field(new FieldReflector<BookFormApp>(nameof(Office))
           .SetType(null)
           .SetDefine(async (state, field) =>
           {
               List<Unit> list = GetListUnitsConfigured();
               string data = string.Empty;
               if (list.Count > 0)
               {
                   foreach (var unit in list)
                   {
                       //This format is important, follow this structure
                       data = unit.Id + "." + unit.Name;
                       field
                           .AddDescription(data, unit.Name)
                           .AddTerms(data, unit.Name);
                   }
               
                   return await Task.FromResult(true);
               }
               else
               {
                   await context.PostAsync($"No records found");
                   throw new Exception("No records found");
               }
           }))
            #endregion
            #region service
          .Field(new FieldReflector<BookFormApp>(nameof(Service))
          .SetType(null)
          .SetDefine((state, field) =>
          {
              int unitId = 0;
              if (!String.IsNullOrEmpty(state.Office))
              {
                  //Get the unit id by the option above selected
                  unitId = Utilities.Util.GetUnitIdFromBotOption(state.Office);
                  if (GetServicesOtempues(unitId).Count > 0)
                  {
                      foreach (var prod in GetServicesOtempues(unitId))
                          field
                                                  .AddDescription(prod.Name, prod.Name)
                                                  .AddTerms(prod.Name, prod.Name);
               
                      return Task.FromResult(true);
                  }
                  else
                  {
                      throw new Exception("Not exists services");
                  }
               }
               return Task.FromResult(true);
             }))
            #endregion
            #region Date
          .Field(new FieldReflector<BookFormApp>(nameof(Date))
          .SetType(null)
          .SetDefine(async (state, field) =>
          {
              List<Service> listService;
              int unitId = 0;
              List<OTempus.Library.Class.Calendar> listCalendars;
              StringBuilder response = new StringBuilder();
              List<CalendarGetSlotsResults> listGetAvailablesSlots = new List<CalendarGetSlotsResults>();
              if (!String.IsNullOrEmpty(state.Service) && !String.IsNullOrEmpty(state.Office))
              {
                  unitId = Utilities.Util.GetUnitIdFromBotOption(state.Office);
                  try
                  {
                      listService = AppoinmentService.listServicesByName(state.Service, 1, "en", false, unitId);
                      int serviceID = listService[0].Id;
                      listCalendars = new List<OTempus.Library.Class.Calendar>();
                      if (GetServicesOtempues(unitId).Count > 0)
                      {
                          listCalendars = GetCalendar(serviceID.ToString(), DateTime.Today);

                          if (listCalendars.Count == 0)
                          {
                              await context.PostAsync($"No records found");

                              throw new Exception("No records found");
                          }
                          else
                          {
                              foreach (var calendar in listCalendars)
                              {
                                  string data = calendar.Id + ".-" + calendar.CalendarDate.ToString();
                                  string data1 = Utilities.Util.GetDateWithOutTime(calendar.CalendarDate);//we see this in form flow
                                  field
                                    .AddDescription(data, data1)
                                    .AddTerms(data, data1);
                              }
                       
                              return await Task.FromResult(true);
                          }//End else 
                      }//End if
                      else{
                          await context.PostAsync($"No records found");

                          throw new Exception("No records found");
                      }
                  }//End try
                  catch (Exception e) { throw e; }
              }
              return await Task.FromResult(true);
          }))
            #endregion
            #region calendarId
           .Field(new FieldReflector<BookFormApp>(nameof(CalendarId)).SetActive(InactiveField))
            #endregion
            #region ordinal Slot
           .Field(new FieldReflector<BookFormApp>(nameof(OrdinalSlot)).SetActive(InactiveField))
            #endregion
            #region Period day
           .Field(nameof(dayPeriod))
            #endregion
            #region hour
           .Field(new FieldReflector<BookFormApp>(nameof(Hour))
           .SetType(null)
           .SetDefine(async (state, value) =>
           {
               string date;
               string service;
               List<Service> listService;
               List<OTempus.Library.Class.Calendar> listCalendars;
               listCalendars = new List<OTempus.Library.Class.Calendar>();
               List<CalendarSlot> listGetAvailablesSlots;
               if (!String.IsNullOrEmpty(state.Date) && !String.IsNullOrEmpty(state.Service) && !String.IsNullOrEmpty(state.Office) && !String.IsNullOrEmpty(state.dayPeriod.ToString()))
               {
                   int periodDay = Convert.ToInt32(Utilities.Util.GetIntPeriodDay(state.dayPeriod.ToString()));
                   int unitId = Utilities.Util.GetUnitIdFromBotOption(state.Office);
                   string calendarId = Utilities.Util.GetCalendarIdFromBotOption(state.Date);
                   //assign the calendar id
                   state.CalendarId = calendarId;
                   try
                   {
                       listService = AppoinmentService.listServicesByName(state.Service, 1, "en", false, unitId);
                       if (listService.Count > 0)
                       {
                           //It find the time one record, but in the case this found two, 
                           int serviceID = listService[0].Id;

                           date = state.Date; service = state.Service;
                           if (GetServicesOtempues(unitId).Count > 0)
                           {
                               //Service two and date...
                               listCalendars = GetCalendar(serviceID.ToString(), DateTime.Today);
                           }
                           else {
                               await context.PostAsync($"No records found");
                               throw new Exception("No records found");
                           }
                           listGetAvailablesSlots = new List<CalendarSlot>();

                           StringBuilder response = new StringBuilder();
                           response.Append("Not exists slots").ToString();
                       }
                       else{
                           await context.PostAsync($"No records found");
                           throw new Exception("No records found");
                       }
                   }
                   catch (Exception ex)
                   {
                       throw new Exception("Here are the error: " + ex.Message);
                   }
                   date = state.Date.ToString();
                   service = state.Service.ToString();
                   if (listCalendars.Count > 0)
                   {
                       listGetAvailablesSlots = AppoinmentService.GetSlotsByPeriod(Convert.ToInt32(calendarId), periodDay.ToString(), 0.ToString());
                       int cont = 0;
                       if (listGetAvailablesSlots.Count > 0)
                       {
                           foreach (OTempus.Library.Class.CalendarSlot calendarSlots in listGetAvailablesSlots)
                           {
                               //string hour = Utilities.Util.GetHourFromStartDate(calendarSlots.DisplayStartTime.ToString()); ;
                               if (calendarSlots.Status.ToString() == "Vacant")
                               {
                                   string data = calendarSlots.OrdinalNumber.ToString();
                                   DateTime date1 = DateTime.Today;
                                   date1 = date1.AddMinutes(calendarSlots.StartTime);
                                   string data1 = string.Format("{0:hh:mm-tt}", date1);
                                   value
                                  .AddDescription(data, data1)
                                  .AddTerms(data, data1);
                                   cont++;
                               }
                           }
                           return await Task.FromResult(true);
                       }
                       else{
                           await context.PostAsync($"No records found");
                           throw new Exception("No records found");
                       }
                   }
                   else
                   {
                       throw new Exception("No records found");
                   }
               }
               return await Task.FromResult(false);
           }))
            #endregion
            #region Confirm commented
            /* .Confirm("Are you selected the information: " +
            "\n* Office: {Office} " +
            "\n* Slot:  {Hour} " +
            "\n* Service:  {Service}? \n" +
            "(yes/no)") This lines are commented because, when the user select no,  the user can create inconsistence when user book the appointment (I try to solve by the best way)*/
            #endregion
            .AddRemainingFields()
            .OnCompletion(processOrder)
            .Build();
        }
        /// <summary>
        /// This method is inside a delegate that inactive the specific field when form flow is running, through the bool property
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private static bool InactiveField(BookFormApp state)
        {
            bool setActive = false;
            return setActive;
        }
        #endregion
    };
}