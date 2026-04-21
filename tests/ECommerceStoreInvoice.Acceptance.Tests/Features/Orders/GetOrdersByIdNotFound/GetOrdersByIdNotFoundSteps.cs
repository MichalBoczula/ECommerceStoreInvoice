using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using Reqnroll;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Orders.GetOrdersByIdNotFound
{
    [Binding]
    public sealed class GetOrdersByIdNotFoundSteps
    {
        private readonly ScenarioApiContext _apiContext;
        private Guid _orderId;

        public GetOrdersByIdNotFoundSteps(ScenarioApiContext apiContext)
        {
            _apiContext = apiContext;
        }

        [Given("I have a non-existing order id for orders retrieval")]
        public void GivenIHaveANonExistingOrderIdForOrdersRetrieval()
        {
            _orderId = Guid.NewGuid();

            AllureJson.AttachObject(
                "Get order by id not found request",
                new { OrderId = _orderId },
                _apiContext.JsonOptions);
        }

        [When("I request order by id for non-existing order")]
        public async Task WhenIRequestOrderByIdForNonExistingOrder()
        {
            _apiContext.Response = await _apiContext.HttpClient.GetAsync($"/orders/{_orderId}");

            var body = await _apiContext.Response.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Response JSON ({(int)_apiContext.Response.StatusCode})", body);
        }

        [Then("problem details are returned for get order by id not found")]
        public async Task ThenProblemDetailsAreReturnedForGetOrderByIdNotFound(Table table)
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
                    problemDetails.Detail!.ShouldContain(_orderId.ToString(), Case.Insensitive);
                    problemDetails.Detail.ShouldContain("Order", Case.Insensitive);
                }
                else
                {
                    problemDetails.Detail.ShouldBeNullOrWhiteSpace();
                }
            }

            var expectedInstance = GetRequiredValue(expected, "Instance").Replace("{orderId}", _orderId.ToString(), StringComparison.OrdinalIgnoreCase);
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
    }
}
