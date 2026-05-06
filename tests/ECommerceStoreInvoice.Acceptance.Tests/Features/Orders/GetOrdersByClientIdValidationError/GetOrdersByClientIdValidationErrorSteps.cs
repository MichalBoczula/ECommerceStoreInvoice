using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using Reqnroll;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Orders.GetOrdersByClientIdValidationError
{
    [Binding]
    public sealed class GetOrdersByClientIdValidationErrorSteps
    {
        private readonly ScenarioApiContext _apiContext;
        private Guid _clientId;
        private IReadOnlyCollection<ValidationErrorExpectation> _expectedErrors = [];

        public GetOrdersByClientIdValidationErrorSteps(ScenarioApiContext apiContext)
        {
            _apiContext = apiContext;
        }

        [Given("I have an invalid client id for orders retrieval")]
        public void GivenIHaveAnInvalidClientIdForOrdersRetrieval(Table table)
        {
            var request = ParseRequestTable(table);
            _clientId = request.ClientId;

            AllureJson.AttachObject(
                "Get orders validation request",
                request,
                _apiContext.JsonOptions);
        }

        [When("I request orders by invalid client id")]
        public async Task WhenIRequestOrdersByInvalidClientId()
        {
            _apiContext.Response = await _apiContext.HttpClient.GetAsync($"/orders/client/{_clientId}");

            var body = await _apiContext.Response.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Response JSON ({(int)_apiContext.Response.StatusCode})", body);
        }

        [Then("problem details are returned for get orders by client id validation error")]
        public async Task ThenProblemDetailsAreReturnedForGetOrdersByClientIdValidationError(Table table)
        {
            var expected = ParseExpectedTable(table);

            AllureJson.AttachObject(
                "Expected get orders validation error",
                expected,
                _apiContext.JsonOptions);

            _apiContext.Response.ShouldNotBeNull();
            _apiContext.Response!.StatusCode.ShouldBe(ParseStatusCode(expected, "StatusCode"));

            var problemDetails = await DeserializeResponse<ApiProblemDetails>(_apiContext.Response);
            problemDetails.ShouldNotBeNull();

            problemDetails!.Title.ShouldBe(GetRequiredValue(expected, "Title"));
            problemDetails.Detail.ShouldBe(GetRequiredValue(expected, "Detail"));
            problemDetails.Type.ShouldBe(GetRequiredValue(expected, "Type"));
            problemDetails.Instance.ShouldBe(GetRequiredValue(expected, "Instance"));

            var errors = problemDetails.Errors.ToList();
            errors.Count.ShouldBe(ParseInt(expected, "ErrorsCount"));
            errors.ShouldNotBeEmpty();
            errors[0].Message.ShouldBe(GetRequiredValue(expected, "FirstErrorMessage"));

            if (_expectedErrors.Count > 0)
            {
                errors.Count.ShouldBe(_expectedErrors.Count);
                foreach (var expectedError in _expectedErrors)
                {
                    errors.Any(e => e.Field == expectedError.Field && e.Message == expectedError.Message).ShouldBeTrue();
                }
            }
        }

        [Then("the validation error response JSON contains")]
        public void ThenTheValidationErrorResponseJsonContains(Table table)
        {
            _expectedErrors = table.Rows
                .Select(row => new ValidationErrorExpectation(row["Field"], row["Message"]))
                .ToList();

            AllureJson.AttachObject(
                "Expected validation errors (JSON) ",
                _expectedErrors,
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

        private static GetOrdersByClientIdValidationRequest ParseRequestTable(Table table)
        {
            var row = table.Rows.Single();
            return new GetOrdersByClientIdValidationRequest(Guid.Parse(row["ClientId"]));
        }

        private sealed record GetOrdersByClientIdValidationRequest(Guid ClientId);

        private sealed record ValidationErrorExpectation(string Field, string Message);
    }
}
