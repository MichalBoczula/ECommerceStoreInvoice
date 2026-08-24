// using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
// using ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts;
// using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
// using Reqnroll;
// using Shouldly;
// using System.Globalization;
// using System.Net;
// using System.Net.Http.Json;
// using System.Text.Json;

// namespace ECommerceStoreInvoice.Acceptance.Tests.Features.ShoppingCarts.UpdateShoppingCartSuccess
// {
//     [Binding]
//     public sealed class UpdateShoppingCartSuccessSteps
//     {
//         private readonly ScenarioApiContext _apiContext;
//         private Guid _clientId;
//         private UpdateShoppingCartRequestDto? _request;

//         public UpdateShoppingCartSuccessSteps(ScenarioApiContext apiContext)
//         {
//             _apiContext = apiContext;
//         }

//         [Given("I have an existing shopping cart for update")]
//         public async Task GivenIHaveAnExistingShoppingCartForUpdate()
//         {
//             _clientId = Guid.NewGuid();

//             AllureJson.AttachObject(
//                 "Update shopping cart setup request",
//                 new { ClientId = _clientId },
//                 _apiContext.JsonOptions);

//             var createResponse = await _apiContext.HttpClient.PostAsync($"/shopping-carts/{_clientId}", content: null);
//             createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

//             var createBody = await createResponse.Content.ReadAsStringAsync();
//             AllureJson.AttachRawJson($"Setup response JSON ({(int)createResponse.StatusCode})", createBody);
//         }

//         [Given("I have a valid update shopping cart request")]
//         public void GivenIHaveAValidUpdateShoppingCartRequest(Table table)
//         {
//             var values = ParseExpectedTable(table);

//             _request = new UpdateShoppingCartRequestDto
//             {
//                 Lines =
//                 [
//                     new ShoppingCartLineRequestDto
//                     {
//                         ProductId = ParseGuid(values, "Line1ProductId"),
//                         Name = GetRequiredValue(values, "Line1Name"),
//                         Brand = GetRequiredValue(values, "Line1Brand"),
//                         UnitPriceAmount = ParseDecimal(values, "Line1UnitPriceAmount", 0m),
//                         UnitPriceCurrency = GetRequiredValue(values, "Line1UnitPriceCurrency"),
//                         Quantity = ParseInt(values, "Line1Quantity", 0)
//                     },
//                     new ShoppingCartLineRequestDto
//                     {
//                         ProductId = ParseGuid(values, "Line2ProductId"),
//                         Name = GetRequiredValue(values, "Line2Name"),
//                         Brand = GetRequiredValue(values, "Line2Brand"),
//                         UnitPriceAmount = ParseDecimal(values, "Line2UnitPriceAmount", 0m),
//                         UnitPriceCurrency = GetRequiredValue(values, "Line2UnitPriceCurrency"),
//                         Quantity = ParseInt(values, "Line2Quantity", 0)
//                     }
//                 ]
//             };

//             AllureJson.AttachObject(
//                 "Update shopping cart request (from Gherkin table)",
//                 _request,
//                 _apiContext.JsonOptions);
//         }

//         [When("I submit the update shopping cart request")]
//         public async Task WhenISubmitTheUpdateShoppingCartRequest()
//         {
//             _request.ShouldNotBeNull();

//             _apiContext.Response = await _apiContext.HttpClient.PutAsJsonAsync($"/shopping-carts/{_clientId}", _request, _apiContext.JsonOptions);

//             var body = await _apiContext.Response.Content.ReadAsStringAsync();
//             AllureJson.AttachRawJson($"Response JSON ({(int)_apiContext.Response.StatusCode})", body);
//         }

//         [Then("the shopping cart is updated successfully")]
//         public async Task ThenTheShoppingCartIsUpdatedSuccessfully(Table table)
//         {
//             var expected = ParseExpectedTable(table);

//             _apiContext.Response.ShouldNotBeNull();
//             _apiContext.Response!.StatusCode.ShouldBe(ParseStatusCode(expected, "StatusCode"));

//             var shoppingCart = await DeserializeResponse<ShoppingCartResponseDto>(_apiContext.Response);
//             shoppingCart.ShouldNotBeNull();

//             if (TryGetBool(expected, "HasId", out var hasId))
//             {
//                 if (hasId)
//                 {
//                     shoppingCart!.Id.ShouldNotBe(Guid.Empty);
//                 }
//                 else
//                 {
//                     shoppingCart!.Id.ShouldBe(Guid.Empty);
//                 }
//             }

