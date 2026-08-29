using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts;
using Reqnroll;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.ShoppingCarts.GetShoppingCartSuccess
{
    [Binding]
    public sealed class GetShoppingCartSuccessSteps
    {
        private readonly ScenarioApiContext _apiContext;
        private Guid _clientId;
        private ShoppingCartResponseDto? _shoppingCartResponse;

        public GetShoppingCartSuccessSteps(ScenarioApiContext apiContext)
        {
            _apiContext = apiContext;
        }

        [Given("I have an existing shopping cart for retrieval")]
        public async Task GivenIHaveAnExistingShoppingCartForRetrieval()
        {
            _clientId = Guid.NewGuid();

            AllureJson.AttachObject(
                "Get shopping cart setup request",
                new { ClientId = _clientId },
                _apiContext.JsonOptions);

            var createResponse = await _apiContext.HttpClient.PostAsync($"/shopping-carts/{_clientId}", content: null);
            createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            var createBody = await createResponse.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Setup response JSON ({(int)createResponse.StatusCode})", createBody);
        }

        [Given("the get shopping cart request data is")]
        public void GivenTheGetShoppingCartRequestDataIs(Table table)
        {
            var request = BuildRequestFromTable(table);
            var resolvedRequest = new
            {
                request.Method,
                Endpoint = $"{request.Endpoint}/{_clientId}",
                ClientId = _clientId
            };

            AllureJson.AttachObject("Get shopping cart request object", resolvedRequest, _apiContext.JsonOptions);
        }

        [When("I request the shopping cart by client id")]
        public async Task WhenIRequestTheShoppingCartByClientId()
        {
            _apiContext.Response = await _apiContext.HttpClient.GetAsync($"/shopping-carts/client/{_clientId}");

            var body = await _apiContext.Response.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Response JSON ({(int)_apiContext.Response.StatusCode})", body);
        }

        [Then("the shopping cart is returned successfully")]
        public async Task ThenTheShoppingCartIsReturnedSuccessfully(Table table)
        {
            var expected = BuildExpectedResponseFromTable(table);

            _apiContext.Response.ShouldNotBeNull();
            _apiContext.Response!.StatusCode.ShouldBe((HttpStatusCode)expected.StatusCode);

            _shoppingCartResponse = await DeserializeResponse<ShoppingCartResponseDto>(_apiContext.Response);
            _shoppingCartResponse.ShouldNotBeNull();

            _shoppingCartResponse!.Id.ShouldNotBe(Guid.Empty);
            _shoppingCartResponse.ClientId.ShouldBe(_clientId);
            (_shoppingCartResponse.CreatedAt != default).ShouldBe(expected.HasCreatedAt);
            (_shoppingCartResponse.UpdatedAt != default).ShouldBe(expected.HasUpdatedAt);
            _shoppingCartResponse.Lines.Count.ShouldBe(expected.LinesCount);
        }

        [Then("the shopping cart response payload is")]
        public void ThenTheShoppingCartResponsePayloadIs(Table table)
        {
            _shoppingCartResponse.ShouldNotBeNull();
            var expected = BuildExpectedPayloadFromTable(table);

            _shoppingCartResponse!.Id.ShouldNotBe(Guid.Empty);
            _shoppingCartResponse.ClientId.ShouldBe(_clientId);
            _shoppingCartResponse.CreatedAt.ShouldNotBe(default);
            _shoppingCartResponse.UpdatedAt.ShouldNotBe(default);

            var expectedLines = JsonSerializer.Deserialize<List<ShoppingCartLineResponseDto>>(
                expected.LinesJson,
                _apiContext.JsonOptions) ?? [];
            _shoppingCartResponse.Lines.ShouldBe(expectedLines);

            AllureJson.AttachObject("Get shopping cart response object", _shoppingCartResponse, _apiContext.JsonOptions);
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
                GetRequiredValue(values, "Method"),
                GetRequiredValue(values, "Endpoint"));
        }

        private static ExpectedResponseTableData BuildExpectedResponseFromTable(Table table)
        {
            var values = ParseTable(table);
            return new ExpectedResponseTableData(
                int.Parse(GetRequiredValue(values, "StatusCode"), CultureInfo.InvariantCulture),
                bool.Parse(GetRequiredValue(values, "HasCreatedAt")),
                bool.Parse(GetRequiredValue(values, "HasUpdatedAt")),
                int.Parse(GetRequiredValue(values, "LinesCount"), CultureInfo.InvariantCulture));
        }

        private static ExpectedPayloadTableData BuildExpectedPayloadFromTable(Table table)
        {
            var values = ParseTable(table);
            return new ExpectedPayloadTableData(GetRequiredValue(values, "Lines"));
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
                throw new InvalidOperationException($"Missing '{key}' value in shopping cart expected result table.");
            }

            return value;
        }

        private sealed record RequestTableData(string Method, string Endpoint);

        private sealed record ExpectedResponseTableData(
            int StatusCode,
            bool HasCreatedAt,
            bool HasUpdatedAt,
            int LinesCount);

        private sealed record ExpectedPayloadTableData(string LinesJson);
    }
}
