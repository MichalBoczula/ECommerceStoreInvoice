using ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using Reqnroll;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.ClientDataVersions.GetClientDataVersionSuccess
{
    [Binding]
    public sealed class GetClientDataVersionSuccessSteps
    {
        private readonly ScenarioApiContext _apiContext;
        private Guid _clientId;
        private CreateClientDataVersionRequestDto? _request;

        public GetClientDataVersionSuccessSteps(ScenarioApiContext apiContext)
        {
            _apiContext = apiContext;
        }

        [Given("I have an existing client data version for retrieval")]
        public async Task GivenIHaveAnExistingClientDataVersionForRetrieval(Table table)
        {
            var requestData = ParseExpectedTable(table);
            _clientId = Guid.NewGuid();
            _request = BuildCreateClientDataVersionRequest(requestData);

            AllureJson.AttachObject(
                "Create client data version setup request",
                new
                {
                    ClientId = _clientId,
                    Request = _request
                },
                _apiContext.JsonOptions);

            var createResponse = await _apiContext.HttpClient.PostAsJsonAsync($"/client-data-versions/{_clientId}", _request, _apiContext.JsonOptions);
            createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            var createBody = await createResponse.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Setup response JSON ({(int)createResponse.StatusCode})", createBody);
        }

        [When("I request the client data version by client id")]
        public async Task WhenIRequestTheClientDataVersionByClientId()
        {
            _apiContext.Response = await _apiContext.HttpClient.GetAsync($"/client-data-versions/client/{_clientId}");

            var body = await _apiContext.Response.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Response JSON ({(int)_apiContext.Response.StatusCode})", body);
        }

        [Then("the client data version is returned successfully")]
        public async Task ThenTheClientDataVersionIsReturnedSuccessfully(Table table)
        {
            var expected = ParseExpectedTable(table);
            var expectedResponseObject = BuildExpectedClientDataVersionResponse(expected);

            AllureJson.AttachObject("Expected response object", expectedResponseObject, _apiContext.JsonOptions);

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

        private static CreateClientDataVersionRequestDto BuildCreateClientDataVersionRequest(IReadOnlyDictionary<string, string> values)
        {
            return new CreateClientDataVersionRequestDto
            {
                ClientName = GetRequiredValue(values, "ClientName"),
                PostalCode = GetRequiredValue(values, "PostalCode"),
                City = GetRequiredValue(values, "City"),
                Street = GetRequiredValue(values, "Street"),
                BuildingNumber = GetRequiredValue(values, "BuildingNumber"),
                ApartmentNumber = GetRequiredValue(values, "ApartmentNumber"),
                PhoneNumber = GetRequiredValue(values, "PhoneNumber"),
                PhonePrefix = GetRequiredValue(values, "PhonePrefix"),
                AddressEmail = GetRequiredValue(values, "AddressEmail")
            };
        }

        private static object BuildExpectedClientDataVersionResponse(IReadOnlyDictionary<string, string> values)
        {
            return new
            {
                StatusCode = ParseStatusCode(values, "StatusCode"),
                HasId = TryGetBool(values, "HasId", out var hasId) && hasId,
                HasClientId = TryGetBool(values, "HasClientId", out var hasClientId) && hasClientId,
                ClientName = GetExpectedValue(values, "ClientName", string.Empty),
                PostalCode = GetExpectedValue(values, "PostalCode", string.Empty),
                City = GetExpectedValue(values, "City", string.Empty),
                Street = GetExpectedValue(values, "Street", string.Empty),
                BuildingNumber = GetExpectedValue(values, "BuildingNumber", string.Empty),
                ApartmentNumber = GetExpectedValue(values, "ApartmentNumber", string.Empty),
                PhoneNumber = GetExpectedValue(values, "PhoneNumber", string.Empty),
                PhonePrefix = GetExpectedValue(values, "PhonePrefix", string.Empty),
                AddressEmail = GetExpectedValue(values, "AddressEmail", string.Empty),
                HasCreatedAt = TryGetBool(values, "HasCreatedAt", out var hasCreatedAt) && hasCreatedAt
            };
        }
    }
}
