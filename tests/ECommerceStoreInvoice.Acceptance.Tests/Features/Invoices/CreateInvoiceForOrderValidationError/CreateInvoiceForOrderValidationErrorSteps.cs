using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using Reqnroll;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Invoices.CreateInvoiceForOrderValidationError
{
    [Binding]
    public sealed class CreateInvoiceForOrderValidationErrorSteps
    {
        private readonly ScenarioApiContext _apiContext;
        private Guid _clientId;
        private Guid _orderId;

        public CreateInvoiceForOrderValidationErrorSteps(ScenarioApiContext apiContext)
        {
            _apiContext = apiContext;
        }

        [Given("I have invoice creation identifiers")]
        public void GivenIHaveInvoiceCreationIdentifiers(Table table)
        {
            var values = ParseExpectedTable(table);
            _clientId = Guid.Parse(GetRequiredValue(values, "ClientId"));
            _orderId = Guid.Parse(GetRequiredValue(values, "OrderId"));

            var requestObject = new
            {
                ClientId = _clientId,
                OrderId = _orderId
            };

            AllureJson.AttachObject(
                "Create invoice validation request",
                requestObject,
                _apiContext.JsonOptions);
        }

        [When("I submit the create invoice request with invalid order id")]
        public async Task WhenISubmitTheCreateInvoiceRequestWithInvalidOrderId()
        {
            _apiContext.Response = await _apiContext.HttpClient.PostAsync($"/invoices/{_clientId}/{_orderId}", content: null);

            var body = await _apiContext.Response.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Response JSON ({(int)_apiContext.Response.StatusCode})", body);
        }

        [Then("problem details are returned for create invoice for order validation error")]
        public async Task ThenProblemDetailsAreReturnedForCreateInvoiceForOrderValidationError(Table table)
        {
            var expected = ParseExpectedTable(table);

            AllureJson.AttachObject(
                "Expected create invoice for order validation error",
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
