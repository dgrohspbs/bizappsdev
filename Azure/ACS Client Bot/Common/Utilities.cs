using ClientFacingBot.Model;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.FormFlow.Json;
using Microsoft.Bot.Connector;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Http;

namespace ClientFacingBot.Common
{
    [Serializable]
    public class Utilities
    {
        public static string StripHTML(string HTMLText)
        {
            Regex reg = new Regex("<.*?>", RegexOptions.IgnoreCase);
            var stripped = reg.Replace(HTMLText, string.Empty);
            return stripped.Trim();
        }

        [BotAuthentication]
        [Authorize]
        public static async Task<HttpResponseMessage> CRMWebAPIRequest(string apiRequest, HttpContent requestContent, string requestType)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            AuthenticationContext authContext = new AuthenticationContext(WebConfigurationManager.AppSettings["adOath2AuthEndpoint"], false);
            UserCredential credentials = new UserCredential(WebConfigurationManager.AppSettings["crmUsername"],
                WebConfigurationManager.AppSettings["crmPassword"]);
            AuthenticationResult tokenResult = authContext.AcquireToken(WebConfigurationManager.AppSettings["crmUri"],
                WebConfigurationManager.AppSettings["adClientId"], credentials);
            HttpResponseMessage apiResponse;

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(WebConfigurationManager.AppSettings["crmUri"]);
                httpClient.Timeout = new TimeSpan(0, 2, 0);
                httpClient.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
                httpClient.DefaultRequestHeaders.Add("OData-Version", "4.0");
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);

