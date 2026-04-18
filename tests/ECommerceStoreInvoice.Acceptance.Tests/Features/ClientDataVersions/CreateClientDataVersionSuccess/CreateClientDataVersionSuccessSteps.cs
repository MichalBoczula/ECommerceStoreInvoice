using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using Reqnroll;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.ClientDataVersions.CreateClientDataVersionSuccess
{
    [Binding]
    public sealed class CreateClientDataVersionSuccessSteps
    {
        private readonly ScenarioApiContext _apiContext;
        private Guid _clientId;
        private CreateClientDataVersionRequestDto? _request;

        public CreateClientDataVersionSuccessSteps(ScenarioApiContext apiContext)
        {
            _apiContext = apiContext;
        }

        [Given("I have a valid client id for client data version creation")]
        public void GivenIHaveAValidClientIdForClientDataVersionCreation()
        {
            _clientId = Guid.NewGuid();

            AllureJson.AttachObject(
                "Create client data version setup",
                new { ClientId = _clientId },
                _apiContext.JsonOptions);
        }

        [Given("I have a valid create client data version request")]
        public void GivenIHaveAValidCreateClientDataVersionRequest()
        {
            _request = new CreateClientDataVersionRequestDto
            {
                ClientName = "John Doe",
                PostalCode = "00-001",
                City = "New York",
                Street = "Main Street",
                BuildingNumber = "10A",
                ApartmentNumber = "5",
                PhoneNumber = "123456789",
                PhonePrefix = "48",
                AddressEmail = "john.doe@test.com"
            };

            AllureJson.AttachObject(
                "Create client data version request",
                _request,
                _apiContext.JsonOptions);
        }

        [When("I submit the create client data version request")]
        public async Task WhenISubmitTheCreateClientDataVersionRequest()
        {
            _request.ShouldNotBeNull();

            _apiContext.Response = await _apiContext.HttpClient.PostAsJsonAsync($"/client-data-versions/{_clientId}", _request, _apiContext.JsonOptions);

            var body = await _apiContext.Response.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Response JSON ({(int)_apiContext.Response.StatusCode})", body);
        }

        [Then("the client data version is created successfully")]
        public async Task ThenTheClientDataVersionIsCreatedSuccessfully(Table table)
        {
            var expected = ParseExpectedTable(table);

            AllureJson.AttachObject("Expected result table", expected, _apiContext.JsonOptions);

            _apiContext.Response.ShouldNotBeNull();
            _apiContext.Response!.StatusCode.ShouldBe(ParseStatusCode(expected, "StatusCode"));

            var clientDataVersion = await DeserializeResponse<ClientDataVersionResponseDto>(_apiContext.Response);
            clientDataVersion.ShouldNotBeNull();

            if (TryGetBool(expected, "HasId", out var hasId))
            {
                if (hasId)
                {
                    clientDataVersion!.Id.ShouldNotBe(Guid.Empty);
                }
                else
                {
                    clientDataVersion!.Id.ShouldBe(Guid.Empty);
                }
            }

            if (TryGetBool(expected, "HasClientId", out var hasClientId))
            {
                if (hasClientId)
                {
                    clientDataVersion!.ClientId.ShouldBe(_clientId);
                }
                else
                {
                    clientDataVersion!.ClientId.ShouldBe(Guid.Empty);
                }
            }

            clientDataVersion!.ClientName.ShouldBe(GetExpectedValue(expected, "ClientName", clientDataVersion.ClientName));
            clientDataVersion.PostalCode.ShouldBe(GetExpectedValue(expected, "PostalCode", clientDataVersion.PostalCode));
            clientDataVersion.City.ShouldBe(GetExpectedValue(expected, "City", clientDataVersion.City));
            clientDataVersion.Street.ShouldBe(GetExpectedValue(expected, "Street", clientDataVersion.Street));
            clientDataVersion.BuildingNumber.ShouldBe(GetExpectedValue(expected, "BuildingNumber", clientDataVersion.BuildingNumber));
            clientDataVersion.ApartmentNumber.ShouldBe(GetExpectedValue(expected, "ApartmentNumber", clientDataVersion.ApartmentNumber));
            clientDataVersion.PhoneNumber.ShouldBe(GetExpectedValue(expected, "PhoneNumber", clientDataVersion.PhoneNumber));
            clientDataVersion.PhonePrefix.ShouldBe(GetExpectedValue(expected, "PhonePrefix", clientDataVersion.PhonePrefix));
            clientDataVersion.AddressEmail.ShouldBe(GetExpectedValue(expected, "AddressEmail", clientDataVersion.AddressEmail));

            if (TryGetBool(expected, "HasCreatedAt", out var hasCreatedAt))
            {
                if (hasCreatedAt)
                {
                    clientDataVersion.CreatedAt.ShouldNotBe(default);
                }
                else
                {
                    clientDataVersion.CreatedAt.ShouldBe(default);
                }
            }
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
                throw new InvalidOperationException($"Missing '{key}' value in client data version expected result table.");
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
