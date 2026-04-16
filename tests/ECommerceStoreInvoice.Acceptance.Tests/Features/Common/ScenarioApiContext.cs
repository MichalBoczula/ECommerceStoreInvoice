using System.Net.Http;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Common
{
    public sealed class ScenarioApiContext
    {
        public HttpClient HttpClient { get; set; } = default!;
        public HttpResponseMessage? Response { get; set; }

        public JsonSerializerOptions JsonOptions { get; } = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }
}
