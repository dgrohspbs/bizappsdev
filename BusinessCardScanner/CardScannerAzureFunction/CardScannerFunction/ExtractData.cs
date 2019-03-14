using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace CardScannerFunction
{
    public static class ExtractData
    {
        [FunctionName("ExtractData")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            try
            {
                log.Info("Extract card data - function processing a request.");

                // parse query parameter
                string fullText = req.GetQueryNameValuePairs()
                    .FirstOrDefault(q => string.Compare(q.Key, "fulltext", true) == 0)
                    .Value;

                // Get request body
                //dynamic data = await req.Content.ReadAsAsync<object>();
                string data = await req.Content.ReadAsStringAsync();
                fullText = fullText ?? data.Substring(data.IndexOf(":")+1);
                //Initialize a new instance of businesscarddata that parses the text values
                var extractedData = new BusinessCardData(fullText);

                return fullText == null
                    ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a full OCRd business card text on the query string or in the request body as parameter 'fulltext'")
                    : req.CreateResponse(HttpStatusCode.OK, new
                    {
                        Fullname = extractedData.FullName,
                        Organisation = extractedData.Organisation,
                        Address = extractedData.Address,
                        Mobile = extractedData.Mobile,
                        Phone = extractedData.FixedLine,
                        EMail = extractedData.Email,
                        Website = extractedData.Website
                    }
                        );
            }
            catch (Exception ex)
            {
                log.Error("Extract card data - error : " + ex.Message);
                return req.CreateResponse(HttpStatusCode.InternalServerError, "An error occured processing your request");
            }
        }
    }
}
