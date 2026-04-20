using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;
using Reqnroll;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Orders.GetOrdersByClientIdEmptySuccess
{
    [Binding]
    public sealed class GetOrdersByClientIdEmptySuccessSteps
    {
        private readonly ScenarioApiContext _apiContext;
        private Guid _clientId;

        public GetOrdersByClientIdEmptySuccessSteps(ScenarioApiContext apiContext)
        {
            _apiContext = apiContext;
        }

        [Given("I have a client without orders")]
        public void GivenIHaveAClientWithoutOrders()
        {
            _clientId = Guid.NewGuid();

            AllureJson.AttachObject(
                "Get orders by client id request (empty result setup)",
                new
                {
                    ClientId = _clientId,
                    Endpoint = $"/orders/client/{_clientId}",
                    Method = "GET",
                    Expectation = "No orders for this client"
                },
                _apiContext.JsonOptions);
        }

        [When("I request orders by client id for the client without orders")]
        public async Task WhenIRequestOrdersByClientIdForTheClientWithoutOrders()
        {
            _apiContext.Response = await _apiContext.HttpClient.GetAsync($"/orders/client/{_clientId}");

            var body = await _apiContext.Response.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Response JSON ({(int)_apiContext.Response.StatusCode})", body);
        }

        [Then("an empty list of orders is returned successfully")]
        public async Task ThenAnEmptyListOfOrdersIsReturnedSuccessfully(Table table)
        {
            var expected = ParseExpectedTable(table);

            AllureJson.AttachObject("Expected empty orders result", expected, _apiContext.JsonOptions);

            _apiContext.Response.ShouldNotBeNull();
            _apiContext.Response!.StatusCode.ShouldBe(ParseStatusCode(expected, "StatusCode"));

            var orders = await DeserializeResponse<IReadOnlyCollection<OrderResponseDto>>(_apiContext.Response);
            orders.ShouldNotBeNull();

            var ordersList = orders!.ToList();
            ordersList.Count.ShouldBe(ParseInt(expected, "OrdersCount", ordersList.Count));

            if (TryGetBool(expected, "IsEmpty", out var isEmpty))
            {
                ordersList.Any().ShouldBe(!isEmpty);
            }

            AllureJson.AttachObject(
                "Actual empty orders result",
                new
                {
                    ClientId = _clientId,
                    OrdersCount = ordersList.Count,
                    IsEmpty = !ordersList.Any()
                },
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
                throw new InvalidOperationException($"Missing '{key}' value in orders expected result table.");
            }

            return value;
        }

        private static HttpStatusCode ParseStatusCode(IReadOnlyDictionary<string, string> values, string key)
        {
            var value = GetRequiredValue(values, key);
            return (HttpStatusCode)int.Parse(value, CultureInfo.InvariantCulture);
        }

        private static int ParseInt(IReadOnlyDictionary<string, string> values, string key, int fallback)
        {
            if (!values.TryGetValue(key, out var value))
            {
                return fallback;
            }

            return int.Parse(value, CultureInfo.InvariantCulture);
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
