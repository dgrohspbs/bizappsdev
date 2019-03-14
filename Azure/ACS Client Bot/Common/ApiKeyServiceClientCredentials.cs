using Microsoft.Rest;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using System.Web.Configuration;

namespace ClientFacingBot.Common
{
    public class ApiKeyServiceClientCredentials : ServiceClientCredentials
    {
        public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add("Ocp-Apim-Subscription-Key", WebConfigurationManager.AppSettings["Ocp-Apim-Subscription-Key"]);
            return base.ProcessHttpRequestAsync(request, cancellationToken);
        }
    }
}