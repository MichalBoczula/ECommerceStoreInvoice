using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts;
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
        public void GivenIHaveAValidUpdateShoppingCartRequest(Table table)
        {
            var values = ParseExpectedTable(table);

            _request = new UpdateShoppingCartRequestDto
            {
                Lines =
                [
                    new ShoppingCartLineRequestDto
                    {
                        ProductId = ParseGuid(values, "Line1ProductId"),
                        Quantity = ParseInt(values, "Line1Quantity")
                    },
                    new ShoppingCartLineRequestDto
                    {
                        ProductId = ParseGuid(values, "Line2ProductId"),
                        Quantity = ParseInt(values, "Line2Quantity")
                    }
                ]
            };

            AllureJson.AttachObject(
                "Update shopping cart request (from Gherkin table)",
                _request,
                _apiContext.JsonOptions);
        }

        [When("I submit the update shopping cart request")]
        public async Task WhenISubmitTheUpdateShoppingCartRequest()
        {
            _request.ShouldNotBeNull();

            _apiContext.Response = await _apiContext.HttpClient.PutAsJsonAsync(
                $"/shopping-carts/{_clientId}",
                _request,
                _apiContext.JsonOptions);

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
            shoppingCart!.Id.ShouldNotBe(Guid.Empty);
            shoppingCart.ClientId.ShouldBe(_clientId);
            shoppingCart.CreatedAt.ShouldNotBe(default);
            shoppingCart.UpdatedAt.ShouldNotBe(default);
            shoppingCart.UpdatedAt.ShouldBeGreaterThanOrEqualTo(shoppingCart.CreatedAt);
            shoppingCart.Lines.Count.ShouldBe(ParseInt(expected, "LinesCount"));

            var expectedLines = new[]
            {
                new ShoppingCartLineResponseDto
                {
                    ProductId = ParseGuid(expected, "Line1ProductId"),
                    Quantity = ParseInt(expected, "Line1Quantity")
                },
                new ShoppingCartLineResponseDto
                {
                    ProductId = ParseGuid(expected, "Line2ProductId"),
                    Quantity = ParseInt(expected, "Line2Quantity")
                }
            };

            shoppingCart.Lines.ShouldBe(expectedLines, ignoreOrder: true);
        }

        private async Task<T?> DeserializeResponse<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content, _apiContext.JsonOptions);
        }

        private static Dictionary<string, string> ParseExpectedTable(Table table)
        {
            return table.Rows.ToDictionary(
                row => row["Field"],
                row => row["Value"],
                StringComparer.OrdinalIgnoreCase);
        }

        private static string GetRequiredValue(IReadOnlyDictionary<string, string> values, string key)
        {
            if (!values.TryGetValue(key, out var value))
            {
                throw new InvalidOperationException($"Missing '{key}' value in shopping cart expected result table.");
            }

            return value;
        }

        private static HttpStatusCode ParseStatusCode(IReadOnlyDictionary<string, string> values, string key)
        {
            return (HttpStatusCode)ParseInt(values, key);
        }

        private static int ParseInt(IReadOnlyDictionary<string, string> values, string key)
        {
            return int.Parse(GetRequiredValue(values, key), CultureInfo.InvariantCulture);
        }

        private static Guid ParseGuid(IReadOnlyDictionary<string, string> values, string key)
        {
            return Guid.Parse(GetRequiredValue(values, key));
        }
    }
}
