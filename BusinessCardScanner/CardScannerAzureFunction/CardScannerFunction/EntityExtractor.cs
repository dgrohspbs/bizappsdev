using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace CardScannerFunction
{
    public class ScannerOutputData
    {
        public string Article { get; set; }
        public string Mention { get; set; }
        public int Offset { get; set; }
        public int Length { get; set; }
        public string Type { get; set; }

        public override string ToString()
        {
            return string.Format("Article: {0},Mention: {1},Offset;{2},Length:{3},Type:{4}", Article, Mention, Offset, Length, Type);
        }


    }

    public class EntityExtractor
    {
        public List<ScannerOutputData> OutputData { get; set; }
        private const string baseUrl = "https://ussouthcentral.services.azureml.net/workspaces/6fd07cdc54234d52b8822c4b7f53a37c/services/74475c981eed45db81ebc3602f8e16df/execute?api-version=2.0&format=swagger";
        private const string apiKey = "thOgj8XCdo6fw/5y8UnkbI1nRg6D8SCdicz/FKntsKo/QMLj2xPS9R2JtDVWxEEq8KTptJHp0osl+EGba0O37A==";

        /// <summary>
        /// Creates a new instance and immediately extracts entities in the given text lines
        /// </summary>
        /// <param name="textLines"></param>
        public EntityExtractor(List<string> textLines)
        {
            ExtractEntitiesFromText(textLines).Wait();
        }

        /// <summary>
        /// Use Azure ML and named entity extraction to predict what person name, organisation and address from text 
        /// </summary>
        /// <param name="textLines"></param>
        /// <returns></returns>
        private async Task ExtractEntitiesFromText(List<string> textLines)
        {
            var webServiceInputText = String.Join(", ", textLines); //The ML model takes all lines comma seperated
            using (var client = new HttpClient())
            {
                var scoreRequest = new
                {
                    Inputs = new Dictionary<string, List<Dictionary<string, string>>>() {
                        {
                            "bcScannerInput",
                            new List<Dictionary<string, string>>(){new Dictionary<string, string>(){
                                            {
                                                "Col1", webServiceInputText
                                            },
                                }
                            }
                        },
                    },
                    GlobalParameters = new Dictionary<string, string>() {
                        {
                            "BusinessCardText",""
                        },
                    }
                };

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                client.BaseAddress = new Uri(baseUrl);
                var response = await client.PostAsJsonAsync("", scoreRequest);

                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Result: {0}", result);
                    OutputData = ParseResponse(result);
                }
                else
                {
                    Console.WriteLine(string.Format("The request failed with status code: {0}", response.StatusCode));
                    // Print the headers - they include the requert ID and the timestamp,
                    // which are useful for debugging the failure
                    Console.WriteLine(response.Headers.ToString());

                    string responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responseContent);
                    throw new Exception(responseContent);
                }
            }
        }

        private List<ScannerOutputData> ParseResponse(string responseData)
        {
            //remove leading and trailing brackets to allow deserialization of JSON into a collection
            var trimmedResponseData = responseData.Remove(0, responseData.IndexOf("["));
            trimmedResponseData = trimmedResponseData.Remove(trimmedResponseData.LastIndexOf("]") + 1, 2);

            return JsonConvert.DeserializeObject<List<ScannerOutputData>>(trimmedResponseData);
            //System.Console.WriteLine("Result parsed: \n");

            //jsonData.ForEach(d => System.Console.WriteLine(d.ToString()));
        }
    }
}