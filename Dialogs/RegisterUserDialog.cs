﻿using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using BarclayBankBot.Forms;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.FormFlow;
using System.Threading;

namespace BarclayBankBot.Dialogs
{
    [Serializable]
    public class RegisterUserDialog : IDialog<object>
    {
        byte[] arrayImage = null;
        int savedImageId = 0;
        string imageName =string.Empty;
        private const string LogOut = "Exit";
        private const string Register = "Create new User";
        public RegisterUserDialog(int _savedImageId, string _imageName)
        {
            this.savedImageId = _savedImageId;
            this.imageName = _imageName;
        }
        public async Task StartAsync(IDialogContext context)
        {
            await context.PostAsync($"Sorry, I don’t recognize you and I need to know some details about you");
            // context.Call(saveFormDialog, ResumeAfterSaveCustomerDialog);
            context.Wait(this.MessageReceivedAsync);
        }
        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            SaveCustomerFormApp.imageName = imageName;
            SaveCustomerFormApp.savedImageId = savedImageId;
            SaveCustomerFormApp.context = context;
            var saveFormDialog = FormDialog.FromForm(SaveCustomerFormApp.BuildForm, FormOptions.PromptInStart);
            context.Call(saveFormDialog, ResumeAfterSaveCustomerDialog);
        }
        public virtual async Task MessageReceivedAsyncRedirectToRootDialog(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;
            context.Call(new RootDialog(), ResumeAfterRootDialog);
        }
        private async Task ResumeAfterRootDialog(IDialogContext context, IAwaitable<object> result)
        {
            context.Done(string.Empty);
        }
        private async Task AfterChoiceSelected(IDialogContext context, IAwaitable<string> result)
        {
            var selection = await result;
            switch (selection)
            {
                case Register:
                    SaveCustomerFormApp.imageName = imageName;
                    SaveCustomerFormApp.savedImageId = savedImageId;
                    SaveCustomerFormApp.context = context;
                    var saveFormDialog = FormDialog.FromForm(SaveCustomerFormApp.BuildForm, FormOptions.PromptInStart);
                    context.Call(saveFormDialog, ResumeAfterSaveCustomerDialog);
                    break;
                case LogOut:
                    context.Done(string.Empty);
                    break;
            }
        }
        private async Task ResumeAfterSaveCustomerDialog(IDialogContext context, IAwaitable<SaveCustomerFormApp> result)
        {
            SaveCustomerFormApp customer = await result;
            RootDialog rd = new RootDialog();
            await context.PostAsync($"Welcome " +customer.name +" "+customer.LastName);
            bool creatingUser = true;
            context.UserData.SetValue<bool>("creatingUser", creatingUser);
            context.Call(new RootDialog(), ResumeAfterRootDialog);
        }
    }
}