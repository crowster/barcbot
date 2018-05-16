﻿/*
* Author: Jesús González Martínez
* Date created: November 16, 2017
* Description: This class is a implements an cancel form flow, and this is in charge of cancel and appointment
* the form flow will guide to the user , at first the user could select one of the availables appointments, in the case
* of have one. Then throw a confirmation message fo cancel or no, we can go out of the form flow tipyng quit 
* Version 1.0 - Sept 20, 2017 - Initial version
* Version 2.0 - November 11, 2017 - Bot for Get inline and manage appointments
*/

using BarclayBankBot.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.FormFlow.Advanced;
using OTempus.Library.Class;
using OTempus.Library.Result;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BarclayBankBot.Forms
{
    public class CancelFormAppReservation
    {
        public static IForm<ReservationCancel> BuildForm()
        {
            
            //Instance of library for manage appointments
            WebAppoinmentsClientLibrary.Appoinments appointmentLibrary = new WebAppoinmentsClientLibrary.Appoinments(); 
            OnCompletionAsyncDelegate<ReservationCancel> processOrder = async (context, state) =>
            {

                //Then when we have our object , we can cancel the appointment because we have his process id
                if (Convert.ToInt32(state.processId) > 0)
                {
                    ResultObjectBase result = appointmentLibrary.CancelAppoinment(Convert.ToInt32(state.processId),
                         0, 0, 0, "notes", false, 0, 0
                         );
                    if (result.ReturnCode > 0)
                    {
                    }
                    else
                        await context.PostAsync($"Your appointment was cancelled!");
                }
                // in other hand we can't find the record, so we will send the appropiate message
                else
                {
                    await context.PostAsync($"I don't found a record with appointment Id: \n*" + state.processId);
                }
            };
            CultureInfo ci = new CultureInfo("en");
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
            var culture = Thread.CurrentThread.CurrentUICulture;
            var form = new FormBuilder<ReservationCancel>();
            var yesTerms = form.Configuration.Yes.ToList();
            var noTerms = form.Configuration.No.ToList();
            yesTerms.Add("Yes");
            noTerms.Add("No");
            form.Configuration.Yes = yesTerms.ToArray();

            return form        //.Message("Select one of the dates availables for cancel the appointment, please")
                              .Field(new FieldReflector<ReservationCancel>(nameof(ReservationCancel.processId)) 
                              .SetType(null)
                              .SetDefine(async (state, field) =>
                              {

                                  //Get the actual user state of the customer
                                  ACFCustomer customerState = new ACFCustomer(); 

                                  int customerIdState = 0;
                                  customerIdState = state._customerId;
                                  string personalIdState = string.Empty;
                                  //personalIdState = customerState.PersonaId;
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
                                      return await Task.FromResult(false);
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
                                              // string data = appointment.AppointmentDate.ToString();
                                              string data = string.Format("{0:dd/MM/yyyy-hh:mmtt}", appointment.AppointmentDate);
                                              field
                                             .AddDescription(listCustomer.ProcessId.ToString(), data + "|" + listCustomer.ServiceName + " in " + listCustomer.UnitName)//here we put the process id and the date of the appointment of this process
                                             .AddTerms(listCustomer.ProcessId.ToString(), data + "|" + listCustomer.ServiceName + " in " + listCustomer.UnitName);
                                          }
                                          return await Task.FromResult(true);
                                      }
                                      else
                                      {
                                          await ReservationCancel.context.PostAsync($"No records found");
                                      }
                                      return await Task.FromResult(true);
                                  }

                              }))
                                   .Field(new FieldReflector<ReservationCancel>(nameof(ReservationCancel._customerId)).SetActive(InactiveField))
                              .Confirm("Are you sure you want to cancel the appointment?  " +
                              "\n* (yes/no) ")
                              .AddRemainingFields()
                              .OnCompletion(processOrder)
                              .Build();
        }
        /// <summary>
        /// This method is inside a delegate that inactive the specific field when form flow is running, through the bool property
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private static bool InactiveField(ReservationCancel state)
        {
            bool setActive = false;
            return setActive;
        }
    };
}
