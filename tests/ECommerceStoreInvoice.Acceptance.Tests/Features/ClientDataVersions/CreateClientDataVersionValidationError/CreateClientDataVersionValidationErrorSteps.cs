using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions;
using Reqnroll;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.ClientDataVersions.CreateClientDataVersionValidationError
{
    [Binding]
    public sealed class CreateClientDataVersionValidationErrorSteps
    {
        private readonly ScenarioApiContext _apiContext;
        private Guid _clientId;
        private CreateClientDataVersionRequestDto? _request;

        public CreateClientDataVersionValidationErrorSteps(ScenarioApiContext apiContext)
        {
            _apiContext = apiContext;
        }

        [Given("I have an invalid client id for client data version creation")]
        public void GivenIHaveAnInvalidClientIdForClientDataVersionCreation()
        {
            _clientId = Guid.Empty;

            AllureJson.AttachObject(
                "Create client data version invalid setup",
                new { ClientId = _clientId },
                _apiContext.JsonOptions);
        }

        [Given("I have a create client data version request for validation error")]
        public void GivenIHaveACreateClientDataVersionRequestForValidationError()
        {
            _request = new CreateClientDataVersionRequestDto
            {
                ClientName = "John Doe",
                PostalCode = "00-001",
                City = "NewYork",
                Street = "Main.St",
                BuildingNumber = "10A",
                ApartmentNumber = "5",
                PhoneNumber = "123456789",
                PhonePrefix = "48",
                AddressEmail = "john.doe@test.com"
            };

            AllureJson.AttachObject(
                "Create client data version request (validation error)",
                _request,
                _apiContext.JsonOptions);
        }

        [When("I submit the create client data version request with invalid data")]
        public async Task WhenISubmitTheCreateClientDataVersionRequestWithInvalidData()
        {
            _request.ShouldNotBeNull();

            _apiContext.Response = await _apiContext.HttpClient.PostAsJsonAsync($"/client-data-versions/{_clientId}", _request, _apiContext.JsonOptions);

            var body = await _apiContext.Response.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Response JSON ({(int)_apiContext.Response.StatusCode})", body);
        }

        [Then("problem details are returned for create client data version validation error")]
        public async Task ThenProblemDetailsAreReturnedForCreateClientDataVersionValidationError(Table table)
        {
            var expected = ParseExpectedTable(table);

            AllureJson.AttachObject(
                "Expected create client data version validation error",
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
