using ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using Reqnroll;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.ShoppingCarts.CreateShoppingCartSuccess
{
    [Binding]
    public sealed class CreateShoppingCartSuccessSteps
    {
        private readonly ScenarioApiContext _apiContext;
        private Guid _clientId;

        public CreateShoppingCartSuccessSteps(ScenarioApiContext apiContext)
        {
            _apiContext = apiContext;
        }

        [Given("I have a valid client id for shopping cart creation")]
        public void GivenIHaveAValidClientIdForShoppingCartCreation()
        {
            _clientId = Guid.NewGuid();

            AllureJson.AttachObject(
                "Create shopping cart request",
                new { ClientId = _clientId },
                _apiContext.JsonOptions);
        }

        [When("I submit the create shopping cart request")]
        public async Task WhenISubmitTheCreateShoppingCartRequest()
        {
            _apiContext.Response = await _apiContext.HttpClient.PostAsync($"/shopping-carts/{_clientId}", content: null);

            var body = await _apiContext.Response.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Response JSON ({(int)_apiContext.Response.StatusCode})", body);
        }

        [Then("the shopping cart is created successfully")]
        public async Task ThenTheShoppingCartIsCreatedSuccessfully(Table table)
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
