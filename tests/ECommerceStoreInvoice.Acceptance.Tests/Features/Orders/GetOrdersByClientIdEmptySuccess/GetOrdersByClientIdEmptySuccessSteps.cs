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
        public void GivenIHaveAClientWithoutOrders(Table table)
        {
            var request = BuildRequestFromTable(table);
            _clientId = request.ClientId == "AUTO" ? Guid.NewGuid() : Guid.Parse(request.ClientId);

            var resolvedRequest = new
            {
                ClientId = _clientId,
                Method = request.Method,
                Endpoint = request.Endpoint.Replace("{id}", _clientId.ToString()),
                Description = request.Description
            };

            AllureJson.AttachObject(
                "Get orders by client id request (empty result setup)",
                resolvedRequest,
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
            var expected = BuildExpectedResponseFromTable(table);

            AllureJson.AttachObject("Expected empty orders result", expected, _apiContext.JsonOptions);

            _apiContext.Response.ShouldNotBeNull();
            _apiContext.Response!.StatusCode.ShouldBe((HttpStatusCode)expected.StatusCode);

            var orders = await DeserializeResponse<IReadOnlyCollection<OrderResponseDto>>(_apiContext.Response);
            orders.ShouldNotBeNull();

            var ordersList = orders!.ToList();
            ordersList.Count.ShouldBe(expected.OrdersCount);
            ordersList.Any().ShouldBe(!expected.IsEmpty);

            var expectedOrders = JsonSerializer.Deserialize<List<OrderResponseDto>>(expected.OrdersJson, _apiContext.JsonOptions) ?? [];
            ordersList.ShouldBe(expectedOrders);

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

        private static RequestTableData BuildRequestFromTable(Table table)
        {
            var values = ParseTable(table);
            return new RequestTableData(
                GetRequiredValue(values, "ClientId"),
                GetRequiredValue(values, "Method"),
                GetRequiredValue(values, "Endpoint"),
                GetRequiredValue(values, "Description"));
        }

        private static ExpectedResponseTableData BuildExpectedResponseFromTable(Table table)
        {
            var values = ParseTable(table);
            return new ExpectedResponseTableData(
                int.Parse(GetRequiredValue(values, "StatusCode"), CultureInfo.InvariantCulture),
                int.Parse(GetRequiredValue(values, "OrdersCount"), CultureInfo.InvariantCulture),
                bool.Parse(GetRequiredValue(values, "IsEmpty")),
                GetRequiredValue(values, "OrdersJson"));
        }

        private static Dictionary<string, string> ParseTable(Table table)
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

        private sealed record RequestTableData(string ClientId, string Method, string Endpoint, string Description);

        private sealed record ExpectedResponseTableData(int StatusCode, int OrdersCount, bool IsEmpty, string OrdersJson);

    }
}
