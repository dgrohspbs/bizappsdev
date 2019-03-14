using ClientFacingBot.Common;
using ClientFacingBot.Model;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Http;

namespace ClientFacingBot.Dialogs
{
    [Serializable]
    [BotAuthentication]
    [Authorize]
    public class RootDialog : IDialog<object>
    {
        public static CaseDetail caseDetail = new CaseDetail();
        public static Contact contact = new Contact();
        public static JObject channelData = null;

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        private static async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;
            var channelData = (JObject)activity.ChannelData;

            if (channelData != null && channelData["id"] != null)
            {
                contact.ContactId = (string)channelData["id"];
            }

            if (activity.Text.ToLower().Contains("hi") || activity.Text.ToLower().Contains("hello"))
            {
                if (contact.ContactId != null)
                {
                    contact = await Utilities.GetContactDetails(context, contact.ContactId, string.Empty);

                    HttpResponseMessage categoryResponse = await Utilities.CRMWebAPIRequest("api/data/v9.0/categories?$select=categoryid,categorynumber,title,a1a_categorybotquestion",
                            null, "retrieve");
                    if (categoryResponse.IsSuccessStatusCode)
                    {
                        string categoryResponseStr = categoryResponse.Content.ReadAsStringAsync().Result;
                        JObject categoryResults = JObject.Parse(categoryResponseStr);
                        caseDetail.categoryItemResults = (JArray)categoryResults["value"];

                        if (categoryResults.Count > 0)
                        {
                            await context.PostAsync($"Hi {contact.Fullname}! " + Utilities.RandomPhrase(Common.Dialogs.introAuthenticatedDialog));

                            string intro = string.Empty + Utilities.RandomPhrase(Common.Dialogs.categoryDialog) + " ";
                            int counter = 1;

                            foreach (var item in caseDetail.categoryItemResults)
                            {
                                intro += Environment.NewLine + counter + " - " + (string)item["title"];
                                counter++;
                            }

                            await Utilities.CreateReply(GetCategoryAsync, context, activity, intro);
                        }
                    }
                }
                else
                {
                    await context.PostAsync(Utilities.RandomPhrase(Common.Dialogs.introAnonymousDialog));
                    context.Wait(GetEmailAddress);
                }
            }
        }

        public static async Task GetCategoryAsync(IDialogContext context, IAwaitable<object> argument)
        {
            var activity = await argument as Activity;
            int categoryValue = 0;
            if (Int32.TryParse(activity.Text, out categoryValue))
            {
                caseDetail.categoryName = (string)caseDetail.categoryItemResults[Convert.ToInt32(categoryValue) - 1]["title"];
                var category = caseDetail.categoryItemResults.Where(t => (string)t["title"] == caseDetail.categoryName).Select(t => t).FirstOrDefault();

                caseDetail.categoryCode = category["categorynumber"].ToString();
                caseDetail.categoryId = category["categoryid"].ToString();
                caseDetail.categoryQuestion = category["a1a_categorybotquestion"].ToString();

                await Utilities.CreateReply(GetProductAsync, context, activity, Utilities.RandomPhrase(Common.Dialogs.printerDialog));
            }
            else
            {
                caseDetail.categoryName = caseDetail.categoryItemResults.Where(t => t["title"].ToString().ToLower().Contains(activity.Text)).Select(t => (string)t["title"]).FirstOrDefault();

                if (caseDetail.categoryName != string.Empty || caseDetail.categoryName != null)
                {
                    var category = caseDetail.categoryItemResults.Where(t => (string)t["title"] == caseDetail.categoryName).Select(t => t).FirstOrDefault();

                    if (category != null)
                    {
                        caseDetail.categoryCode = category["categorynumber"].ToString();
                        caseDetail.categoryId = category["categoryid"].ToString();
                        caseDetail.categoryQuestion = category["a1a_categorybotquestion"].ToString();

                        await Utilities.CreateReply(GetProductAsync, context, activity, Utilities.RandomPhrase(Common.Dialogs.printerDialog));
                    }
                    else
                    {
                        string intro = $"Sorry I cannot understand that. How can i help you?";
                        int counter = 1;

                        foreach (var item in caseDetail.categoryItemResults)
                        {
                            intro += Environment.NewLine + counter + " - " + (string)item["title"];
                            counter++;
                        }

                        await Utilities.CreateReply(GetCategoryAsync, context, activity, intro);
                    }
                }
                else
                {
                    string intro = $"Sorry I cannot understand that. How can i help you?";
                    int counter = 1;

                    foreach (var item in caseDetail.categoryItemResults)
                    {
                        intro += Environment.NewLine + counter + " - " + (string)item["title"];
                        counter++;
                    }

                    await Utilities.CreateReply(GetCategoryAsync, context, activity, intro);
                }
            }
        }

