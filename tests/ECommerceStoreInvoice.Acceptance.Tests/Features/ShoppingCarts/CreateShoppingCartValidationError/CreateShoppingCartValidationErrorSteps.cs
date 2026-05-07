using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using Reqnroll;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.ShoppingCarts.CreateShoppingCartValidationError
{
    [Binding]
    public sealed class CreateShoppingCartValidationErrorSteps
    {
        private readonly ScenarioApiContext _apiContext;
        private Guid _clientId;

        public CreateShoppingCartValidationErrorSteps(ScenarioApiContext apiContext)
        {
            _apiContext = apiContext;
        }

        [Given("I have an invalid client id for shopping cart creation")]
        public void GivenIHaveAnInvalidClientIdForShoppingCartCreation()
        {
            _clientId = Guid.Empty;

            AllureJson.AttachObject(
                "Create shopping cart invalid request",
                new { ClientId = _clientId },
                _apiContext.JsonOptions);
        }

        [When("I submit the create shopping cart request with invalid data")]
        public async Task WhenISubmitTheCreateShoppingCartRequestWithInvalidData(Table table)
        {
            var requestDefinition = ParseExpectedTable(table);
            var requestObject = BuildCreateShoppingCartRequestObject(requestDefinition, _clientId);

            AllureJson.AttachObject(
                "Create shopping cart validation request object",
                requestObject,
                _apiContext.JsonOptions);

            _apiContext.Response = await _apiContext.HttpClient.PostAsync($"/shopping-carts/{_clientId}", content: null);

            var body = await _apiContext.Response.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Response JSON ({(int)_apiContext.Response.StatusCode})", body);
        }

        [Then("problem details are returned for create shopping cart validation error")]
        public async Task ThenProblemDetailsAreReturnedForCreateShoppingCartValidationError(Table table)
        {
            var expected = ParseExpectedTable(table);

            AllureJson.AttachObject(
                "Expected create shopping cart validation error",
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

            var expectedResponseObject = BuildCreateShoppingCartValidationErrorResponseObject(expected, _clientId);
            AllureJson.AttachObject(
                "Create shopping cart validation expected response object",
                expectedResponseObject,
                _apiContext.JsonOptions);
        }

        private static object BuildCreateShoppingCartRequestObject(IReadOnlyDictionary<string, string> values, Guid clientId)
        {
            return new
            {
                Method = GetRequiredValue(values, "Method"),
                Path = GetRequiredValue(values, "PathTemplate").Replace("{clientId}", clientId.ToString(), StringComparison.OrdinalIgnoreCase),
                ContentType = GetRequiredValue(values, "ContentType"),
                Body = (object?)null
            };
        }

        private static object BuildCreateShoppingCartValidationErrorResponseObject(IReadOnlyDictionary<string, string> values, Guid clientId)
        {
            return new
            {
                status = ParseInt(values, "StatusCode"),
                title = GetRequiredValue(values, "Title"),
                detail = GetRequiredValue(values, "Detail"),
                type = GetRequiredValue(values, "Type"),
                instance = GetRequiredValue(values, "Instance").Replace("{clientId}", clientId.ToString(), StringComparison.OrdinalIgnoreCase),
                errors = new[]
                {
                    new
                    {
                        message = GetRequiredValue(values, "FirstErrorMessage")
                    }
                }
            };
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
