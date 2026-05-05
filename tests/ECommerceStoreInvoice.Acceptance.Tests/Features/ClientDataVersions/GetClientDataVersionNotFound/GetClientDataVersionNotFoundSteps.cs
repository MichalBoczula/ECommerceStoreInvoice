using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using Reqnroll;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.ClientDataVersions.GetClientDataVersionNotFound
{
    [Binding]
    public sealed class GetClientDataVersionNotFoundSteps
    {
        private readonly ScenarioApiContext _apiContext;
        private Guid _clientId;

        public GetClientDataVersionNotFoundSteps(ScenarioApiContext apiContext)
        {
            _apiContext = apiContext;
        }

        [Given("I have a non-existing client id for client data version retrieval")]
        public void GivenIHaveANonExistingClientIdForClientDataVersionRetrieval(Table table)
        {
            _clientId = Guid.NewGuid();

            var requestTemplate = ParseExpectedTable(table);
            var request = new
            {
                Method = GetRequiredValue(requestTemplate, "Method"),
                Endpoint = GetRequiredValue(requestTemplate, "Endpoint").Replace("{clientId}", _clientId.ToString(), StringComparison.OrdinalIgnoreCase),
                ClientId = _clientId,
                ClientIdSource = GetRequiredValue(requestTemplate, "ClientIdSource")
            };

            AllureJson.AttachObject(
                "Get client data version not found request",
                request,
                _apiContext.JsonOptions);
        }

        [When("I request the client data version by client id for non-existing client")]
        public async Task WhenIRequestTheClientDataVersionByClientIdForNonExistingClient()
        {
            _apiContext.Response = await _apiContext.HttpClient.GetAsync($"/client-data-versions/client/{_clientId}");

            var body = await _apiContext.Response.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Response JSON ({(int)_apiContext.Response.StatusCode})", body);
        }

        [Then("problem details are returned for get client data version not found")]
        public async Task ThenProblemDetailsAreReturnedForGetClientDataVersionNotFound(Table table)
        {
            var expected = ParseExpectedTable(table);

            _apiContext.Response.ShouldNotBeNull();
            _apiContext.Response!.StatusCode.ShouldBe(ParseStatusCode(expected, "StatusCode"));

            var problemDetails = await DeserializeResponse<NotFoundProblemDetails>(_apiContext.Response);
            problemDetails.ShouldNotBeNull();

            problemDetails!.Title.ShouldBe(GetRequiredValue(expected, "Title"));
            problemDetails.Type.ShouldBe(GetRequiredValue(expected, "Type"));

            if (TryGetBool(expected, "HasDetail", out var hasDetail))
            {
                if (hasDetail)
                {
                    problemDetails.Detail.ShouldNotBeNullOrWhiteSpace();
                    problemDetails.Detail!.ShouldContain(_clientId.ToString(), Case.Insensitive);
                    problemDetails.Detail.ShouldContain("ClientDataVersion", Case.Insensitive);
                }
                else
                {
                    problemDetails.Detail.ShouldBeNullOrWhiteSpace();
                }
            }

            var expectedInstance = GetRequiredValue(expected, "Instance").Replace("{clientId}", _clientId.ToString(), StringComparison.OrdinalIgnoreCase);
            problemDetails.Instance.ShouldBe(expectedInstance);

            var detailContains = GetRequiredValue(expected, "DetailContains");
            problemDetails.Detail.ShouldContain(detailContains, Case.Insensitive);

            if (TryGetBool(expected, "DetailContainsGuid", out var detailContainsGuid) && detailContainsGuid)
            {
                problemDetails.Detail.ShouldContain(_clientId.ToString(), Case.Insensitive);
            }

            bool? hasTraceIdValue = null;
            if (TryGetBool(expected, "HasTraceId", out var hasTraceId))
            {
                hasTraceIdValue = hasTraceId;
                if (hasTraceId)
                {
                    problemDetails.TraceId.ShouldNotBeNullOrWhiteSpace();
                }
                else
                {
                    problemDetails.TraceId.ShouldBeNullOrWhiteSpace();
                }
            }

            var expectedResponse = new
            {
                StatusCode = (int)ParseStatusCode(expected, "StatusCode"),
                Title = GetRequiredValue(expected, "Title"),
                Type = GetRequiredValue(expected, "Type"),
                DetailContains = detailContains,
                Instance = expectedInstance,
                HasTraceId = hasTraceIdValue
            };

            AllureJson.AttachObject(
                "Get client data version not found expected response",
                expectedResponse,
                _apiContext.JsonOptions);
        }

        private async Task<T?> DeserializeResponse<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content, _apiContext.JsonOptions);
        }

        private static Dictionary<string, string> ParseExpectedTable(Table table)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in table.Rows)
            {
                values[row["Field"]] = row["Value"];
            }

            return values;
        }

        private static string GetRequiredValue(IReadOnlyDictionary<string, string> values, string key)
        {
            if (!values.TryGetValue(key, out var value))
            {
                throw new InvalidOperationException($"Missing '{key}' value in problem details expected result table.");
            }

            return value;
        }

        private static HttpStatusCode ParseStatusCode(IReadOnlyDictionary<string, string> values, string key)
        {
            var value = GetRequiredValue(values, key);
            return (HttpStatusCode)int.Parse(value, CultureInfo.InvariantCulture);
        }

        private static bool TryGetBool(IReadOnlyDictionary<string, string> values, string key, out bool result)
        {
            if (!values.TryGetValue(key, out var value))
            {
                result = false;
                return false;
            }

            result = bool.Parse(value);
            return true;
        }
    }
}
