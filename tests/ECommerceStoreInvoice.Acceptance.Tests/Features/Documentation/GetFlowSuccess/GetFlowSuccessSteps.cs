using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using Reqnroll;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Documentation.GetFlowSuccess
{
    [Binding]
    public sealed class GetFlowSuccessSteps
    {
        private readonly ScenarioApiContext _apiContext;

        public GetFlowSuccessSteps(ScenarioApiContext apiContext)
        {
            _apiContext = apiContext;
        }

        [When("I request flow documentation")]
        public async Task WhenIRequestFlowDocumentation()
        {
            AllureJson.AttachObject(
                "Flow documentation request",
                new { Method = "GET", Path = "/documentation/flows" },
                _apiContext.JsonOptions);

            _apiContext.Response = await _apiContext.HttpClient.GetAsync("/documentation/flows");

            var body = await _apiContext.Response.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Response JSON ({(int)_apiContext.Response.StatusCode})", body);
        }

        [Then("the flow documentation is returned successfully")]
        public async Task ThenTheFlowDocumentationIsReturnedSuccessfully(Table table)
        {
            var expected = ParseExpectedTable(table);
            AllureJson.AttachObject("Expected result table", expected, _apiContext.JsonOptions);

            _apiContext.Response.ShouldNotBeNull();
            _apiContext.Response!.StatusCode.ShouldBe(ParseStatusCode(expected, "StatusCode"));

            var documentation = await DeserializeResponse<FlowDescriptorsResponseDto>(_apiContext.Response);
            documentation.ShouldNotBeNull();

            documentation!.Flows.Count.ShouldBe(ParseInt(expected, "FlowsCount", documentation.Flows.Count));

            var descriptorNames = documentation.Flows
                .SelectMany(flow => flow.Keys)
                .ToHashSet(StringComparer.Ordinal);

            AssertFlowPresence(expected, "ContainsGetShoppingCartByClientIdDescriptor", descriptorNames, "GetShoppingCartByClientIdDescriptor");
            AssertFlowPresence(expected, "ContainsGetCreateShoppingCartDescriptor", descriptorNames, "GetCreateShoppingCartDescriptor");
            AssertFlowPresence(expected, "ContainsGetCreateOrderDescriptor", descriptorNames, "GetCreateOrderDescriptor");
            AssertFlowPresence(expected, "ContainsGetCreateInvoiceForOrderDescriptor", descriptorNames, "GetCreateInvoiceForOrderDescriptor");
            AssertFlowPresence(expected, "ContainsGetCreateClientDataVersionDescriptor", descriptorNames, "GetCreateClientDataVersionDescriptor");
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
                throw new InvalidOperationException($"Missing '{key}' value in flow documentation expected result table.");
            }

            return value;
        }

        private static HttpStatusCode ParseStatusCode(IReadOnlyDictionary<string, string> values, string key)
        {
            var value = GetRequiredValue(values, key);
            return (HttpStatusCode)int.Parse(value, CultureInfo.InvariantCulture);
        }

        private static int ParseInt(IReadOnlyDictionary<string, string> values, string key, int fallback)
        {
            if (!values.TryGetValue(key, out var value))
            {
                return fallback;
            }

            return int.Parse(value, CultureInfo.InvariantCulture);
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

        private static void AssertFlowPresence(IReadOnlyDictionary<string, string> expected, string key, IReadOnlySet<string> flowNames, string flowName)
        {
            if (!TryGetBool(expected, key, out var shouldExist))
            {
                return;
            }

            flowNames.Contains(flowName).ShouldBe(shouldExist);
        }
    }
}
