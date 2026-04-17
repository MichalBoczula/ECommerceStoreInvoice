using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using Reqnroll;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.ShoppingCarts.UpdateShoppingCartValidationError
{
    [Binding]
    public sealed class UpdateShoppingCartValidationErrorSteps
    {
        private readonly ScenarioApiContext _apiContext;
        private Guid _clientId;
        private UpdateShoppingCartRequestDto? _request;

        public UpdateShoppingCartValidationErrorSteps(ScenarioApiContext apiContext)
        {
            _apiContext = apiContext;
        }

        [Given("I have an existing shopping cart for invalid update")]
        public async Task GivenIHaveAnExistingShoppingCartForInvalidUpdate()
        {
            _clientId = Guid.NewGuid();

            AllureJson.AttachObject(
                "Update shopping cart validation setup",
                new { ClientId = _clientId },
                _apiContext.JsonOptions);

            var createResponse = await _apiContext.HttpClient.PostAsync($"/shopping-carts/{_clientId}", content: null);
            createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            var createBody = await createResponse.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Setup response JSON ({(int)createResponse.StatusCode})", createBody);
        }

        [Given("I have an invalid update shopping cart request")]
        public void GivenIHaveAnInvalidUpdateShoppingCartRequest(Table table)
        {
            var lines = table.Rows.Select(row => new ShoppingCartLineRequestDto
            {
                ProductId = Guid.Parse(row["ProductId"]),
                Name = row["Name"],
                Brand = row["Brand"],
                UnitPriceAmount = decimal.Parse(row["UnitPriceAmount"], CultureInfo.InvariantCulture),
                UnitPriceCurrency = row["UnitPriceCurrency"],
                Quantity = int.Parse(row["Quantity"], CultureInfo.InvariantCulture)
            }).ToList();

            _request = new UpdateShoppingCartRequestDto { Lines = lines };

            AllureJson.AttachObject(
                "Update shopping cart invalid request",
                _request,
                _apiContext.JsonOptions);
        }

        [When("I submit the invalid update shopping cart request")]
        public async Task WhenISubmitTheInvalidUpdateShoppingCartRequest()
        {
            _request.ShouldNotBeNull();

            _apiContext.Response = await _apiContext.HttpClient.PutAsJsonAsync($"/shopping-carts/{_clientId}", _request, _apiContext.JsonOptions);

            var body = await _apiContext.Response.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Response JSON ({(int)_apiContext.Response.StatusCode})", body);
        }

        [Then("problem details are returned for update shopping cart validation error")]
        public async Task ThenProblemDetailsAreReturnedForUpdateShoppingCartValidationError(Table table)
        {
            var expected = ParseExpectedTable(table);

            AllureJson.AttachObject(
                "Expected update shopping cart validation error",
                expected,
                _apiContext.JsonOptions);

            _apiContext.Response.ShouldNotBeNull();
            _apiContext.Response!.StatusCode.ShouldBe(ParseStatusCode(expected, "StatusCode"));

            var problemDetails = await DeserializeResponse<ApiProblemDetails>(_apiContext.Response);
            problemDetails.ShouldNotBeNull();

            problemDetails!.Title.ShouldBe(GetRequiredValue(expected, "Title"));
            problemDetails.Detail.ShouldBe(GetRequiredValue(expected, "Detail"));
            problemDetails.Type.ShouldBe(GetRequiredValue(expected, "Type"));

            var expectedInstance = GetRequiredValue(expected, "Instance").Replace("{clientId}", _clientId.ToString(), StringComparison.OrdinalIgnoreCase);
            problemDetails.Instance.ShouldBe(expectedInstance);

            var errors = problemDetails.Errors.ToList();
            errors.Count.ShouldBe(ParseInt(expected, "ErrorsCount"));
            errors.ShouldNotBeEmpty();
            errors[0].Message.ShouldBe(GetRequiredValue(expected, "FirstErrorMessage"));
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

        private static int ParseInt(IReadOnlyDictionary<string, string> values, string key)
        {
            var value = GetRequiredValue(values, key);
            return int.Parse(value, CultureInfo.InvariantCulture);
        }
    }
}
