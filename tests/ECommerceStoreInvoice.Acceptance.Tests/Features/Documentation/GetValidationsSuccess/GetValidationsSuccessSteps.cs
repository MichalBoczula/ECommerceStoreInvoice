using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using Reqnroll;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Documentation.GetValidationsSuccess
{
    [Binding]
    public sealed class GetValidationsSuccessSteps
    {
        private readonly ScenarioApiContext _apiContext;

        public GetValidationsSuccessSteps(ScenarioApiContext apiContext)
        {
            _apiContext = apiContext;
        }

        [Given("I am ready to retrieve validation documentation")]
        public void GivenIAmReadyToRetrieveValidationDocumentation()
        {
            AllureJson.AttachObject(
                "Get validation documentation request",
                new { Endpoint = "/documentation/validations", Method = "GET" },
                _apiContext.JsonOptions);
        }

        [When("I request validation documentation")]
        public async Task WhenIRequestValidationDocumentation()
        {
            _apiContext.Response = await _apiContext.HttpClient.GetAsync("/documentation/validations");

            var body = await _apiContext.Response.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Response JSON ({(int)_apiContext.Response.StatusCode})", body);
        }

        [Then("the validation documentation is returned successfully")]
        public async Task ThenTheValidationDocumentationIsReturnedSuccessfully(Table table)
        {
            var expected = ParseExpectedTable(table);

            AllureJson.AttachObject(
                "Expected validation documentation result",
                expected,
                _apiContext.JsonOptions);

            _apiContext.Response.ShouldNotBeNull();
            _apiContext.Response!.StatusCode.ShouldBe(ParseStatusCode(expected, "StatusCode"));

            var response = await DeserializeResponse<ValidationDescriptorsResponseDto>(_apiContext.Response);
            response.ShouldNotBeNull();
            response!.Validations.ShouldNotBeNull();

            if (TryParseInt(expected, "ValidationsCount", out var validationsCount))
            {
                response.Validations.Count.ShouldBe(validationsCount);
            }
            else if (TryParseInt(expected, "MinValidationsCount", out var minValidationsCount))
            {
                response.Validations.Count.ShouldBeGreaterThanOrEqualTo(minValidationsCount);
            }

            var policyNames = response.Validations
                .SelectMany(dictionary => dictionary.Keys)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in expected.Where(x => x.Key.StartsWith("HasPolicy_", StringComparison.OrdinalIgnoreCase)))
            {
                var policyName = kvp.Key["HasPolicy_".Length..];
                var shouldExist = bool.Parse(kvp.Value);

                if (shouldExist)
                {
                    policyNames.ShouldContain(policyName);
                }
                else
                {
                    policyNames.ShouldNotContain(policyName);
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
                throw new InvalidOperationException($"Missing '{key}' value in validation documentation expected result table.");
            }

            return value;
        }

        private static HttpStatusCode ParseStatusCode(IReadOnlyDictionary<string, string> values, string key)
        {
            var value = GetRequiredValue(values, key);
            return (HttpStatusCode)int.Parse(value, CultureInfo.InvariantCulture);
        }

        private static bool TryParseInt(IReadOnlyDictionary<string, string> values, string key, out int result)
        {
            if (!values.TryGetValue(key, out var value))
            {
                result = default;
                return false;
            }

            result = int.Parse(value, CultureInfo.InvariantCulture);
            return true;
        }
    }
}
