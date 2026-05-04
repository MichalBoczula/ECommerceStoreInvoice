using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions;
using Reqnroll;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Invoices.CreateInvoiceForOrderNotFound
{
    [Binding]
    public sealed class CreateInvoiceForOrderNotFoundSteps
    {
        private readonly ScenarioApiContext _apiContext;
        private Guid _clientId;
        private Guid _orderId;

        public CreateInvoiceForOrderNotFoundSteps(ScenarioApiContext apiContext)
        {
            _apiContext = apiContext;
        }

        [Given("I have an existing client and a non-existing order id for invoice creation")]
        public async Task GivenIHaveAnExistingClientAndANonExistingOrderIdForInvoiceCreation()
        {
            _clientId = Guid.NewGuid();
            _orderId = Guid.NewGuid();

            var clientDataVersionRequest = new CreateClientDataVersionRequestDto
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
                "Create client data version setup request",
                new { ClientId = _clientId, NonExistingOrderId = _orderId, Request = clientDataVersionRequest },
                _apiContext.JsonOptions);

            var createClientDataVersionResponse = await _apiContext.HttpClient.PostAsJsonAsync($"/client-data-versions/{_clientId}", clientDataVersionRequest, _apiContext.JsonOptions);
            createClientDataVersionResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            var createClientDataVersionBody = await createClientDataVersionResponse.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Create client data version response JSON ({(int)createClientDataVersionResponse.StatusCode})", createClientDataVersionBody);
        }

        [When("I submit create invoice for a non-existing order request")]
        public async Task WhenISubmitCreateInvoiceForANonExistingOrderRequest()
        {
            _apiContext.Response = await _apiContext.HttpClient.PostAsync($"/invoices/{_clientId}/{_orderId}", content: null);

            var body = await _apiContext.Response.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Response JSON ({(int)_apiContext.Response.StatusCode})", body);
        }

        [Then("problem details are returned for create invoice order not found")]
        public async Task ThenProblemDetailsAreReturnedForCreateInvoiceOrderNotFound(Table table)
        {
            var expected = ParseExpectedTable(table);
            AllureJson.AttachObject("Expected create invoice order not found result", expected, _apiContext.JsonOptions);

            _apiContext.Response.ShouldNotBeNull();
            _apiContext.Response!.StatusCode.ShouldBe(ParseStatusCode(expected, "StatusCode"));

            var problemDetails = await DeserializeResponse<NotFoundProblemDetails>(_apiContext.Response);
            problemDetails.ShouldNotBeNull();

            problemDetails!.Title.ShouldBe(GetRequiredValue(expected, "Title"));
            problemDetails.Type.ShouldBe(GetRequiredValue(expected, "Type"));

            if (TryGetBool(expected, "HasDetail", out var hasDetail))
            {
                if (hasDetail)
                {
                    problemDetails.Detail.ShouldNotBeNullOrWhiteSpace();
                    problemDetails.Detail!.ShouldContain(_orderId.ToString(), Case.Insensitive);
                    problemDetails.Detail.ShouldContain("Order", Case.Insensitive);
                }
                else
                {
                    problemDetails.Detail.ShouldBeNullOrWhiteSpace();
                }
            }

            var expectedInstance = GetRequiredValue(expected, "Instance")
                .Replace("{clientId}", _clientId.ToString(), StringComparison.OrdinalIgnoreCase)
                .Replace("{orderId}", _orderId.ToString(), StringComparison.OrdinalIgnoreCase);
            problemDetails.Instance.ShouldBe(expectedInstance);

            if (TryGetBool(expected, "HasTraceId", out var hasTraceId))
            {
                if (hasTraceId)
                {
                    problemDetails.TraceId.ShouldNotBeNullOrWhiteSpace();
                }
                else
                {
                    problemDetails.TraceId.ShouldBeNullOrWhiteSpace();
                }
            }

            AllureJson.AttachObject("Actual create invoice order not found result", problemDetails, _apiContext.JsonOptions);
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
