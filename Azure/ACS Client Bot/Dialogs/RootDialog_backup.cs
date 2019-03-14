using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json.Linq;
using BotApplicationDemo.Common;
using Microsoft.Bot.Builder.FormFlow;
using System.Linq;

namespace BotApplicationDemo.Dialogs
{
    [Serializable]
    [BotAuthentication]
    [Authorize]
    public class RootDialog_Backup : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            System.Collections.Generic.List<string> startOptions = new System.Collections.Generic.List<string>()
            {
                "Information", "Submit an Issue", "Track Cases", "Sentiment Analysis"
            };

            PromptDialog.Choice(context, AfterServiceChoiceAsync, new PromptOptions<string>("Hi! How can I help you?", null, null, startOptions, 3, null));
        }

        public async Task AfterServiceChoiceAsync(IDialogContext context, IAwaitable<string> argument)
        {
            var option = await argument;
            if (option == "Information")
            {
                PromptDialog.Text(context, AfterSearchTermProvidedAsync, "What are you interested in?", null, 3);
            }
            else if (option == "Sentiment Analysis")
            {
                PromptDialog.Text(context, TextAnalysisAsync, "What do you want to analyze?", null, 3);
            }
            else if (option == "Submit an Issue")
            {
                context.Call(FormDialog.FromForm(Utilities.BuildJsonForm, FormOptions.PromptInStart), CompletedCaseSubmission);
            }
            else
            {
                await context.PostAsync("I will be able to help you with those things soon. Stay tuned.");
                context.Wait(MessageReceivedAsync);
            }

        }

        private async Task TextAnalysisAsync(IDialogContext context, IAwaitable<string> argument)
        {
            string keyword = await argument;

            await context.PostAsync("The keyword: \"" + keyword + "\" score is: " + (Utilities.TextAnalytics(context, keyword) * 100) + "%");
        }

        public async Task AfterSearchTermProvidedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            string keyword = await argument;

            HttpResponseMessage kbResponse = await Utilities.CRMWebAPIRequest("api/data/v9.0/knowledgearticles?" +
                                "$select=title,articlepublicnumber,description,content&$filter=" +
                                "contains(title, '" + keyword.ToString().ToLower() + "') and isrootarticle eq false",
                                null, "retrieve");

            if (kbResponse.IsSuccessStatusCode)
            {
                string myString = kbResponse.Content.ReadAsStringAsync().Result;
                JObject kbResults = JObject.Parse(myString);
                JArray items = (JArray)kbResults["value"];
                JObject item;

                if (items.Count > 0)
                {
                    IMessageActivity msgMarkdown = context.MakeMessage();
                    msgMarkdown.Text = $"I found {items.Count} article(s):  \n";

                    for (int i = 0; i < items.Count; i++)
                    {
                        item = (JObject)items[i];

                        msgMarkdown.Text += Environment.NewLine + "[" + (string)item["title"] + "](" + (string)item["articlepublicnumber"] + ") ";

                        string description = (string)item["contentx"];
                        if (String.IsNullOrEmpty(description) == false)
                        {
                            msgMarkdown.Text += Environment.NewLine + Utilities.StripHTML(description);
                        }

                    }

                    await context.PostAsync(msgMarkdown);

                    PromptDialog.Confirm(context, AfterSearchAgainAsync, "Would you like to search again?");
                }
                else
                {
                    PromptDialog.Confirm(context, AfterSearchAgainAsync, "I couldn't find any articles. Would you like to search again?");
                }
            }
            else
            {
                PromptDialog.Confirm(context, AfterSearchAgainAsync, "There was an error searching. Would you like to try again?");
            }
        }

        public async Task AfterSearchAgainAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var option = await argument;
            if (option == true)
            {
                PromptDialog.Text(context, AfterSearchTermProvidedAsync, "What do you want to search?", null, 3);
            }
            else
            {
                await context.PostAsync("Thanks. Let me know if I can help you again.");
                context.Wait(MessageReceivedAsync);
            }
        }

        private async Task CompletedCaseSubmission(IDialogContext context, IAwaitable<JObject> result)
        {
            Guid contactid = new Guid("514CB5A0-D79A-E811-A85A-000D3A33A9A3");

            try
            {
                var completed = await result;

                var stringContent = new StringContent("{" +
                    "\"title\":\"" + (string)completed["title"] + "\"," +
                    "\"description\":\"" + (string)completed["description"] + "\"," +
                    "\"prioritycode\":" + (string)completed["prioritycode"] + "," +
                    "\"customerid_contact@odata.bind\":\"/contacts(" + contactid.ToString() + ")\"" +
                    "}", System.Text.Encoding.UTF8, "application/json");
                HttpResponseMessage caseResponse = await Utilities.CRMWebAPIRequest("api/data/v9.0/incidents", stringContent, "create");

                if (caseResponse.IsSuccessStatusCode)
                {
                    Guid caseId = new Guid();
                    string caseDetailsRequest = "";
                    string caseUri = caseResponse.Headers.GetValues("OData-EntityId").FirstOrDefault();
                    if (caseUri != null)
                    {
                        caseId = Guid.Parse(caseUri.Split('(', ')')[1]);
                        caseDetailsRequest = "api/data/v9.0/incidents(" +
                            caseId + ")?" + "$select=ticketnumber";

                        HttpResponseMessage caseDetailsResponse = await Utilities.CRMWebAPIRequest(caseDetailsRequest, null, "retrieve");

                        if (caseDetailsResponse.IsSuccessStatusCode)
                        {
                            string myString = caseDetailsResponse.Content.ReadAsStringAsync().Result;
                            JObject caseResults =
                                JObject.Parse(caseDetailsResponse.Content.ReadAsStringAsync().Result);
                            string ticketNumber = (string)caseResults["ticketnumber"];

                            await context.PostAsync("Your issue has been submitted. Your Case Number is: __" + ticketNumber + "__ .");
                        }
                        else
                        {
                            await context.PostAsync("An error occurred while submitting your issue.");
                        }
                    }
                    else
                    {
                        await context.PostAsync("An error occurred while submitting your issue.");
                    }
                }
            }
            catch (FormCanceledException<JObject> e)
            {
                string reply;
                if (e.InnerException == null)
                {
                    reply = $"You quit on {e.Last}--maybe you can finish next time!";
                }
                else
                {
                    reply = "Sorry, I've had a short circuit.  Please try again.";
                }
                await context.PostAsync(reply);
            }

            await context.PostAsync("If you need any assistance in the future, you know where to find me.");
            context.Done<bool>(true);
        }
    }
}