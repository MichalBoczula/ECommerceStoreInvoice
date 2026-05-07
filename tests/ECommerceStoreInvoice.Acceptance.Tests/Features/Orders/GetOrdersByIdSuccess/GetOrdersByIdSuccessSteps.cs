using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;
using Reqnroll;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Orders.GetOrdersByIdSuccess
{
    [Binding]
    public sealed class GetOrdersByIdSuccessSteps
    {
        private readonly ScenarioApiContext _apiContext;
        private Guid _clientId;
        private Guid _orderId;

        public GetOrdersByIdSuccessSteps(ScenarioApiContext apiContext)
        {
            _apiContext = apiContext;
        }

        [Given("I have an existing order id with setup data")]
        public async Task GivenIHaveAnExistingOrderIdWithSetupData(Table table)
        {
            var setup = ParseExpectedTable(table);

            _clientId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var productName = GetRequiredValue(setup, "ProductName");
            var productBrand = GetRequiredValue(setup, "ProductBrand");
            var unitPriceAmount = ParseDecimal(setup, "UnitPriceAmount", fallback: 0m);
            var unitPriceCurrency = GetRequiredValue(setup, "UnitPriceCurrency");
            var quantity = ParseInt(setup, "Quantity", fallback: 0);

            AllureJson.AttachObject(
                "Get order by id setup request",
                new
                {
                    ClientId = _clientId,
                    ShoppingCartLines = new[]
                    {
                        new
                        {
                            ProductId = productId,
                            Name = productName,
                            Brand = productBrand,
                            UnitPriceAmount = unitPriceAmount,
                            UnitPriceCurrency = unitPriceCurrency,
                            Quantity = quantity
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
                        Name = productName,
                        Brand = productBrand,
                        UnitPriceAmount = unitPriceAmount,
                        UnitPriceCurrency = unitPriceCurrency,
                        Quantity = quantity
                    }
                ]
            };

            AllureJson.AttachObject("Update shopping cart setup request", updateRequest, _apiContext.JsonOptions);

            var updateShoppingCartResponse = await _apiContext.HttpClient.PutAsJsonAsync($"/shopping-carts/{_clientId}", updateRequest, _apiContext.JsonOptions);
            updateShoppingCartResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            var updateShoppingCartBody = await updateShoppingCartResponse.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Update shopping cart response JSON ({(int)updateShoppingCartResponse.StatusCode})", updateShoppingCartBody);

            var createOrderResponse = await _apiContext.HttpClient.PostAsync($"/orders/{_clientId}", content: null);
            var createOrderBody = await createOrderResponse.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Create order response JSON ({(int)createOrderResponse.StatusCode})", createOrderBody);
            createOrderResponse.StatusCode.ShouldBe(HttpStatusCode.OK, createOrderBody);

            var createdOrder = JsonSerializer.Deserialize<OrderResponseDto>(createOrderBody, _apiContext.JsonOptions);
            createdOrder.ShouldNotBeNull();

            _orderId = createdOrder!.Id;
            _orderId.ShouldNotBe(Guid.Empty);
        }

        [When("I request order by id")]
        public async Task WhenIRequestOrderById()
        {
            _apiContext.Response = await _apiContext.HttpClient.GetAsync($"/orders/{_orderId}");

            var body = await _apiContext.Response.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Response JSON ({(int)_apiContext.Response.StatusCode})", body);
        }

        [Then("the order is returned successfully by id")]
        public async Task ThenTheOrderIsReturnedSuccessfullyById(Table table)
        {
            var expected = ParseExpectedTable(table);

            AllureJson.AttachObject("Expected get order by id result", expected, _apiContext.JsonOptions);

            _apiContext.Response.ShouldNotBeNull();
            _apiContext.Response!.StatusCode.ShouldBe(ParseStatusCode(expected, "StatusCode"));

            var order = await DeserializeResponse<OrderResponseDto>(_apiContext.Response);
            order.ShouldNotBeNull();

            if (TryGetBool(expected, "HasId", out var hasId))
            {
                if (hasId)
                {
                    order!.Id.ShouldNotBe(Guid.Empty);
                }
                else
                {
                    order!.Id.ShouldBe(Guid.Empty);
                }
            }

            if (TryGetBool(expected, "HasClientId", out var hasClientId))
            {
                if (hasClientId)
                {
                    order!.ClientId.ShouldBe(_clientId);
                }
                else
                {
                    order!.ClientId.ShouldBe(Guid.Empty);
                }
            }

            order!.Id.ShouldBe(_orderId);
            order.Status.ShouldBe(GetExpectedValue(expected, "Status", order.Status));
            order.TotalAmount.ShouldBe(ParseDecimal(expected, "TotalAmount", order.TotalAmount));
            order.TotalCurrency.ShouldBe(GetExpectedValue(expected, "TotalCurrency", order.TotalCurrency));
            order.Lines.Count.ShouldBe(ParseInt(expected, "LinesCount", order.Lines.Count));

            var firstLine = order.Lines.FirstOrDefault();
            firstLine.ShouldNotBeNull();

            if (TryGetBool(expected, "FirstLineHasProductVersionId", out var firstLineHasProductVersionId))
            {
                if (firstLineHasProductVersionId)
                {
                    firstLine!.ProductVersionId.ShouldNotBe(Guid.Empty);
                }
                else
                {
                    firstLine!.ProductVersionId.ShouldBe(Guid.Empty);
                }
            }

            firstLine!.Name.ShouldBe(GetExpectedValue(expected, "FirstLineName", firstLine.Name));
            firstLine.Brand.ShouldBe(GetExpectedValue(expected, "FirstLineBrand", firstLine.Brand));
            firstLine.Quantity.ShouldBe(ParseInt(expected, "FirstLineQuantity", firstLine.Quantity));
            firstLine.UnitPriceAmount.ShouldBe(ParseDecimal(expected, "FirstLineUnitPriceAmount", firstLine.UnitPriceAmount));
            firstLine.UnitPriceCurrency.ShouldBe(GetExpectedValue(expected, "FirstLineUnitPriceCurrency", firstLine.UnitPriceCurrency));
            firstLine.TotalAmount.ShouldBe(ParseDecimal(expected, "FirstLineTotalAmount", firstLine.TotalAmount));
            firstLine.TotalCurrency.ShouldBe(GetExpectedValue(expected, "FirstLineTotalCurrency", firstLine.TotalCurrency));

            AllureJson.AttachObject(
                "Actual get order by id result",
                new
                {
                    OrderId = order.Id,
                    ClientId = order.ClientId,
                    order.Status,
                    order.TotalAmount,
                    order.TotalCurrency,
                    LinesCount = order.Lines.Count,
                    FirstLine = firstLine
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
                throw new InvalidOperationException($"Missing '{key}' value in get order by id expected result table.");
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
