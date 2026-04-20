using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using Reqnroll;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Orders.GetOrdersByClientIdSuccess
{
    [Binding]
    public sealed class GetOrdersByClientIdSuccessSteps
    {
        private readonly ScenarioApiContext _apiContext;
        private Guid _clientId;

        public GetOrdersByClientIdSuccessSteps(ScenarioApiContext apiContext)
        {
            _apiContext = apiContext;
        }

        [Given("I have existing orders for a client")]
        public async Task GivenIHaveExistingOrdersForAClient()
        {
            _clientId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            AllureJson.AttachObject(
                "Get orders setup request",
                new
                {
                    ClientId = _clientId,
                    ShoppingCartLines = new[]
                    {
                        new
                        {
                            ProductId = productId,
                            Name = "Laptop",
                            Brand = "Lenovo",
                            UnitPriceAmount = 999.99m,
                            UnitPriceCurrency = "usd",
                            Quantity = 2
                        }
                    }
                },
                _apiContext.JsonOptions);

            var createShoppingCartResponse = await _apiContext.HttpClient.PostAsync($"/shopping-carts/{_clientId}", content: null);
            createShoppingCartResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            var createShoppingCartBody = await createShoppingCartResponse.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Create shopping cart response JSON ({(int)createShoppingCartResponse.StatusCode})", createShoppingCartBody);

            var updateRequest = new UpdateShoppingCartRequestDto
            {
                Lines =
                [
                    new ShoppingCartLineRequestDto
                    {
                        ProductId = productId,
                        Name = "Laptop",
                        Brand = "Lenovo",
                        UnitPriceAmount = 999.99m,
                        UnitPriceCurrency = "usd",
                        Quantity = 2
                    }
                ]
            };

            AllureJson.AttachObject("Update shopping cart setup request", updateRequest, _apiContext.JsonOptions);

            var updateShoppingCartResponse = await _apiContext.HttpClient.PutAsJsonAsync($"/shopping-carts/{_clientId}", updateRequest, _apiContext.JsonOptions);
            updateShoppingCartResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            var updateShoppingCartBody = await updateShoppingCartResponse.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Update shopping cart response JSON ({(int)updateShoppingCartResponse.StatusCode})", updateShoppingCartBody);

            var createOrderResponse = await _apiContext.HttpClient.PostAsync($"/orders/{_clientId}", content: null);
            createOrderResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            var createOrderBody = await createOrderResponse.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Create order response JSON ({(int)createOrderResponse.StatusCode})", createOrderBody);

            var refillShoppingCartResponse = await _apiContext.HttpClient.PutAsJsonAsync($"/shopping-carts/{_clientId}", updateRequest, _apiContext.JsonOptions);
            refillShoppingCartResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            var refillShoppingCartBody = await refillShoppingCartResponse.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Refill shopping cart response JSON ({(int)refillShoppingCartResponse.StatusCode})", refillShoppingCartBody);

            var createSecondOrderResponse = await _apiContext.HttpClient.PostAsync($"/orders/{_clientId}", content: null);
            createSecondOrderResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            var createSecondOrderBody = await createSecondOrderResponse.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Create second order response JSON ({(int)createSecondOrderResponse.StatusCode})", createSecondOrderBody);
        }

        [When("I request orders by client id")]
        public async Task WhenIRequestOrdersByClientId()
        {
            _apiContext.Response = await _apiContext.HttpClient.GetAsync($"/orders/client/{_clientId}");

            var body = await _apiContext.Response.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Response JSON ({(int)_apiContext.Response.StatusCode})", body);
        }

        [Then("the orders are returned successfully")]
        public async Task ThenTheOrdersAreReturnedSuccessfully(Table table)
        {
            var expected = ParseExpectedTable(table);

            _apiContext.Response.ShouldNotBeNull();
            _apiContext.Response!.StatusCode.ShouldBe(ParseStatusCode(expected, "StatusCode"));

            var orders = await DeserializeResponse<IReadOnlyCollection<OrderResponseDto>>(_apiContext.Response);
            orders.ShouldNotBeNull();

            var ordersList = orders!.ToList();
            ordersList.Count.ShouldBe(ParseInt(expected, "OrdersCount", ordersList.Count));
            ordersList.Count(o => o.ClientId == _clientId).ShouldBe(ordersList.Count);

            var firstOrder = ordersList.First();

            if (TryGetBool(expected, "FirstOrderHasId", out var firstOrderHasId))
            {
                if (firstOrderHasId)
                {
                    firstOrder.Id.ShouldNotBe(Guid.Empty);
                }
                else
                {
                    firstOrder.Id.ShouldBe(Guid.Empty);
                }
            }

            if (TryGetBool(expected, "FirstOrderHasClientId", out var firstOrderHasClientId))
            {
                if (firstOrderHasClientId)
                {
                    firstOrder.ClientId.ShouldBe(_clientId);
                }
                else
                {
                    firstOrder.ClientId.ShouldBe(Guid.Empty);
                }
            }

            firstOrder.Status.ShouldBe(GetExpectedValue(expected, "FirstOrderStatus", firstOrder.Status));
            firstOrder.TotalAmount.ShouldBe(ParseDecimal(expected, "FirstOrderTotalAmount", firstOrder.TotalAmount));
            firstOrder.TotalCurrency.ShouldBe(GetExpectedValue(expected, "FirstOrderTotalCurrency", firstOrder.TotalCurrency));
            firstOrder.Lines.Count.ShouldBe(ParseInt(expected, "FirstOrderLinesCount", firstOrder.Lines.Count));

            var firstLine = firstOrder.Lines.FirstOrDefault();
            firstLine.ShouldNotBeNull();

            firstLine!.Name.ShouldBe(GetExpectedValue(expected, "FirstLineName", firstLine.Name));
            firstLine.Brand.ShouldBe(GetExpectedValue(expected, "FirstLineBrand", firstLine.Brand));
            firstLine.Quantity.ShouldBe(ParseInt(expected, "FirstLineQuantity", firstLine.Quantity));
            firstLine.UnitPriceAmount.ShouldBe(ParseDecimal(expected, "FirstLineUnitPriceAmount", firstLine.UnitPriceAmount));
            firstLine.UnitPriceCurrency.ShouldBe(GetExpectedValue(expected, "FirstLineUnitPriceCurrency", firstLine.UnitPriceCurrency));
            firstLine.TotalAmount.ShouldBe(ParseDecimal(expected, "FirstLineTotalAmount", firstLine.TotalAmount));
            firstLine.TotalCurrency.ShouldBe(GetExpectedValue(expected, "FirstLineTotalCurrency", firstLine.TotalCurrency));

            var expectedLinesCount = ParseInt(expected, "FirstOrderLinesCount", firstOrder.Lines.Count);
            var expectedLineQuantity = ParseInt(expected, "FirstLineQuantity", firstLine.Quantity);
            foreach (var order in ordersList)
            {
                order.Lines.Count.ShouldBe(expectedLinesCount);
                order.Lines.Sum(l => l.Quantity).ShouldBe(expectedLineQuantity);
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
                throw new InvalidOperationException($"Missing '{key}' value in orders expected result table.");
            }

            return value;
        }

        private static string GetExpectedValue(IReadOnlyDictionary<string, string> values, string key, string fallback)
        {
            return values.TryGetValue(key, out var value) ? value : fallback;
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

        private static decimal ParseDecimal(IReadOnlyDictionary<string, string> values, string key, decimal fallback)
        {
            if (!values.TryGetValue(key, out var value))
            {
                return fallback;
            }

            return decimal.Parse(value, CultureInfo.InvariantCulture);
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