        public static async Task GetProductAsync(IDialogContext context, IAwaitable<object> argument)
        {
            var activity = await argument as Activity;

            caseDetail.productNumber = activity.Text;

            HttpResponseMessage productResponse = await Utilities.CRMWebAPIRequest("/api/data/v9.0/products?$select=name,productid,productnumber&$filter=productnumber eq '" + caseDetail.productNumber + "'",
                                null, "retrieve");

            if (productResponse.IsSuccessStatusCode)
            {
                string productResponseStr = productResponse.Content.ReadAsStringAsync().Result;
                JObject productResults = JObject.Parse(productResponseStr);
                JArray productItemResults = (JArray)productResults["value"];

                if (productItemResults.Count > 0)
                {
                    caseDetail.productId = new Guid(productItemResults[0]["productid"].ToString());
                    caseDetail.productName = productItemResults[0]["name"].ToString();

                    if (caseDetail.categoryQuestion != null || caseDetail.categoryQuestion != string.Empty)
                    {
                        await Utilities.CreateReply(GetKBAsync, context, activity, $"I understand that you have a concern about {caseDetail.categoryName} regarding your {caseDetail.productName}. " + caseDetail.categoryQuestion);
                    }
                    else
                    {
                        await Utilities.CreateReply(GetKBAsync, context, activity, "Can you provide information about your concern?");
                    }
                }
                else
                {
                    await Utilities.CreateReply(GetProductAsync, context, activity, "I’m sorry I can’t find that printer model. Can you enter the correct printer model?");
                }
            }
            else
            {
                await Utilities.CreateReply(GetProductAsync, context, activity, "I’m sorry I can’t find that printer model. Can you enter the correct printer model?");
            }

        }

        public static async Task GetKBAsync(IDialogContext context, IAwaitable<object> argument)
        {
            var activity = await argument as Activity;

            caseDetail.problemString = activity.Text;

            HttpResponseMessage kbResponse = await Utilities.CRMWebAPIRequest("/api/data/v9.0/knowledgearticles?fetchXml=%3Cfetch%20version%3D%221.0%22%20output-format%3D%22xml-platform%22%20mapping%3D%22logical%22%20distinct%3D%22true%22%3E%3Centity%20name%3D%22knowledgearticle%22%3E%3Cattribute%20name%3D%22articlepublicnumber%22%20%2F%3E%3Cattribute%20name%3D%22knowledgearticleid%22%20%2F%3E%3C" +
                "attribute%20name%3D%22title%22%20%2F%3E%3Cfilter%20type%3D%22and%22%3E%3Ccondition%20attribute%3D%22isrootarticle%22%20operator%3D%22eq%22%20value%3D%220%22%20%2F%3E%3C%2Ffilter%3E%3Clink-entity%20name%3D%22connection%22%20from%3D%22record2id%22%20to%3D%22knowledgearticleid%22%20" +
                "link-type%3D%22inner%22%20alias%3D%22at%22%3E%3Clink-entity%20name%3D%22product%22%20from%3D%22productid%22%20to%3D%22record1id%22%20link-type%3D%22inner%22%20alias%3D%22au%22%3E%3Cfilter%20type%3D%22and%22%3E%3Ccondition%20attribute%3D%22productnumber%22%20operator%3D%22eq%22%20value%3D%22" +
                caseDetail.productNumber + "%22%20%2F%3E%3C%2Ffilter%3E%3C%2Flink-entity%3E%3C%2Flink-entity%3E%3Clink-entity%20name%3D%22knowledgearticlescategories%22%20from%3D%22knowledgearticleid%22%20to%3D%22knowledgearticleid%22%20visible%3D%22false%22%20intersect%3D%22true%22%3E%3Clink-entity%20name%3D%22category%22%20from%3D%22" +
                "categoryid%22%20to%3D%22categoryid%22%20alias%3D%22av%22%3E%3Cfilter%20type%3D%22and%22%3E%3Ccondition%20attribute%3D%22categorynumber%22%20operator%3D%22eq%22%20value%3D%22" + caseDetail.categoryCode + "%22%20%2F%3E%3C%2Ffilter%3E%3C%2Flink-entity%3E%3C%2Flink-entity%3E%3C%2Fentity%3E%3C%2Ffetch%3E"
                , null, "retrieve");

            if (kbResponse.IsSuccessStatusCode)
            {
                string myString = kbResponse.Content.ReadAsStringAsync().Result;
                JObject kbResults = JObject.Parse(myString);
                caseDetail.kbItemResults = (JArray)kbResults["value"];
                caseDetail.kbItemResultsIndex = 0;

                if (caseDetail.kbItemResults.Count > 0)
                {
                    IMessageActivity msgMarkdown = context.MakeMessage();
                    msgMarkdown.Text = Utilities.RandomPhrase(Common.Dialogs.foundArticleDialog) + " ";
                    msgMarkdown.Text += WebConfigurationManager.AppSettings["PortalUrl"] + "/knowledgebase/article/" + (string)caseDetail.kbItemResults[caseDetail.kbItemResultsIndex]["articlepublicnumber"];

                    await context.PostAsync(msgMarkdown);

                    await Utilities.CreateReply(ShowNextKBAsync, context, activity, Utilities.RandomPhrase(Common.Dialogs.articleDialog));
                }
                else
                {
                    await Utilities.SubmitCaseAsync(context, caseDetail, contact);
                }
            }
            else
            {
                context.Wait(GetKBAsync);
            }
        }

