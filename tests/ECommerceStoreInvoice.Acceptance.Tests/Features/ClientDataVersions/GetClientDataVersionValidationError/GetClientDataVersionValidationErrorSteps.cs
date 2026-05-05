using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using Reqnroll;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.ClientDataVersions.GetClientDataVersionValidationError
{
    [Binding]
    public sealed class GetClientDataVersionValidationErrorSteps
    {
        private readonly ScenarioApiContext _apiContext;
        private Guid _clientId;
        private GetClientDataVersionValidationErrorRequest? _request;

        public GetClientDataVersionValidationErrorSteps(ScenarioApiContext apiContext)
        {
            _apiContext = apiContext;
        }

        [Given("I have an invalid client data version request")]
        public void GivenIHaveAnInvalidClientDataVersionRequest(Table table)
        {
            var requestTable = ParseExpectedTable(table);
            _request = new GetClientDataVersionValidationErrorRequest
            {
                ClientId = Guid.Parse(GetRequiredValue(requestTable, "ClientId"))
            };

            _clientId = _request.ClientId;

            AllureJson.AttachObject(
                "Get client data version validation request",
                _request,
                _apiContext.JsonOptions);
        }

        [When("I request the client data version by invalid client id")]
        public async Task WhenIRequestTheClientDataVersionByInvalidClientId()
        {
            _apiContext.Response = await _apiContext.HttpClient.GetAsync($"/client-data-versions/client/{_clientId}");

            var body = await _apiContext.Response.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Response JSON ({(int)_apiContext.Response.StatusCode})", body);
        }

        [Then("problem details are returned for get client data version validation error")]
        public async Task ThenProblemDetailsAreReturnedForGetClientDataVersionValidationError(Table table)
        {
            var expected = ParseExpectedTable(table);
            var expectedResponse = BuildExpectedResponse(expected);

            AllureJson.AttachObject(
                "Expected get client data version validation error",
                expectedResponse,
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

        private static object BuildExpectedResponse(IReadOnlyDictionary<string, string> expected)
        {
            return new
            {
                StatusCode = int.Parse(GetRequiredValue(expected, "StatusCode"), CultureInfo.InvariantCulture),
                Title = GetRequiredValue(expected, "Title"),
                Detail = GetRequiredValue(expected, "Detail"),
                Type = GetRequiredValue(expected, "Type"),
                Instance = GetRequiredValue(expected, "Instance"),
                Errors = new[]
                {
                    new
                    {
                        Message = GetRequiredValue(expected, "FirstErrorMessage")
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

        private sealed class GetClientDataVersionValidationErrorRequest
        {
            public Guid ClientId { get; set; }
        }
    }
}
