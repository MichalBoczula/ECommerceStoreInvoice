using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using Reqnroll;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.ShoppingCarts.UpdateShoppingCartSuccess
{
    [Binding]
    public sealed class UpdateShoppingCartSuccessSteps
    {
        private readonly ScenarioApiContext _apiContext;
        private Guid _clientId;
        private UpdateShoppingCartRequestDto? _request;

        public UpdateShoppingCartSuccessSteps(ScenarioApiContext apiContext)
        {
            _apiContext = apiContext;
        }

        [Given("I have an existing shopping cart for update")]
        public async Task GivenIHaveAnExistingShoppingCartForUpdate()
        {
            _clientId = Guid.NewGuid();

            AllureJson.AttachObject(
                "Update shopping cart setup request",
                new { ClientId = _clientId },
                _apiContext.JsonOptions);

            var createResponse = await _apiContext.HttpClient.PostAsync($"/shopping-carts/{_clientId}", content: null);
            createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            var createBody = await createResponse.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Setup response JSON ({(int)createResponse.StatusCode})", createBody);
        }

        [Given("I have a valid update shopping cart request")]
        public void GivenIHaveAValidUpdateShoppingCartRequest()
        {
            _request = new UpdateShoppingCartRequestDto
            {
                Lines =
                [
                    new ShoppingCartLineRequestDto
                    {
                        ProductId = Guid.NewGuid(),
                        Name = "Phone",
                        Brand = "Apple",
                        UnitPriceAmount = 999.99m,
                        UnitPriceCurrency = "usd",
                        Quantity = 2
                    },
                    new ShoppingCartLineRequestDto
                    {
                        ProductId = Guid.NewGuid(),
                        Name = "Watch",
                        Brand = "Apple",
                        UnitPriceAmount = 399.99m,
                        UnitPriceCurrency = "usd",
                        Quantity = 1
                    }
                ]
            };

            AllureJson.AttachObject(
                "Update shopping cart request",
                _request,
                _apiContext.JsonOptions);
        }

        [When("I submit the update shopping cart request")]
        public async Task WhenISubmitTheUpdateShoppingCartRequest()
        {
            _request.ShouldNotBeNull();

            _apiContext.Response = await _apiContext.HttpClient.PutAsJsonAsync($"/shopping-carts/{_clientId}", _request, _apiContext.JsonOptions);

            var body = await _apiContext.Response.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Response JSON ({(int)_apiContext.Response.StatusCode})", body);
        }

        [Then("the shopping cart is updated successfully")]
        public async Task ThenTheShoppingCartIsUpdatedSuccessfully(Table table)
        {
            var expected = ParseExpectedTable(table);

            _apiContext.Response.ShouldNotBeNull();
            _apiContext.Response!.StatusCode.ShouldBe(ParseStatusCode(expected, "StatusCode"));

            var shoppingCart = await DeserializeResponse<ShoppingCartResponseDto>(_apiContext.Response);
            shoppingCart.ShouldNotBeNull();

            if (TryGetBool(expected, "HasId", out var hasId))
            {
                if (hasId)
                {
                    shoppingCart!.Id.ShouldNotBe(Guid.Empty);
                }
                else
                {
                    shoppingCart!.Id.ShouldBe(Guid.Empty);
                }
            }

            if (TryGetBool(expected, "HasClientId", out var hasClientId))
            {
                if (hasClientId)
                {
                    shoppingCart!.ClientId.ShouldBe(_clientId);
                }
                else
                {
                    shoppingCart!.ClientId.ShouldBe(Guid.Empty);
                }
            }

            shoppingCart!.TotalAmount.ShouldBe(ParseDecimal(expected, "TotalAmount", shoppingCart.TotalAmount));
            shoppingCart.TotalCurrency.ShouldBe(GetExpectedValue(expected, "TotalCurrency", shoppingCart.TotalCurrency));
            shoppingCart.Lines.Count.ShouldBe(ParseInt(expected, "LinesCount", shoppingCart.Lines.Count));

            var firstLine = shoppingCart.Lines.FirstOrDefault();
            if (firstLine is not null)
            {
                firstLine.Name.ShouldBe(GetExpectedValue(expected, "FirstLineName", firstLine.Name));
                firstLine.Brand.ShouldBe(GetExpectedValue(expected, "FirstLineBrand", firstLine.Brand));
                firstLine.Quantity.ShouldBe(ParseInt(expected, "FirstLineQuantity", firstLine.Quantity));
                firstLine.TotalAmount.ShouldBe(ParseDecimal(expected, "FirstLineTotalAmount", firstLine.TotalAmount));
                firstLine.TotalCurrency.ShouldBe(GetExpectedValue(expected, "FirstLineTotalCurrency", firstLine.TotalCurrency));
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
                throw new InvalidOperationException($"Missing '{key}' value in shopping cart expected result table.");
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

        private static decimal ParseDecimal(IReadOnlyDictionary<string, string> values, string key, decimal fallback)
        {
            if (!values.TryGetValue(key, out var value))
            {
                return fallback;
            }

            return decimal.Parse(value, CultureInfo.InvariantCulture);
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