                if (requestType == "retrieve")
                {
                    apiResponse = await httpClient.GetAsync(apiRequest);
                }
                else if (requestType == "create")
                {
                    apiResponse = await httpClient.PostAsync(apiRequest, requestContent);
                }
                else
                {
                    apiResponse = null;
                }
            }
            return apiResponse;
        }

        //public static Double TextAnalytics(IDialogContext context, string keyword)
        //{
        //    ITextAnalyticsClient client = new TextAnalyticsClient(new ApiKeyServiceClientCredentials())
        //    {
        //        BaseUri = new Uri(WebConfigurationManager.AppSettings["TextAnalyticsEndpoint"])
        //    };

        //    try
        //    {
        //        var result = client.SentimentAsync(new MultiLanguageBatchInput(
        //                new List<MultiLanguageInput>()
        //                {
        //                  new MultiLanguageInput("en", "0", keyword)
        //                })).Result;

        //        if (result.Documents.Count > 0)
        //        {
        //            return Math.Round(Convert.ToDouble(result.Documents[0].Score), 2);
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }

        //    return 0;
        //}

        public static IForm<JObject> BuildJsonForm()
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BotApplicationDemo.JsonSchema.CreateIncident_Schema.json"))
            {
                var schema = JObject.Parse(new StreamReader(stream).ReadToEnd());
                return new FormBuilderJson(schema)
                    .AddRemainingFields()
                    .Build();
            }
        }

        public static async Task CreateReply(ResumeAfter<IMessageActivity> resume, IDialogContext context, Activity activity, string message)
        {
            var reply = activity.CreateReply(message);
            reply.Type = ActivityTypes.Message;
            reply.TextFormat = TextFormatTypes.Plain;

            await context.PostAsync(reply);
            context.Wait(resume);
        }

        public static async Task SubmitCaseAsync(IDialogContext context, CaseDetail caseDetail, Contact contact)
        {
            try
            {
                var stringContent = new StringContent("{" +
                    "\"title\":\"" + caseDetail.categoryName + " Case for " + contact.Fullname + " regarding " + caseDetail.productName + "\"," +
                    "\"description\":\"" + caseDetail.problemString + "\"," +
                    "\"customerid_contact@odata.bind\":\"/contacts(" + contact.ContactId + ")\"," +
                    "\"productid@odata.bind\":\"/products(" + caseDetail.productId.ToString() + ")\"," +
                    "\"a1a_Category@odata.bind\":\"/categories(" + caseDetail.categoryId.ToString() + ")\"," +
                    "\"caseorigincode\":" + Convert.ToInt32(WebConfigurationManager.AppSettings["LiveAssistCaseOriginCode"].ToString()) + string.Empty +
                    "}", System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage caseResponse = await CRMWebAPIRequest("/api/data/v9.0/incidents", stringContent, "create");

                if (caseResponse.IsSuccessStatusCode)
                {
                    Guid caseId = new Guid();
                    string caseDetailsRequest = string.Empty;
                    string caseUri = caseResponse.Headers.GetValues("OData-EntityId").FirstOrDefault();
                    if (caseUri != null)
                    {
                        caseId = Guid.Parse(caseUri.Split('(', ')')[1]);
                        caseDetailsRequest = "api/data/v9.0/incidents(" +
                            caseId + ")?" + "$select=ticketnumber";

                        HttpResponseMessage caseDetailsResponse = await CRMWebAPIRequest(caseDetailsRequest, null, "retrieve");

                        if (caseDetailsResponse.IsSuccessStatusCode)
                        {
                            string myString = caseDetailsResponse.Content.ReadAsStringAsync().Result;
                            JObject caseResults =
                                JObject.Parse(caseDetailsResponse.Content.ReadAsStringAsync().Result);
                            string ticketNumber = (string)caseResults["ticketnumber"];

                            await context.PostAsync("Sorry to hear that. Here's your ticket number: " + ticketNumber + ". " + Utilities.RandomPhrase(Common.Dialogs.articleNotUsefulDialog));
                            context.Wait(Escalate);
                        }
                        else
                        {
                            await SubmitCaseAsync(context, caseDetail, contact);
                        }
                    }
                    else
                    {
                        await SubmitCaseAsync(context, caseDetail, contact);
                    }
                }
            }
            catch (Exception)
            {
                await SubmitCaseAsync(context, caseDetail, contact);
            }

            //context.Done<bool>(true);
        }

        public static async Task SubmitCaseDeflection(IDialogContext context, CaseDetail caseDetail, Contact contact)
        {
            try
            {
                var stringContent = new StringContent("{" +
                    "\"adx_name\":\"" + caseDetail.categoryName + " Case for " + contact.Fullname + " regarding " + caseDetail.productName + "\"," +
                    "\"adx_casetitle\":\"" + caseDetail.categoryName + " Case for " + contact.Fullname + " regarding " + caseDetail.productName + "\"," +
                    "\"a1a_description\":\"" + caseDetail.problemString + "\"," +
                    "\"adx_Contact@odata.bind\":\"/contacts(" + contact.ContactId + ")\"," +
                    "\"a1a_ProductId@odata.bind\":\"/products(" + caseDetail.productId.ToString() + ")\"," +
                    "\"adx_KnowledgeArticle@odata.bind\":\"/knowledgearticles(" + caseDetail.kbItemResults[caseDetail.kbItemResultsIndex]["knowledgearticleid"].ToString() + ")\"" +
                    "}", System.Text.Encoding.UTF8, "application/json");

                var str = stringContent.ReadAsStringAsync();

                HttpResponseMessage caseResponse = await CRMWebAPIRequest("/api/data/v9.0/adx_casedeflections", stringContent, "create");

                if (caseResponse.IsSuccessStatusCode)
                {
                    await context.PostAsync(Utilities.RandomPhrase(Common.Dialogs.articleUsefulDialog));
                }
                else
                {
                    await SubmitCaseDeflection(context, caseDetail, contact);
                }
            }
            catch (Exception)
            {
                await SubmitCaseDeflection(context, caseDetail, contact);
            }
        }

        public static async Task Escalate(IDialogContext context, IAwaitable<object> argument)
        {
            var activity = await argument as Activity;

            if (activity.Text.ToLower().Contains("yes"))
            {
                await context.PostAsync(Utilities.RandomPhrase(Common.Dialogs.escalateDialog));

                IMessageActivity transferMsg = context.MakeMessage();
                JObject transferChannelData = JObject.Parse(@"{'type':'transfer','skill':'BotEscalation'}");
                transferMsg.ChannelData = transferChannelData;
                transferMsg.Text = string.Empty;
                transferMsg.Type = ActivityTypes.Message;
                await context.PostAsync(transferMsg);
            }
            else
            {
                await context.PostAsync(Utilities.RandomPhrase(Common.Dialogs.dontEscalateDialog));
            }
        }

        public static async Task<Contact> GetContactDetails(IDialogContext context, string contactid, string emailaddress)
        {
            Contact contact = new Contact();

            HttpResponseMessage contactResponse = new HttpResponseMessage();

            if (contactid == null || contactid == string.Empty)
            {
                contactResponse = await CRMWebAPIRequest("/api/data/v9.0/contacts?$select=contactid,emailaddress1,fullname&$filter=contains(emailaddress1,%20%27" + emailaddress + "%27)", null, "retrieve");
            }

            if (emailaddress == null || emailaddress == string.Empty)
            {
                contactResponse = await CRMWebAPIRequest("/api/data/v9.0/contacts?$select=contactid,emailaddress1,fullname&$filter=contactid%20eq%20" + contactid, null, "retrieve");
            }

            if (contactResponse.IsSuccessStatusCode)
            {
                string contactResponseStr = contactResponse.Content.ReadAsStringAsync().Result;
                JObject contactResults = JObject.Parse(contactResponseStr);
                JArray contactItemResults = (JArray)contactResults["value"];

                if (contactItemResults.Count > 0)
                {
                    contact.Fullname = contactItemResults[0]["fullname"].ToString();
                    contact.EmailAddress = contactItemResults[0]["emailaddress1"].ToString();
                    contact.ContactId = contactItemResults[0]["contactid"].ToString();

                    return contact;
                }
            }

            contact.ContactId = WebConfigurationManager.AppSettings["AnonymousUserId"];
            contact.Fullname = "Anonymous User";

            return contact;

        }

        public static string RandomPhrase(string[] strArray)
        {
            Random rand = new Random();
            var str = strArray[rand.Next(strArray.Length - 1)];
            return str;
        }
    }
}