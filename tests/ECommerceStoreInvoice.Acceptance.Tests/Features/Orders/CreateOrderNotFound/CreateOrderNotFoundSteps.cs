using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using Reqnroll;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Orders.CreateOrderNotFound
{
    [Binding]
    public sealed class CreateOrderNotFoundSteps
    {
        private readonly ScenarioApiContext _apiContext;
        private Guid _clientId;

        public CreateOrderNotFoundSteps(ScenarioApiContext apiContext)
        {
            _apiContext = apiContext;
        }

        [Given("I have a non-existing client id for order creation")]
        public void GivenIHaveANonExistingClientIdForOrderCreation(Table table)
        {
            _clientId = Guid.NewGuid();

            var requestContext = ParseExpectedTable(table);
            var requestObject = new CreateOrderNotFoundRequestContext(
                requestContext.TryGetValue("ClientId", out var clientIdTemplate) ? clientIdTemplate : "<generatedId>",
                _clientId.ToString());

            AllureJson.AttachObject(
                "Create order not found request",
                requestObject,
                _apiContext.JsonOptions);
        }

        [When("I submit create order request for a non-existing client")]
        public async Task WhenISubmitCreateOrderRequestForANonExistingClient(Table table)
        {
            var requestData = ParseExpectedTable(table);
            var requestObject = new CreateOrderNotFoundHttpRequest(
                Method: requestData.TryGetValue("Method", out var method) ? method : "POST",
                Endpoint: requestData.TryGetValue("Endpoint", out var endpointTemplate)
                    ? endpointTemplate.Replace("{clientId}", _clientId.ToString(), StringComparison.OrdinalIgnoreCase)
                    : $"/orders/{_clientId}",
                Body: requestData.TryGetValue("Body", out var requestBodyValue) ? requestBodyValue : "null");

            AllureJson.AttachObject("Create order HTTP request", requestObject, _apiContext.JsonOptions);

            _apiContext.Response = await _apiContext.HttpClient.PostAsync(requestObject.Endpoint, content: null);

            var responseBody = await _apiContext.Response.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Response JSON ({(int)_apiContext.Response.StatusCode})", responseBody);
        }

        [Then("problem details are returned for create order not found")]
        public async Task ThenProblemDetailsAreReturnedForCreateOrderNotFound(Table table)
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
                    problemDetails.Detail.ShouldContain("ShoppingCart", Case.Insensitive);
                }
                else
                {
                    problemDetails.Detail.ShouldBeNullOrWhiteSpace();
                }
            }

            var expectedInstance = GetRequiredValue(expected, "Instance").Replace("{clientId}", _clientId.ToString(), StringComparison.OrdinalIgnoreCase);
            problemDetails.Instance.ShouldBe(expectedInstance);

            if (TryGetBool(expected, "HasTraceId", out var hasTraceId))
            {
                if (hasTraceId)
                {
                    problemDetails.TraceId.ShouldNotBeNullOrWhiteSpace();
                }
                else
                {
                    problemDetails.TraceId.ShouldBeNullOrWhiteSpace();
                }
            }
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

        private sealed record CreateOrderNotFoundRequestContext(string ClientId, string ResolvedClientId);

        private sealed record CreateOrderNotFoundHttpRequest(string Method, string Endpoint, string Body);
    }
}
