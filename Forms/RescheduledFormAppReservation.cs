/*
* Author: Jesús González Martínez
* Date created: November 16, 2017
* Description: This class is a implements an reschedule form flow, and this is in charge of reschedule and appointment
* the form flow will guide to the user , at first the user could select one of the availables appointments, in the case
* of have one. Then the bot will show the availables dates and hour for reschedule the appointment
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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BarclayBankBot.Forms
{
    public class RescheduledFormAppReservation
    {
        #region Methods
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
                throw new Exception("error in Get Calendar: " + ex.Message);
            }
        }
        #endregion

        #region Creation of IForm
        /// <summary>
        /// This method create an Iform (form flow) for reschedule an appointement
        /// </summary>
        /// <returns></returns>
        public static IForm<ReservationReschedule> BuildForm()
        {
            //Instance of library for manage appoinments
            WebAppoinmentsClientLibrary.Appoinments appointmentLibrary = new WebAppoinmentsClientLibrary.Appoinments();
            #region On complete, process Order
            OnCompletionAsyncDelegate<ReservationReschedule> processOrder = async (context, state) =>
            {
                try
                {
                    Char delimiter = '.';
                    string[] arrayInformation = state.processIdServiceId.Split(delimiter);
                    int processId = Convert.ToInt32(arrayInformation[0]);
                    int serviceId = Convert.ToInt32(arrayInformation[1]);

                    string[] dateInformation = state.Date.Split(delimiter);
                    string stringDate = dateInformation[1];
                    //stringDate = stringDate.Replace("-", " ");
                    stringDate = Utilities.Util.GetDateWithOutTime(stringDate);
                    //Here I create the new date
                    string newDate = stringDate + " " + state.Hour;
                    string newDat2 = Utilities.Util.GetDateWithCorrectPositionOfTheMonth(newDate);
                    int result = 0;
                    try
                    {
                        result = AppoinmentService.RescheduleAppoinment(processId, newDat2, serviceId);
                    }
                    catch (Exception)
                    {
                        result = AppoinmentService.RescheduleAppoinment(processId, newDate, serviceId);
                    }
                    AppointmentGetResults _appointment = AppoinmentService.GetAppointmentById(result);
                    await context.PostAsync($"The appointment was rescheduled, Ticket: " + _appointment.QCode + _appointment.QNumber);
                }
                catch (Exception ex)
                {
                    await context.PostAsync(ex.Message.ToString());
                }
            };
            #endregion
            #region set language and create a container for form builder
            CultureInfo ci = new CultureInfo("en");
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
            var culture = Thread.CurrentThread.CurrentUICulture;
            var form = new FormBuilder<ReservationReschedule>();
            var yesTerms = form.Configuration.Yes.ToList();
            var noTerms = form.Configuration.No.ToList();
            yesTerms.Add("Yes");
            noTerms.Add("No");
            form.Configuration.Yes = yesTerms.ToArray();
            return form
            #endregion
            #region process and service ids
                           .Field(new FieldReflector<ReservationReschedule>(nameof(ReservationReschedule.processIdServiceId))
                           .SetType(null)
                           .SetDefine(async (state, field) =>
                           {
                               //Get the actual user state of the customer
                               ACFCustomer customerState = new ACFCustomer();
                               int customerIdState = 0;
                               customerIdState = state._customerId;
                               string personalIdState = string.Empty;
                               //Instance of library for manage customers
                               WebAppoinmentsClientLibrary.Customers customerLibrary = new WebAppoinmentsClientLibrary.Customers();
                               //Instance of library for manage cases
                               WebAppoinmentsClientLibrary.Cases caseLibrary = new WebAppoinmentsClientLibrary.Cases();
                               //Here we will to find the customer by customer id or personal id
                               Customer customer = null;
                               if (!string.IsNullOrEmpty(customerIdState.ToString()))
                               {
                                   //Get the object ObjectCustomer and inside of this the object Customer
                                   try
                                   {
                                       customer = customerLibrary.GetCustomer(customerIdState).Customer;
                                   }
                                   catch (Exception)
                                   {
                                       // throw; here we not send the exception beacuse we need to do the next method below 
                                   }
                               }
                               //If not found by customer id , we will try to find by personal id
                               else
                               {
                                   int idType = 0;
                                   //Get the object ObjectCustomer and inside of this the object Customer
                                   try
                                   {
                                       customer = customerLibrary.GetCustomerByPersonalId(personalIdState, idType).Customer;
                                   }
                                   catch (Exception)
                                   {
                                       //throw;
                                   }
                               }
                               if (customer == null)
                               {
                                   throw new Exception("No records found");
                               }
                               else
                               {
                                   //Declaration of Calendar Get Slots Results object
                                   CalendarGetSlotsResults slotToShowInformation = new CalendarGetSlotsResults();
                                   //Set the parameters for get the expected appoinments
                                   int customerTypeId = 0;
                                   string customerTypeName = "";
                                   int customerId = customer.Id;
                                   DateTime startDate = DateTime.Today;
                                   //here we add ten days to the startdate
                                   DateTime endDate = startDate.AddDays(10);
                                   string fromDate = startDate.ToString();
                                   string toDate = endDate.ToString();
                                   string typeSeriaizer = "XML";
                                   //Declaration of the ocject to save the result of the GetExpectedAppoinment
                                   ObjectCustomerGetExpectedAppointmentsResults objectCustomerGetExpectedAppointmentsResults = new ObjectCustomerGetExpectedAppointmentsResults();
                                   objectCustomerGetExpectedAppointmentsResults = customerLibrary.GetExpectedAppoinment(customerTypeId, customerTypeName, customerId, startDate, endDate, typeSeriaizer);
                                   if (objectCustomerGetExpectedAppointmentsResults.ListCustomerGetExpectedAppointmentsResults.Count > 0)
                                   {
                                       foreach (CustomerGetExpectedAppointmentsResults listCustomer in objectCustomerGetExpectedAppointmentsResults.ListCustomerGetExpectedAppointmentsResults)
                                       {
                                           //At first I need to find the appoinment by appoiment id, for saw the actual status
                                           Appointment appointment = appointmentLibrary.GetAppoinment(listCustomer.AppointmentId).AppointmentInformation;
                                           string data = appointment.AppointmentDate.ToString();
                                           string processIdAndServiceId = appointment.ProcessId + "." + appointment.ServiceId + "." + Utilities.Util.GetDateWithOutTime(data);
                                           field
                                          .AddDescription(processIdAndServiceId, data + " | " + listCustomer.ServiceName)//here we put the process id and the date of the appointment of this process
                                          .AddTerms(processIdAndServiceId, data + " | " + listCustomer.ServiceName);
                                       }
                                       return await Task.FromResult(true);
                                   }
                                   else
                                   {
                                       throw new Exception("No records found");
                                   }
                               }
                           }))
            #endregion
            #region Date
            .Field(new FieldReflector<ReservationReschedule>(nameof(ReservationReschedule.Date))
            .SetType(null)
            .SetDefine(async (state, field) =>
            {
                List<OTempus.Library.Class.Calendar> listCalendars;
                StringBuilder response = new StringBuilder();
                List<CalendarGetSlotsResults> listGetAvailablesSlots = new List<CalendarGetSlotsResults>();
                if (!String.IsNullOrEmpty(state.processIdServiceId))
                {
                    Char delimiter = '.';
                    string[] arrayInformation = state.processIdServiceId.Split(delimiter);
                    int processId = Convert.ToInt32(arrayInformation[0]);
                    int serviceId = Convert.ToInt32(arrayInformation[1]);
                    string currentAppoinmentDate = arrayInformation[2];
                    DateTime dateFromString = DateTime.Parse(currentAppoinmentDate, System.Globalization.CultureInfo.CurrentCulture);
                    try
                    {
                        listCalendars = new List<OTempus.Library.Class.Calendar>();

                        listCalendars = GetCalendar(serviceId.ToString(), dateFromString);

                        if (listCalendars.Count == 0)
                        {
                            throw new Exception("No records found");

                        }
                        else
                        {
                            foreach (var calendar in listCalendars)
                            {
                                string data = calendar.Id + "." + calendar.CalendarDate.ToString();
                                string data1 = Utilities.Util.GetDateWithOutTime(calendar.CalendarDate.ToString());//we see this in form flow
                                field
                                  .AddDescription(data, data1)
                                  .AddTerms(data, data1);
                            }
                            return await Task.FromResult(true);
                        }//End else 

                    }//End try
                    catch (Exception e) { }
                }
                return await Task.FromResult(true);
            }))
            #endregion
            #region Period day
           .Field(nameof(ReservationReschedule.dayPeriod))
            #endregion
            #region hour
           .Field(new FieldReflector<ReservationReschedule>(nameof(ReservationReschedule.Hour))
           .SetType(null)
           .SetDefine(async (state, value) =>
           {
               string date;
               List<OTempus.Library.Class.Calendar> listCalendars;
               List<CalendarSlot> listGetAvailablesSlots;
               if (!String.IsNullOrEmpty(state.Date) && !String.IsNullOrEmpty(state.dayPeriod.ToString()) && !String.IsNullOrEmpty(state.processIdServiceId))
               {
                   Char delimiter = '.';
                   string[] arrayInformation = state.processIdServiceId.Split(delimiter);
                   int processId = Convert.ToInt32(arrayInformation[0]);
                   int serviceId = Convert.ToInt32(arrayInformation[1]);
                   string currentAppoinmentDate = arrayInformation[2];
                   DateTime dateFromString = DateTime.Parse(currentAppoinmentDate, System.Globalization.CultureInfo.CurrentCulture);


                   int periodDay = Convert.ToInt32(Utilities.Util.GetIntPeriodDay(state.dayPeriod.ToString()));
                   string calendarId = Utilities.Util.GetCalendarIdFromBotOption(state.Date);
                   try
                   {
                       listCalendars = new List<OTempus.Library.Class.Calendar>();
                       date = state.Date;
                       listCalendars = GetCalendar(serviceId.ToString(), dateFromString);
                       listGetAvailablesSlots = new List<CalendarSlot>();

                       StringBuilder response = new StringBuilder();
                       response.Append("Not exists slots").ToString();
                   }
                   catch (Exception ex)
                   {
                       throw new Exception("Here are the error: " + ex.Message);
                   }
                   date = state.Date.ToString();
                   if (listCalendars.Count > 0)
                   {
                       listGetAvailablesSlots = AppoinmentService.GetSlotsByPeriod(Convert.ToInt32(calendarId), periodDay.ToString(), 0.ToString());
                       if (listGetAvailablesSlots.Count > 0)
                       {

                           int cont = 0;
                           foreach (OTempus.Library.Class.CalendarSlot calendarSlots in listGetAvailablesSlots)
                           {
                               if (calendarSlots.Status.ToString() == "Vacant")
                               {
                                   string data = calendarSlots.StartTime.ToString();
                                   DateTime date1 = DateTime.Today;
                                   date1 = date1.AddMinutes(calendarSlots.StartTime);
                                    string data1 = date1.ToShortTimeString();
                                   //string data1 = string.Format("{0:hh:mm-tt}", date1);
                                   //assign the calendar id
                                   value
                                  .AddDescription(data1, data1)
                                  .AddTerms(data1, data1);
                                   cont++;
                               }
                           }
                           return await Task.FromResult(true);
                       }
                       else
                       {
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
            .Field(new FieldReflector<ReservationReschedule>(nameof(ReservationReschedule._customerId)).SetActive(InactiveField))

            #endregion
             .OnCompletion(processOrder)
             .Build();
        }
        /// <summary>
        /// This method is inside a delegate that inactive the specific field when form flow is running, through the bool property
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private static bool InactiveField(ReservationReschedule state)
        {
            bool setActive = false;
            return setActive;
        }

    };
    #endregion
}