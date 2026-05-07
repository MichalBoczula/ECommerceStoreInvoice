using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using Reqnroll;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.ShoppingCarts.UpdateShoppingCartNotFound
{
    [Binding]
    public sealed class UpdateShoppingCartNotFoundSteps
    {
        private readonly ScenarioApiContext _apiContext;
        private Guid _clientId;
        private UpdateShoppingCartRequestDto? _request;

        public UpdateShoppingCartNotFoundSteps(ScenarioApiContext apiContext)
        {
            _apiContext = apiContext;
        }

        [Given("I have a non-existing client id for shopping cart update")]
        public void GivenIHaveANonExistingClientIdForShoppingCartUpdate(Table table)
        {
            _clientId = Guid.NewGuid();

            var requestContext = ParseExpectedTable(table);
            var requestObject = new UpdateShoppingCartNotFoundRequestContext(
                requestContext.TryGetValue("ClientId", out var clientIdTemplate) ? clientIdTemplate : "<generatedId>",
                _clientId.ToString());

            AllureJson.AttachObject(
                "Update shopping cart not found setup",
                requestObject,
                _apiContext.JsonOptions);
        }

        [Given("I have an update shopping cart request for a non-existing shopping cart")]
        public void GivenIHaveAnUpdateShoppingCartRequestForANonExistingShoppingCart(Table table)
        {
            var lines = table.Rows.Select(row => new ShoppingCartLineRequestDto
            {
                ProductId = Guid.Parse(row["ProductId"]),
                Name = row["Name"],
                Brand = row["Brand"],
                UnitPriceAmount = decimal.Parse(row["UnitPriceAmount"], CultureInfo.InvariantCulture),
                UnitPriceCurrency = row["UnitPriceCurrency"],
                Quantity = int.Parse(row["Quantity"], CultureInfo.InvariantCulture)
            }).ToList();

            _request = new UpdateShoppingCartRequestDto { Lines = lines };

            AllureJson.AttachObject(
                "Update shopping cart not found request",
                _request,
                _apiContext.JsonOptions);
        }

        [When("I submit the update shopping cart request for a non-existing shopping cart")]
        public async Task WhenISubmitTheUpdateShoppingCartRequestForANonExistingShoppingCart(Table table)
        {
            _request.ShouldNotBeNull();

            var requestData = ParseExpectedTable(table);
            var requestObject = new UpdateShoppingCartNotFoundHttpRequest(
                Method: requestData.TryGetValue("Method", out var method) ? method : "PUT",
                Endpoint: requestData.TryGetValue("Endpoint", out var endpointTemplate)
                    ? endpointTemplate.Replace("{clientId}", _clientId.ToString(), StringComparison.OrdinalIgnoreCase)
                    : $"/shopping-carts/{_clientId}",
                Lines: requestData.TryGetValue("Lines", out var lines) ? int.Parse(lines, CultureInfo.InvariantCulture) : _request.Lines.Count);

            requestObject.Lines.ShouldBe(_request.Lines.Count);
            AllureJson.AttachObject("Update shopping cart HTTP request", requestObject, _apiContext.JsonOptions);

            _apiContext.Response = await _apiContext.HttpClient.PutAsJsonAsync(requestObject.Endpoint, _request, _apiContext.JsonOptions);

            var body = await _apiContext.Response.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Response JSON ({(int)_apiContext.Response.StatusCode})", body);

        }

        [Then("problem details are returned for update shopping cart not found")]
        public async Task ThenProblemDetailsAreReturnedForUpdateShoppingCartNotFound(Table table)
        {
            var expected = ParseExpectedTable(table);

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
                    problemDetails.Detail!.ShouldContain(_clientId.ToString(), Case.Insensitive);
                    problemDetails.Detail.ShouldContain("ShoppingCart", Case.Insensitive);
                }
                else
                {
                    problemDetails.Detail.ShouldBeNullOrWhiteSpace();
                }
            }

            var expectedInstance = GetRequiredValue(expected, "Instance").Replace("{clientId}", _clientId.ToString(), StringComparison.OrdinalIgnoreCase);
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
        }

        [Then("the update shopping cart not found response data is")]
        public async Task ThenTheUpdateShoppingCartNotFoundResponseDataIs(Table table)
        {
            var expected = ParseExpectedTable(table);

            _apiContext.Response.ShouldNotBeNull();
            var actualResponseJson = await _apiContext.Response!.Content.ReadAsStringAsync();
            var problemDetails = JsonSerializer.Deserialize<NotFoundProblemDetails>(actualResponseJson, _apiContext.JsonOptions);
            problemDetails.ShouldNotBeNull();

            if (TryGetBool(expected, "DetailContainsId", out var hasDetailContainsId) && hasDetailContainsId)
            {
                problemDetails!.Detail.ShouldNotBeNullOrWhiteSpace();
                problemDetails.Detail!.ShouldContain(_clientId.ToString(), Case.Insensitive);
            }

            if (expected.TryGetValue("DetailContains", out var detailContains))
            {
                problemDetails!.Detail.ShouldNotBeNullOrWhiteSpace();
                problemDetails.Detail!.ShouldContain(detailContains, Case.Insensitive);
            }

            if (expected.TryGetValue("TraceId", out var traceIdValue) && traceIdValue.Equals("<generated>", StringComparison.OrdinalIgnoreCase))
            {
                problemDetails!.TraceId.ShouldNotBeNullOrWhiteSpace();
            }
        }

        private sealed record UpdateShoppingCartNotFoundRequestContext(string ClientId, string ResolvedClientId);

        private sealed record UpdateShoppingCartNotFoundHttpRequest(string Method, string Endpoint, int Lines);

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