//             if (TryGetBool(expected, "HasClientId", out var hasClientId))
//             {
//                 if (hasClientId)
//                 {
//                     var expectedClientId = GetExpectedValue(expected, "ClientId", "from-scenario-client-id");
//                     shoppingCart!.ClientId.ShouldBe(expectedClientId.Equals("from-scenario-client-id", StringComparison.OrdinalIgnoreCase) ? _clientId : Guid.Parse(expectedClientId));
//                 }
//                 else
//                 {
//                     shoppingCart!.ClientId.ShouldBe(Guid.Empty);
//                 }
//             }

//             shoppingCart!.TotalAmount.ShouldBe(ParseDecimal(expected, "TotalAmount", shoppingCart.TotalAmount));
//             shoppingCart.TotalCurrency.ShouldBe(GetExpectedValue(expected, "TotalCurrency", shoppingCart.TotalCurrency));
//             shoppingCart.Lines.Count.ShouldBe(ParseInt(expected, "LinesCount", shoppingCart.Lines.Count));

//             var firstLine = shoppingCart.Lines.ElementAtOrDefault(0);
//             if (firstLine is not null)
//             {
//                 firstLine.Name.ShouldBe(GetExpectedValue(expected, "Line1Name", firstLine.Name));
//                 firstLine.Brand.ShouldBe(GetExpectedValue(expected, "Line1Brand", firstLine.Brand));
//                 firstLine.Quantity.ShouldBe(ParseInt(expected, "Line1Quantity", firstLine.Quantity));
//                 firstLine.TotalAmount.ShouldBe(ParseDecimal(expected, "Line1TotalAmount", firstLine.TotalAmount));
//                 firstLine.TotalCurrency.ShouldBe(GetExpectedValue(expected, "Line1TotalCurrency", firstLine.TotalCurrency));
//             }

//             var secondLine = shoppingCart.Lines.ElementAtOrDefault(1);
//             if (secondLine is not null)
//             {
//                 secondLine.Name.ShouldBe(GetExpectedValue(expected, "Line2Name", secondLine.Name));
//                 secondLine.Brand.ShouldBe(GetExpectedValue(expected, "Line2Brand", secondLine.Brand));
//                 secondLine.Quantity.ShouldBe(ParseInt(expected, "Line2Quantity", secondLine.Quantity));
//                 secondLine.TotalAmount.ShouldBe(ParseDecimal(expected, "Line2TotalAmount", secondLine.TotalAmount));
//                 secondLine.TotalCurrency.ShouldBe(GetExpectedValue(expected, "Line2TotalCurrency", secondLine.TotalCurrency));
//             }
//         }

//         private async Task<T?> DeserializeResponse<T>(HttpResponseMessage response)
//         {
//             var content = await response.Content.ReadAsStringAsync();
//             return JsonSerializer.Deserialize<T>(content, _apiContext.JsonOptions);
//         }

//         private static Dictionary<string, string> ParseExpectedTable(Table table)
//         {
//             var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
//             foreach (var row in table.Rows)
//             {
//                 values[row["Field"]] = row["Value"];
//             }

//             return values;
//         }

//         private static string GetRequiredValue(IReadOnlyDictionary<string, string> values, string key)
//         {
//             if (!values.TryGetValue(key, out var value))
//             {
//                 throw new InvalidOperationException($"Missing '{key}' value in shopping cart expected result table.");
//             }

//             return value;
//         }

//         private static string GetExpectedValue(IReadOnlyDictionary<string, string> values, string key, string fallback)
//         {
//             return values.TryGetValue(key, out var value) ? value : fallback;
//         }

//         private static HttpStatusCode ParseStatusCode(IReadOnlyDictionary<string, string> values, string key)
//         {
//             var value = GetRequiredValue(values, key);
//             return (HttpStatusCode)int.Parse(value, CultureInfo.InvariantCulture);
//         }

//         private static decimal ParseDecimal(IReadOnlyDictionary<string, string> values, string key, decimal fallback)
//         {
//             if (!values.TryGetValue(key, out var value))
//             {
//                 return fallback;
//             }

//             return decimal.Parse(value, CultureInfo.InvariantCulture);
//         }

//         private static int ParseInt(IReadOnlyDictionary<string, string> values, string key, int fallback)
//         {
//             if (!values.TryGetValue(key, out var value))
//             {
//                 return fallback;
//             }

//             return int.Parse(value, CultureInfo.InvariantCulture);
//         }


//         private static Guid ParseGuid(IReadOnlyDictionary<string, string> values, string key)
//         {
//             var value = GetRequiredValue(values, key);
//             return Guid.Parse(value);
//         }
//         private static bool TryGetBool(IReadOnlyDictionary<string, string> values, string key, out bool result)
//         {
//             if (!values.TryGetValue(key, out var value))
//             {
//                 result = false;
//                 return false;
//             }

//             result = bool.Parse(value);
//             return true;
//         }
//     }
// }
