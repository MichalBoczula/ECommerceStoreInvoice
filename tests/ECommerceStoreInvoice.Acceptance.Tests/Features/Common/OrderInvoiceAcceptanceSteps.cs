using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Common.RequestsDto.Orders;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;
using Reqnroll;
using Shouldly;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Common;

[Binding]
public sealed class OrderInvoiceAcceptanceSteps(ScenarioApiContext context)
{
    private Guid _clientId;
    private Guid _orderId;
    private Guid _invoiceId;
    private Guid _productId;

    [Given("I have a valid shopping cart for order creation")]
    public Task GivenValidCart(Table table) => CreateCart(table);

    [Given("I have an existing order id with setup data")]
    public async Task GivenExistingOrderId(Table table)
    {
        await CreateCart(table);
        await CreateOrder();
    }

    [Given("I have existing orders for a client")]
    public async Task GivenOrdersForClient(Table table)
    {
        await CreateCart(table);
        var count = int.Parse(Values(table)["OrdersToCreate"], CultureInfo.InvariantCulture);
        for (var index = 0; index < count; index++)
        {
            await CreateOrder();
            if (index + 1 < count)
                await FillCart();
        }
    }

    [Given("I have a paid order for invoice creation")]
    public async Task GivenPaidOrder(Table table)
    {
        await CreateCart(table);
        await CreateClientDataVersion(table);
        await CreateOrder();
        await MarkOrderPaid();
    }

    [Given("I have an existing invoice for a paid order")]
    public async Task GivenExistingInvoice(Table table)
    {
        await GivenPaidOrder(table);
        await CreateInvoice();
    }

    [Given("I have an existing invoice id")]
    public async Task GivenExistingInvoiceId()
    {
        var table = new Table("Field", "Value");
        table.AddRow("Quantity", "2");
        await GivenPaidOrder(table);
        await CreateInvoice();
    }

    [Given("I have an order id that does not exist")]
    public void GivenMissingOrderForStatusUpdate() => _orderId = Guid.NewGuid();

    [Given("I have an empty order id for status update")]
    public void GivenEmptyOrderForStatusUpdate() => _orderId = Guid.Empty;

    [Given("I have a paid order without client data")]
    public async Task GivenPaidOrderWithoutClientData()
    {
        var table = new Table("Field", "Value");
        table.AddRow("Quantity", "2");
        await CreateCart(table);
        await CreateOrder();
        await MarkOrderPaid();
    }

    [When("I change the order status to {string}")]
    public async Task WhenOrderStatusChanges(string status) =>
        context.Response = await context.HttpClient.PatchAsJsonAsync($"/orders/{_orderId}/status",
            new UpdateOrderStatusRequestDto { Status = status }, context.JsonOptions);

    [When("I request an invoice for the current order")]
    public Task WhenInvoiceRequestedForCurrentOrder() => SendPost($"/invoices/{_clientId}/{_orderId}");

    [When("a different client requests an invoice for the order")]
    public Task WhenAnotherClientRequestsInvoice() => SendPost($"/invoices/{Guid.NewGuid()}/{_orderId}");

    [When("an empty client id requests an invoice for the order")]
    public Task WhenEmptyClientRequestsInvoice() => SendPost($"/invoices/{Guid.Empty}/{_orderId}");

