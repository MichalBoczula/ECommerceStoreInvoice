using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Common.RequestsDto.Orders;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;
using Reqnroll;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Invoices.GetInvoiceByIdSuccess
{
    [Binding]
    public sealed class GetInvoiceByIdSuccessSteps
    {
        private readonly ScenarioApiContext _apiContext;
        private GetInvoiceByIdRequestDoc? _requestDoc;
        private Guid _clientId;
        private Guid _orderId;
        private Guid _invoiceId;

        public GetInvoiceByIdSuccessSteps(ScenarioApiContext apiContext)
        {
            _apiContext = apiContext;
        }

        [Given("I have an existing invoice id")]
        public async Task GivenIHaveAnExistingInvoiceId()
        {
            _clientId = Guid.NewGuid();
            var productId = Guid.NewGuid();

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

            AllureJson.AttachObject("Create client data version setup request", clientDataVersionRequest, _apiContext.JsonOptions);

            var createClientDataVersionResponse = await _apiContext.HttpClient.PostAsJsonAsync($"/client-data-versions/{_clientId}", clientDataVersionRequest, _apiContext.JsonOptions);
            createClientDataVersionResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            var createClientDataVersionBody = await createClientDataVersionResponse.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Create client data version response JSON ({(int)createClientDataVersionResponse.StatusCode})", createClientDataVersionBody);

            var createShoppingCartResponse = await _apiContext.HttpClient.PostAsync($"/shopping-carts/{_clientId}", content: null);
            createShoppingCartResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            var createShoppingCartBody = await createShoppingCartResponse.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Create shopping cart response JSON ({(int)createShoppingCartResponse.StatusCode})", createShoppingCartBody);

            var updateShoppingCartRequest = new UpdateShoppingCartRequestDto
            {
                Lines =
                [
                    new ShoppingCartLineRequestDto
                    {
                        ProductId = productId,
                        Name = "Laptop",
                        Brand = "Lenovo",
                        UnitPriceAmount = 999.99m,
                        UnitPriceCurrency = "usd",
                        Quantity = 2
                    }
                ]
            };

            AllureJson.AttachObject("Update shopping cart setup request", updateShoppingCartRequest, _apiContext.JsonOptions);

            var updateShoppingCartResponse = await _apiContext.HttpClient.PutAsJsonAsync($"/shopping-carts/{_clientId}", updateShoppingCartRequest, _apiContext.JsonOptions);
            updateShoppingCartResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            var updateShoppingCartBody = await updateShoppingCartResponse.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Update shopping cart response JSON ({(int)updateShoppingCartResponse.StatusCode})", updateShoppingCartBody);

            var createOrderResponse = await _apiContext.HttpClient.PostAsync($"/orders/{_clientId}", content: null);
            createOrderResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            var createOrderBody = await createOrderResponse.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Create order response JSON ({(int)createOrderResponse.StatusCode})", createOrderBody);

            var createdOrder = JsonSerializer.Deserialize<OrderResponseDto>(createOrderBody, _apiContext.JsonOptions);
            createdOrder.ShouldNotBeNull();
            _orderId = createdOrder!.Id;
            _orderId.ShouldNotBe(Guid.Empty);

            var payOrderRequest = new UpdateOrderStatusRequestDto
            {
                Status = "Paid"
            };

            AllureJson.AttachObject("Pay order setup request", payOrderRequest, _apiContext.JsonOptions);

            var payOrderResponse = await _apiContext.HttpClient.PatchAsJsonAsync($"/orders/{_orderId}/status", payOrderRequest, _apiContext.JsonOptions);
            payOrderResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            var payOrderBody = await payOrderResponse.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Pay order response JSON ({(int)payOrderResponse.StatusCode})", payOrderBody);

            var createInvoiceResponse = await _apiContext.HttpClient.PostAsync($"/invoices/{_clientId}/{_orderId}", content: null);
            createInvoiceResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            var createInvoiceBody = await createInvoiceResponse.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Create invoice response JSON ({(int)createInvoiceResponse.StatusCode})", createInvoiceBody);

            var createdInvoice = JsonSerializer.Deserialize<InvoiceResponseDto>(createInvoiceBody, _apiContext.JsonOptions);
            createdInvoice.ShouldNotBeNull();

            _invoiceId = createdInvoice!.Id;
            _invoiceId.ShouldNotBe(Guid.Empty);
        }

        [When("I request invoice by id")]
        public async Task WhenIRequestInvoiceById()
        {
            _apiContext.Response = await _apiContext.HttpClient.GetAsync($"/invoices/{_invoiceId}");

            var body = await _apiContext.Response.Content.ReadAsStringAsync();
            AllureJson.AttachRawJson($"Response JSON ({(int)_apiContext.Response.StatusCode})", body);
        }

        [Given("the get invoice by id request is documented as")]
        public void GivenTheGetInvoiceByIdRequestIsDocumentedAs(Table table)
        {
            var values = ParseExpectedTable(table);
            _requestDoc = new GetInvoiceByIdRequestDoc
            {
                Method = GetRequiredValue(values, "Method"),
                EndpointTemplate = GetRequiredValue(values, "EndpointTemplate"),
                HasInvoiceId = bool.Parse(GetRequiredValue(values, "HasInvoiceId")),
                InvoiceId = _invoiceId,
                InvoiceIdSource = GetRequiredValue(values, "InvoiceIdSource"),
                Accept = GetRequiredValue(values, "Accept")
            };

            AllureJson.AttachObject("Get invoice by id request (from gherkin table)", _requestDoc, _apiContext.JsonOptions);
        }

        [Then("the invoice is returned successfully by id")]
        public async Task ThenTheInvoiceIsReturnedSuccessfullyById(Table table)
        {
            var expected = ParseExpectedTable(table);
            AllureJson.AttachObject("Expected get invoice by id result", expected, _apiContext.JsonOptions);

            _apiContext.Response.ShouldNotBeNull();
            _apiContext.Response!.StatusCode.ShouldBe(ParseStatusCode(expected, "StatusCode"));

            var invoice = await DeserializeResponse<InvoiceResponseDto>(_apiContext.Response);
            invoice.ShouldNotBeNull();

            var responseDoc = new GetInvoiceByIdResponseDoc
            {
                StatusCode = (int)_apiContext.Response.StatusCode,
                Id = invoice!.Id,
                OrderId = invoice.OrderId,
                ClietDataVersionId = invoice.ClietDataVersionId,
                StorageUrl = invoice.StorageUrl,
                CreatedAt = invoice.CreatedAt
            };
            AllureJson.AttachObject("Get invoice by id response (actual object)", responseDoc, _apiContext.JsonOptions);

            if (TryGetBool(expected, "HasId", out var hasId) && hasId)
            {
                invoice!.Id.ShouldBe(_invoiceId);
            }

            if (TryGetBool(expected, "HasOrderId", out var hasOrderId) && hasOrderId)
            {
                invoice!.OrderId.ShouldBe(_orderId);
            }

            if (TryGetBool(expected, "HasClientDataVersionId", out var hasClientDataVersionId) && hasClientDataVersionId)
            {
                invoice!.ClietDataVersionId.ShouldNotBe(Guid.Empty);
            }

            if (TryGetBool(expected, "HasStorageUrl", out var hasStorageUrl) && hasStorageUrl)
            {
                invoice!.StorageUrl.ShouldNotBeNullOrWhiteSpace();
            }

            if (TryGetBool(expected, "HasCreatedAt", out var hasCreatedAt) && hasCreatedAt)
            {
                invoice!.CreatedAt.ShouldNotBe(default);
            }

            AllureJson.AttachObject("Actual get invoice by id result", invoice!, _apiContext.JsonOptions);
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
                throw new InvalidOperationException($"Missing '{key}' value in get invoice by id expected result table.");
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

        private sealed class GetInvoiceByIdRequestDoc
        {
            public string Method { get; init; } = string.Empty;
            public string EndpointTemplate { get; init; } = string.Empty;
            public bool HasInvoiceId { get; init; }
            public Guid InvoiceId { get; init; }
            public string InvoiceIdSource { get; init; } = string.Empty;
            public string Accept { get; init; } = string.Empty;
        }

        private sealed class GetInvoiceByIdResponseDoc
        {
            public int StatusCode { get; init; }
            public Guid Id { get; init; }
            public Guid OrderId { get; init; }
            public Guid ClietDataVersionId { get; init; }
            public string StorageUrl { get; init; } = string.Empty;
            public DateTime CreatedAt { get; init; }
        }
    }
}