        public static async Task ShowNextKBAsync(IDialogContext context, IAwaitable<object> argument)
        {
            var activity = await argument as Activity;

            if (activity.Text.ToLower().Contains("no"))
            {
                caseDetail.kbItemResultsIndex++;

                if (caseDetail.kbItemResultsIndex <= caseDetail.kbItemResults.Count - 1)
                {
                    IMessageActivity msgMarkdown = context.MakeMessage();
                    msgMarkdown.Text = msgMarkdown.Text = Utilities.RandomPhrase(Common.Dialogs.foundArticleDialog) + " ";
                    msgMarkdown.Text += WebConfigurationManager.AppSettings["PortalUrl"] + "/knowledgebase/article/" + (string)caseDetail.kbItemResults[caseDetail.kbItemResultsIndex]["articlepublicnumber"];
                    caseDetail.kbItemResultsIndex++;
                    await context.PostAsync(msgMarkdown);

                    await Utilities.CreateReply(ShowNextKBAsync, context, activity, Utilities.RandomPhrase(Common.Dialogs.articleDialog));
                }
                else
                {
                    await Utilities.SubmitCaseAsync(context, caseDetail, contact);
                }
            }
            else if (activity.Text.ToLower().Contains("yes"))
            {
                await Utilities.SubmitCaseDeflection(context, caseDetail, contact);
            }
        }

        public static async Task GetEmailAddress(IDialogContext context, IAwaitable<object> argument)
        {
            var activity = await argument as Activity;
            contact = await Utilities.GetContactDetails(context, null, activity.Text);

            HttpResponseMessage categoryResponse = await Utilities.CRMWebAPIRequest("api/data/v9.0/categories?$select=categoryid,categorynumber,title,a1a_categorybotquestion",
                            null, "retrieve");
            if (categoryResponse.IsSuccessStatusCode)
            {
                string categoryResponseStr = categoryResponse.Content.ReadAsStringAsync().Result;
                JObject categoryResults = JObject.Parse(categoryResponseStr);
                caseDetail.categoryItemResults = (JArray)categoryResults["value"];

                if (categoryResults.Count > 0)
                {
                    await context.PostAsync($"Hi {contact.Fullname}! " + Utilities.RandomPhrase(Common.Dialogs.introAuthenticatedDialog));

                    string intro = string.Empty + Utilities.RandomPhrase(Common.Dialogs.categoryDialog) + " ";
                    int counter = 1;

                    foreach (var item in caseDetail.categoryItemResults)
                    {
                        intro += Environment.NewLine + counter + " - " + (string)item["title"];
                        counter++;
                    }

                    await Utilities.CreateReply(GetCategoryAsync, context, activity, intro);
                }
            }
        }

    }
}