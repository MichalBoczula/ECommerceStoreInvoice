using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using Reqnroll;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Orders.CreateOrderValidationError
{
    [Binding]
    public sealed class CreateOrderValidationErrorSteps
    {
        private readonly ScenarioApiContext _apiContext;
        private Guid _clientId;

        public CreateOrderValidationErrorSteps(ScenarioApiContext apiContext)
        {
            _apiContext = apiContext;
        }

        [Given("I have a client with an empty shopping cart for order creation")]
        public async Task GivenIHaveAClientWithAnEmptyShoppingCartForOrderCreation()
        {
            _clientId = Guid.NewGuid();

            AllureJson.AttachObject(
                "Create order validation setup request",
                new { ClientId = _clientId },
                _apiContext.JsonOptions);

            var createShoppingCartResponse = await _apiContext.HttpClient.PostAsync($"/shopping-carts/{_clientId}", content: null);
            var createShoppingCartBody = await createShoppingCartResponse.Content.ReadAsStringAsync();

            AllureJson.AttachRawJson($"Create shopping cart response JSON ({(int)createShoppingCartResponse.StatusCode})", createShoppingCartBody);
            createShoppingCartResponse.StatusCode.ShouldBe(HttpStatusCode.OK, createShoppingCartBody);
        }

        [When("I submit the create order request for the empty shopping cart")]
        public async Task WhenISubmitTheCreateOrderRequestForTheEmptyShoppingCart()
        {
            _apiContext.Response = await _apiContext.HttpClient.PostAsync($"/orders/{_clientId}", content: null);

            var body = await _apiContext.Response.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Response JSON ({(int)_apiContext.Response.StatusCode})", body);
        }

        [Then("problem details are returned for create order validation error")]
        public async Task ThenProblemDetailsAreReturnedForCreateOrderValidationError(Table table)
        {
            var expected = ParseExpectedTable(table);

            AllureJson.AttachObject(
                "Expected create order validation error",
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