    [Then("the order status response is 200 with status {string}")]
    public async Task ThenOrderStatusUpdated(string expectedStatus)
    {
        context.Response.ShouldNotBeNull();
        context.Response.StatusCode.ShouldBe(HttpStatusCode.OK, await context.Response.Content.ReadAsStringAsync());
        var order = await context.Response.Content.ReadFromJsonAsync<OrderResponseDto>(context.JsonOptions);
        order.ShouldNotBeNull();
        order.Id.ShouldBe(_orderId);
        order.Status.ShouldBe(expectedStatus);
        using var stored = await context.HttpClient.GetAsync($"/orders/{_orderId}");
        stored.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await stored.Content.ReadFromJsonAsync<OrderResponseDto>(context.JsonOptions))!.Status.ShouldBe(expectedStatus);
    }

    [Then("the order status response is {int}")]
    public async Task ThenOrderStatusFails(int status)
    {
        context.Response.ShouldNotBeNull();
        context.Response.StatusCode.ShouldBe((HttpStatusCode)status);
        context.Response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        using var json = JsonDocument.Parse(await context.Response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("status").GetInt32().ShouldBe(status);
    }

    [Then("the order status response is {int} and the stored status is {string}")]
    public async Task ThenOrderStatusFailsAndIsNotSaved(int status, string expectedStatus)
    {
        await ThenOrderStatusFails(status);
        using var stored = await context.HttpClient.GetAsync($"/orders/{_orderId}");
        stored.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await stored.Content.ReadFromJsonAsync<OrderResponseDto>(context.JsonOptions))!.Status.ShouldBe(expectedStatus);
    }

    [Then("invoice creation fails with status {int}")]
    public async Task ThenInvoiceCreationFails(int status)
    {
        context.Response.ShouldNotBeNull();
        context.Response.StatusCode.ShouldBe((HttpStatusCode)status);
        context.Response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        using var json = JsonDocument.Parse(await context.Response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("status").GetInt32().ShouldBe(status);
        using var order = await context.HttpClient.GetAsync($"/orders/{_orderId}");
        order.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Given("the get invoice by id request is documented as")]
    public void GivenGetInvoiceRequest(Table table)
    {
        var values = Values(table);
        values["Method"].ShouldBe("GET");
        values["EndpointTemplate"].ShouldBe("/invoices/{invoiceId}");
        bool.Parse(values["HasInvoiceId"]).ShouldBeTrue();
        values["InvoiceIdSource"].ShouldBe("setup-created-invoice");
        values["Accept"].ShouldBe("application/json");
    }

    [Given("I have a non-existing invoice id for invoices retrieval")]
    public void GivenMissingInvoiceId(Table table)
    {
        var values = Values(table);
        values["Endpoint"].ShouldBe("/invoices/{invoiceId}");
        _invoiceId = Guid.NewGuid();
    }

    [When("I submit the create order request")]
    public Task WhenCreateOrder() => SendPost($"/orders/{_clientId}");

    [When("I request order by id")]
    public Task WhenGetOrderById() => SendGet($"/orders/{_orderId}");

    [When("I request orders by client id")]
    public Task WhenGetOrdersByClientId() => SendGet($"/orders/client/{_clientId}");

    [When("I submit the create invoice for order request")]
    public Task WhenCreateInvoice(Table table)
    {
        var values = Values(table);
        values["HttpMethod"].ShouldBe("POST");
        values["Route"].ShouldBe("/invoices/{clientId}/{orderId}");
        return SendPost($"/invoices/{_clientId}/{_orderId}");
    }

    [When("I submit the duplicate create invoice for order request")]
    public Task WhenDuplicateInvoice(Table table)
    {
        bool.Parse(Values(table)["HasBody"]).ShouldBeFalse();
        return SendPost($"/invoices/{_clientId}/{_orderId}");
    }

    [When("I request invoice by id")]
    public Task WhenGetInvoiceById() => SendGet($"/invoices/{_invoiceId}");

    [When("I request invoice by id for non-existing invoice")]
    public Task WhenGetMissingInvoice() => SendGet($"/invoices/{_invoiceId}");

    [Then("the order is created successfully")]
    public async Task ThenOrderCreated(Table table)
    {
        var order = await ReadSuccess<OrderResponseDto>(table);
        AssertOrder(order, Values(table));
        _orderId = order.Id;
    }

    [Then("the order is returned successfully by id")]
    public async Task ThenOrderById(Table table)
    {
        var order = await ReadSuccess<OrderResponseDto>(table);
        order.Id.ShouldBe(_orderId);
        AssertOrder(order, Values(table));
    }

    [Then("the orders are returned successfully")]
    public async Task ThenOrdersByClientId(Table table)
    {
        var orders = await ReadSuccess<List<OrderResponseDto>>(table);
        var values = Values(table);
        orders.Count.ShouldBe(int.Parse(values["OrdersCount"], CultureInfo.InvariantCulture));
        orders.Select(order => order.Id).Distinct().Count().ShouldBe(orders.Count);
        foreach (var order in orders)
        {
            order.ClientId.ShouldBe(_clientId);
            order.Status.ShouldBe(values["FirstOrderStatus"]);
            order.TotalAmount.ShouldBe(decimal.Parse(values["FirstOrderTotalAmount"], CultureInfo.InvariantCulture));
            order.TotalCurrency.ShouldBe(values["FirstOrderTotalCurrency"]);
            order.Lines.Count.ShouldBe(int.Parse(values["FirstOrderLinesCount"], CultureInfo.InvariantCulture));
            order.Lines.Single().ProductVersion.Name.ShouldBe(values["FirstLineName"]);
            order.Lines.Single().Quantity.ShouldBe(int.Parse(values["FirstLineQuantity"], CultureInfo.InvariantCulture));
        }
    }

    [Then("the invoice is created successfully")]
    public async Task ThenInvoiceCreated(Table table)
    {
        var invoice = await ReadSuccess<InvoiceResponseDto>(table);
        AssertInvoice(invoice);
        _invoiceId = invoice.Id;
    }

    [Then("the invoice is returned successfully by id")]
    public async Task ThenInvoiceById(Table table)
    {
        var invoice = await ReadSuccess<InvoiceResponseDto>(table);
        invoice.Id.ShouldBe(_invoiceId);
        AssertInvoice(invoice);
    }

    [Then("duplicate create invoice for order returns conflict")]
    public async Task ThenDuplicateInvoiceConflict(Table table)
    {
        var values = Values(table);
        context.Response!.StatusCode.ShouldBe((HttpStatusCode)int.Parse(values["StatusCode"], CultureInfo.InvariantCulture));
        var problem = await context.Response.Content.ReadFromJsonAsync<ConflictProblemDetails>(context.JsonOptions);
        problem.ShouldNotBeNull();
        problem.Title.ShouldBe(values["Title"]);
        problem.Type.ShouldBe(values["Type"]);
        problem.Detail.ShouldContain(_orderId.ToString(), Case.Insensitive);
    }

    [Then("problem details are returned for get invoice by id not found")]
    public async Task ThenInvoiceMissing(Table table)
    {
        var values = Values(table);
        context.Response!.StatusCode.ShouldBe((HttpStatusCode)int.Parse(values["StatusCode"], CultureInfo.InvariantCulture));
        var problem = await context.Response.Content.ReadFromJsonAsync<NotFoundProblemDetails>(context.JsonOptions);
        problem.ShouldNotBeNull();
        problem.Title.ShouldBe(values["Title"]);
        problem.Type.ShouldBe(values["Type"]);
        problem.Detail.ShouldContain(values["DetailContains"], Case.Insensitive);
        problem.Detail.ShouldContain(_invoiceId.ToString(), Case.Insensitive);
        problem.Instance.ShouldBe($"/invoices/{_invoiceId}");
        problem.TraceId.ShouldNotBeNullOrWhiteSpace();
    }

    private async Task CreateCart(Table table)
    {
        _clientId = Guid.NewGuid();
        _productId = Guid.NewGuid();
        using var response = await context.HttpClient.PostAsync($"/shopping-carts/{_clientId}", null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        await FillCart(int.TryParse(Values(table).GetValueOrDefault("Quantity"), out var quantity) ? quantity : 2);
    }

    private Task FillCart() => FillCart(2);

    private async Task FillCart(int quantity)
    {
        var request = new UpdateShoppingCartRequestDto
        {
            Lines = [new ShoppingCartLineRequestDto { ProductId = _productId, Quantity = quantity }]
        };
        using var response = await context.HttpClient.PutAsJsonAsync($"/shopping-carts/{_clientId}", request, context.JsonOptions);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task CreateClientDataVersion(Table table)
    {
        var values = Values(table);
        var request = new CreateClientDataVersionRequestDto
        {
            ClientName = values.GetValueOrDefault("ClientName", "John Doe"),
            PostalCode = values.GetValueOrDefault("PostalCode", "00-001"),
            City = values.GetValueOrDefault("City", "NewYork"),
            Street = values.GetValueOrDefault("Street", "Main.St"),
            BuildingNumber = values.GetValueOrDefault("BuildingNumber", "10A"),
            ApartmentNumber = values.GetValueOrDefault("ApartmentNumber", "5"),
            PhoneNumber = values.GetValueOrDefault("PhoneNumber", "123456789"),
            PhonePrefix = values.GetValueOrDefault("PhonePrefix", "48"),
            AddressEmail = values.GetValueOrDefault("AddressEmail", "john.doe@test.com")
        };
        using var response = await context.HttpClient.PostAsJsonAsync($"/client-data-versions/{_clientId}", request, context.JsonOptions);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task CreateOrder()
    {
        using var response = await context.HttpClient.PostAsync($"/orders/{_clientId}", null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var order = await response.Content.ReadFromJsonAsync<OrderResponseDto>(context.JsonOptions);
        order.ShouldNotBeNull();
        _orderId = order.Id;
    }

    private async Task MarkOrderPaid()
    {
        using var response = await context.HttpClient.PatchAsJsonAsync($"/orders/{_orderId}/status",
            new UpdateOrderStatusRequestDto { Status = "Paid" }, context.JsonOptions);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    private async Task CreateInvoice()
    {
        using var response = await context.HttpClient.PostAsync($"/invoices/{_clientId}/{_orderId}", null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponseDto>(context.JsonOptions);
        invoice.ShouldNotBeNull();
        _invoiceId = invoice.Id;
    }

    private async Task SendPost(string route) =>
        context.Response = await context.HttpClient.PostAsync(route, null);

    private async Task SendGet(string route) =>
        context.Response = await context.HttpClient.GetAsync(route);

    private async Task<T> ReadSuccess<T>(Table table) where T : class
    {
        context.Response.ShouldNotBeNull();
        context.Response.StatusCode.ShouldBe((HttpStatusCode)int.Parse(Values(table)["StatusCode"], CultureInfo.InvariantCulture),
            await context.Response.Content.ReadAsStringAsync());
        var value = await context.Response.Content.ReadFromJsonAsync<T>(context.JsonOptions);
        value.ShouldNotBeNull();
        return value;
    }

    private void AssertOrder(OrderResponseDto order, IReadOnlyDictionary<string, string> values)
    {
        order.Id.ShouldNotBe(Guid.Empty);
        order.ClientId.ShouldBe(_clientId);
        order.Status.ShouldBe(values["Status"]);
        order.TotalAmount.ShouldBe(decimal.Parse(values["TotalAmount"], CultureInfo.InvariantCulture));
        order.TotalCurrency.ShouldBe(values["TotalCurrency"]);
        order.Lines.Count.ShouldBe(int.Parse(values["LinesCount"], CultureInfo.InvariantCulture));
        var line = order.Lines.Single();
        line.ProductVersionId.ShouldNotBe(Guid.Empty);
        line.ProductVersion.Name.ShouldBe(values["FirstLineName"]);
        line.ProductVersion.Brand.ShouldBe(values["FirstLineBrand"]);
        line.Quantity.ShouldBe(int.Parse(values["FirstLineQuantity"], CultureInfo.InvariantCulture));
        line.ProductVersion.PriceAmount.ShouldBe(decimal.Parse(values["FirstLineUnitPriceAmount"], CultureInfo.InvariantCulture));
        line.ProductVersion.PriceCurrency.ShouldBe(values["FirstLineUnitPriceCurrency"]);
        line.LineTotalAmount.ShouldBe(decimal.Parse(values["FirstLineTotalAmount"], CultureInfo.InvariantCulture));
    }

    private void AssertInvoice(InvoiceResponseDto invoice)
    {
        invoice.Id.ShouldNotBe(Guid.Empty);
        invoice.OrderId.ShouldBe(_orderId);
        invoice.ClietDataVersionId.ShouldNotBe(Guid.Empty);
        invoice.StorageUrl.ShouldNotBeNullOrWhiteSpace();
        invoice.CreatedAt.ShouldNotBe(default);
    }

    private static Dictionary<string, string> Values(Table table) =>
        table.Rows.ToDictionary(row => row["Field"], row => row["Value"], StringComparer.OrdinalIgnoreCase);
}
