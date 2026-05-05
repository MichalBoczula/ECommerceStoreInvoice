using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Common.RequestsDto.Orders;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;
using Reqnroll;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Invoices.CreateInvoiceForOrderConflict
{
    [Binding]
    public sealed class CreateInvoiceForOrderConflictSteps
    {
        private readonly ScenarioApiContext _apiContext;
        private Guid _clientId;
        private Guid _orderId;

        public CreateInvoiceForOrderConflictSteps(ScenarioApiContext apiContext)
        {
            _apiContext = apiContext;
        }

        [Given("I have an existing invoice for a paid order")]
        public async Task GivenIHaveAnExistingInvoiceForAPaidOrder(Table table)
        {
            _clientId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            var setupData = ParseExpectedTable(table);

            var clientDataVersionRequest = new CreateClientDataVersionRequestDto
            {
                ClientName = GetRequiredValue(setupData, "ClientName"),
                PostalCode = GetRequiredValue(setupData, "PostalCode"),
                City = GetRequiredValue(setupData, "City"),
                Street = GetRequiredValue(setupData, "Street"),
                BuildingNumber = GetRequiredValue(setupData, "BuildingNumber"),
                ApartmentNumber = GetRequiredValue(setupData, "ApartmentNumber"),
                PhoneNumber = GetRequiredValue(setupData, "PhoneNumber"),
                PhonePrefix = GetRequiredValue(setupData, "PhonePrefix"),
                AddressEmail = GetRequiredValue(setupData, "AddressEmail")
            };

            AllureJson.AttachObject("Create client data version setup request", clientDataVersionRequest, _apiContext.JsonOptions);

            var createClientDataVersionResponse = await _apiContext.HttpClient.PostAsJsonAsync($"/client-data-versions/{_clientId}", clientDataVersionRequest, _apiContext.JsonOptions);
            createClientDataVersionResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            var createShoppingCartResponse = await _apiContext.HttpClient.PostAsync($"/shopping-carts/{_clientId}", content: null);
            createShoppingCartResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            var updateShoppingCartRequest = new UpdateShoppingCartRequestDto
            {
                Lines =
                [
                    new ShoppingCartLineRequestDto
                    {
                        ProductId = productId,
                        Name = GetRequiredValue(setupData, "ProductName"),
                        Brand = GetRequiredValue(setupData, "ProductBrand"),
                        UnitPriceAmount = decimal.Parse(GetRequiredValue(setupData, "UnitPriceAmount"), CultureInfo.InvariantCulture),
                        UnitPriceCurrency = GetRequiredValue(setupData, "UnitPriceCurrency"),
                        Quantity = int.Parse(GetRequiredValue(setupData, "Quantity"), CultureInfo.InvariantCulture)
                    }
                ]
            };

            AllureJson.AttachObject("Update shopping cart setup request", updateShoppingCartRequest, _apiContext.JsonOptions);

            var updateShoppingCartResponse = await _apiContext.HttpClient.PutAsJsonAsync($"/shopping-carts/{_clientId}", updateShoppingCartRequest, _apiContext.JsonOptions);
            updateShoppingCartResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            var createOrderResponse = await _apiContext.HttpClient.PostAsync($"/orders/{_clientId}", content: null);
            createOrderResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            var createOrderBody = await createOrderResponse.Content.ReadAsStringAsync();
            var createdOrder = JsonSerializer.Deserialize<OrderResponseDto>(createOrderBody, _apiContext.JsonOptions);
            createdOrder.ShouldNotBeNull();
            _orderId = createdOrder!.Id;
            _orderId.ShouldNotBe(Guid.Empty);

            var payOrderRequest = new UpdateOrderStatusRequestDto { Status = GetRequiredValue(setupData, "OrderStatus") };
            AllureJson.AttachObject("Pay order setup request", payOrderRequest, _apiContext.JsonOptions);

            var payOrderResponse = await _apiContext.HttpClient.PatchAsJsonAsync($"/orders/{_orderId}/status", payOrderRequest, _apiContext.JsonOptions);
            payOrderResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            var createInvoiceResponse = await _apiContext.HttpClient.PostAsync($"/invoices/{_clientId}/{_orderId}", content: null);
            createInvoiceResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            var createInvoiceBody = await createInvoiceResponse.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Initial create invoice response JSON ({(int)createInvoiceResponse.StatusCode})", createInvoiceBody);
        }

        [When("I submit the duplicate create invoice for order request")]
        public async Task WhenISubmitTheDuplicateCreateInvoiceForOrderRequest(Table table)
        {
            var requestData = ParseExpectedTable(table);
            AllureJson.AttachObject("Duplicate create invoice request table", requestData, _apiContext.JsonOptions);

            var hasBody = TryGetBool(requestData, "HasBody", out var bodyFlag) && bodyFlag;
            _apiContext.Response = await _apiContext.HttpClient.PostAsync($"/invoices/{_clientId}/{_orderId}", content: hasBody ? JsonContent.Create(requestData, options: _apiContext.JsonOptions) : null);

            var body = await _apiContext.Response.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Response JSON ({(int)_apiContext.Response.StatusCode})", body);
        }

        [Then("duplicate create invoice for order returns conflict")]
        public async Task ThenDuplicateCreateInvoiceForOrderReturnsConflict(Table table)
        {
            var expected = ParseExpectedTable(table);
            AllureJson.AttachObject("Expected duplicate create invoice result", expected, _apiContext.JsonOptions);

            _apiContext.Response.ShouldNotBeNull();
            _apiContext.Response!.StatusCode.ShouldBe(ParseStatusCode(expected, "StatusCode"));

            var body = await _apiContext.Response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            root.GetProperty("title").GetString().ShouldBe(GetRequiredValue(expected, "Title"));
            root.GetProperty("type").GetString().ShouldBe(GetRequiredValue(expected, "Type"));

            if (TryGetBool(expected, "HasDetailWithOrderId", out var hasDetailWithOrderId) && hasDetailWithOrderId)
            {
                var detail = root.GetProperty("detail").GetString();
                detail.ShouldNotBeNullOrWhiteSpace();
                detail!.ShouldContain(_orderId.ToString());
            }

            AllureJson.AttachRawJson("Actual duplicate create invoice result", body);
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
                throw new InvalidOperationException($"Missing '{key}' value in duplicate create invoice expected result table.");
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
