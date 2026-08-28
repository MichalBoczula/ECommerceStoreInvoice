using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using Reqnroll;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.ShoppingCarts.GetShoppingCartNotFound
{
    [Binding]
    public sealed class GetShoppingCartNotFoundSteps
    {
        private readonly ScenarioApiContext _apiContext;
        private Guid _clientId;
        private string _requestPath = null!;

        public GetShoppingCartNotFoundSteps(ScenarioApiContext apiContext)
        {
            _apiContext = apiContext;
        }

        [Given("I have a non-existing shopping cart request payload")]
        public void GivenIHaveANonExistingShoppingCartRequestPayload(Table table)
        {
            var requestValues = ParseExpectedTable(table);
            _clientId = Guid.NewGuid();

            var pathTemplate = GetRequiredValue(requestValues, "Path");
            _requestPath = pathTemplate.Replace("{clientId}", _clientId.ToString(), StringComparison.OrdinalIgnoreCase);
            var request = new
            {
                Method = GetRequiredValue(requestValues, "Method"),
                PathTemplate = pathTemplate,
                Path = _requestPath,
                ClientId = _clientId
            };

            AllureJson.AttachObject(
                "Get shopping cart not found request",
                request,
                _apiContext.JsonOptions);
        }

        [When("I request the shopping cart by client id for non-existing client")]
        public async Task WhenIRequestTheShoppingCartByClientIdForNonExistingClient()
        {
            _apiContext.Response = await _apiContext.HttpClient.GetAsync(_requestPath);

            var body = await _apiContext.Response.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Response JSON ({(int)_apiContext.Response.StatusCode})", body);
        }

        [Then("problem details are returned for get shopping cart not found")]
        public async Task ThenProblemDetailsAreReturnedForGetShoppingCartNotFound(Table table)
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
                HasDetail = TryGetBool(expected, "HasDetail", out var expectedHasDetail) ? expectedHasDetail : (bool?)null,
                Instance = expectedInstance,
                HasTraceId = hasTraceIdValue
            };

            AllureJson.AttachObject(
                "Get shopping cart not found expected response",
                expectedResponse,
                _apiContext.JsonOptions);
        }

        [Then("the problem details json contains")]
        public async Task ThenTheProblemDetailsJsonContains(Table table)
        {
            var expected = ParseExpectedTable(table);
            var problemDetails = await DeserializeResponse<NotFoundProblemDetails>(_apiContext.Response!);
            problemDetails.ShouldNotBeNull();

            problemDetails!.Title.ShouldBe(GetRequiredValue(expected, "title"));
            problemDetails.Type.ShouldBe(GetRequiredValue(expected, "type"));
            problemDetails.Status.ShouldBe(int.Parse(GetRequiredValue(expected, "status"), CultureInfo.InvariantCulture));
            problemDetails.Instance.ShouldBe(GetRequiredValue(expected, "instance").Replace("{clientId}", _clientId.ToString(), StringComparison.OrdinalIgnoreCase));

            var detailRule = GetRequiredValue(expected, "detail");
            if (detailRule.StartsWith("contains:", StringComparison.OrdinalIgnoreCase))
            {
                var expectedFragment = detailRule["contains:".Length..];
                problemDetails.Detail.ShouldNotBeNullOrWhiteSpace();
                problemDetails.Detail!.ShouldContain(expectedFragment, Case.Insensitive);
            }

            if (string.Equals(GetRequiredValue(expected, "traceId"), "not-empty", StringComparison.OrdinalIgnoreCase))
            {
                problemDetails.TraceId.ShouldNotBeNullOrWhiteSpace();
            }

            AllureJson.AttachObject("Expected problem details json", expected, _apiContext.JsonOptions);
            AllureJson.AttachObject("Actual problem details json", problemDetails, _apiContext.JsonOptions);
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
