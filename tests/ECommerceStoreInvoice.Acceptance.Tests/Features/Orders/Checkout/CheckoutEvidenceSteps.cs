using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using MongoDB.Bson;
using Reqnroll;
using Shouldly;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Orders.Checkout;

[Binding]
public sealed class CheckoutEvidenceSteps(ScenarioApiContext context)
{
    private readonly Guid _clientId = Guid.NewGuid();
    private readonly Guid _cartId = Guid.NewGuid();
    private readonly Guid _otherClientId = Guid.NewGuid();
    private readonly Guid _otherCartId = Guid.NewGuid();
    private DateTime _createdAt;
    private DateTime _updatedAt;
    private DateTime _started;
    private DateTime _completed;
    private CartProduct[] _products = [];
    private Dictionary<string, BsonDocument[]>? _before;
    private Dictionary<string, BsonDocument[]>? _committed;
    private OrderResponseDto? _response;
    private int _catalogBefore;

    [Given("the checkout cart uses these identities and dates")]
    public void GivenDates(Table table)
    {
        var values = Values(table);
        Required(values, "CartId").ShouldBe("generated");
        Required(values, "ClientId").ShouldBe("generated");
        _createdAt = DateTime.Parse(Required(values, "CreatedAt"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        _updatedAt = DateTime.Parse(Required(values, "UpdatedAt"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        _createdAt.Kind.ShouldBe(DateTimeKind.Utc);
        _updatedAt.Kind.ShouldBe(DateTimeKind.Utc);
        context.RecordEvidence("Checkout cart identity and date fixture", new { values, cartId = _cartId, clientId = _clientId });
    }

    [Given("the checkout cart contains these catalog products")]
    public async Task GivenProducts(Table table)
    {
        _products = table.Rows.Select(row => new CartProduct(Product(row), int.Parse(row["Quantity"], CultureInfo.InvariantCulture))).ToArray();
        _products.Select(line => line.Product.ProductId).Distinct().Count().ShouldBe(_products.Length);
        context.RecordEvidence("Checkout cart products fixture", _products);
        await context.Factory.SeedShoppingCartDocumentAsync(CartDocument(_cartId, _clientId,
            _products.Select(line => (line.Product.ProductId, line.Quantity))));
    }

    [Given("another checkout client has these stored cart lines")]
    public async Task GivenOtherCart(Table table)
    {
        var lines = table.Rows.Select(row => (ProductId: Guid.Parse(row["ProductId"]), Quantity: int.Parse(row["Quantity"], CultureInfo.InvariantCulture))).ToArray();
        context.RecordEvidence("Other checkout client cart fixture", new { cartId = _otherCartId, clientId = _otherClientId, lines = lines.Select(line => new { line.ProductId, line.Quantity }) });
        await context.Factory.SeedShoppingCartDocumentAsync(CartDocument(_otherCartId, _otherClientId, lines));
    }

    [Given("the isolated catalog contains these product values")]
    [Then("the isolated catalog changes to these product values")]
    public async Task ChangeCatalog(Table table)
    {
        var products = table.Rows.Select(Product).ToArray();
        context.RecordEvidence("Isolated catalog SQL fixture change", products);
        foreach (var product in products)
            await context.Factory.ChangeIsolatedCatalogProductAsync(product.ProductId, product.Name, product.Brand, product.PriceAmount, product.PriceCurrency);

        using var client = context.CreateCatalogFixtureClient();
        using var response = await client.PostAsJsonAsync("mobile-phones/by-ids", products.Select(product => product.ProductId).ToArray(), context.JsonOptions);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        context.RecordEvidence("Isolated catalog actual HTTP product values", document.RootElement.Clone());
        document.RootElement.GetArrayLength().ShouldBe(products.Length);
        foreach (var expected in products)
        {
            var actual = document.RootElement.EnumerateArray().Single(item => item.GetProperty("id").GetGuid() == expected.ProductId);
            actual.GetProperty("name").GetString().ShouldBe(expected.Name);
            actual.GetProperty("brand").GetString().ShouldBe(expected.Brand);
            actual.GetProperty("price").GetProperty("amount").GetDecimal().ShouldBe(expected.PriceAmount);
            actual.GetProperty("price").GetProperty("currency").GetString().ShouldBe(expected.PriceCurrency);
        }
    }

    [When("the client checks out using this request")]
    public async Task WhenCheckout(Table table)
    {
        var values = Values(table);
        Required(values, "Method").ShouldBe("POST");
        Required(values, "Endpoint").ShouldBe("/orders/client/{clientId}");
        Required(values, "HasBody").ShouldBe("false");
        _before = await context.Factory.GetCheckoutDocumentsAsync();
        AttachSnapshot("Checkout database before", _before);
        _catalogBefore = context.Factory.CatalogRequestCount;
        context.RecordEvidence("Checkout resolved request", new { method = "POST", path = $"/orders/client/{_clientId}", clientId = _clientId, hasBody = false });
        _started = DateTime.UtcNow;
        context.Response = await context.HttpClient.PostAsync($"/orders/client/{_clientId}", null);
        _completed = DateTime.UtcNow;
        context.RecordEvidence("Checkout request window", new { startedAt = _started, completedAt = _completed });
    }

    [Then("checkout returns this complete order profile")]
    public async Task ThenOrder(Table table)
    {
        var expected = Values(table);
        context.RecordEvidence("Checkout order response expectations", expected);
        context.Response.ShouldNotBeNull();
        ((int)context.Response.StatusCode).ShouldBe(Number(expected, "StatusCode"));
        context.Response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        _response = await context.Response.Content.ReadFromJsonAsync<OrderResponseDto>(context.JsonOptions);
        _response.ShouldNotBeNull();
        context.RecordEvidence("Checkout complete actual response", _response);
        Required(expected, "Id").ShouldBe("generated");
        Required(expected, "ClientId").ShouldBe("requested");
        Required(expected, "CreatedAt").ShouldBe("generated");
        Required(expected, "UpdatedAt").ShouldBe("<null>");
        _response.Id.ShouldNotBe(Guid.Empty);
        _response.ClientId.ShouldBe(_clientId);
        InWindow(_response.CreatedAt, _started, _completed);
        _response.UpdatedAt.ShouldBeNull();
        _response.Status.ShouldBe(Required(expected, "Status"));
        _response.Status.ShouldBe("Created");
        _response.TotalAmount.ShouldBe(Amount(expected, "TotalAmount"));
        _response.TotalAmount.ShouldBe(_products.Sum(line => line.Product.PriceAmount * line.Quantity));
        _response.TotalCurrency.ShouldBe(Required(expected, "TotalCurrency"));
        _response.Lines.Count.ShouldBe(Number(expected, "LinesCount"));
        _response.Lines.Count.ShouldBe(_products.Length);
    }

    [Then("checkout returns these complete snapshot lines")]
    public void ThenLines(Table table)
    {
        context.RecordEvidence("Checkout snapshot line expectations", table.Rows.Select(row => row.ToDictionary(pair => pair.Key, pair => pair.Value)).ToArray());
        _response.ShouldNotBeNull();
        table.Rows.Count.ShouldBe(_response.Lines.Count);
        _response.Lines.Select(line => line.ProductVersionId).Distinct().Count().ShouldBe(_products.Length);
        foreach (var row in table.Rows)
        {
            var expected = Product(row);
            var actual = _response.Lines.Single(line => line.ProductVersion.ProductId == expected.ProductId);
            var request = _products.Single(line => line.Product.ProductId == expected.ProductId);
            expected.ShouldBe(request.Product);
            row["ProductVersionId"].ShouldBe("generated");
            row["CreatedAt"].ShouldBe("generated");
            row["DeactivatedAt"].ShouldBe("<null>");
            actual.ProductVersionId.ShouldNotBe(Guid.Empty);
            actual.ProductVersionId.ShouldNotBe(expected.ProductId);
            actual.ProductVersion.Id.ShouldBe(actual.ProductVersionId);
            actual.Quantity.ShouldBe(int.Parse(row["Quantity"], CultureInfo.InvariantCulture));
            actual.Quantity.ShouldBe(request.Quantity);
            actual.ProductVersion.IsActive.ShouldBe(bool.Parse(row["IsActive"]));
            actual.ProductVersion.IsActive.ShouldBeTrue();
            InWindow(actual.ProductVersion.CreatedAt, _started, _completed);
            actual.ProductVersion.DeactivatedAt.ShouldBeNull();
            actual.ProductVersion.Name.ShouldBe(expected.Name);
            actual.ProductVersion.Brand.ShouldBe(expected.Brand);
            actual.ProductVersion.PriceAmount.ShouldBe(expected.PriceAmount);
            actual.ProductVersion.PriceCurrency.ShouldBe(expected.PriceCurrency);
            actual.LineTotalAmount.ShouldBe(decimal.Parse(row["LineTotalAmount"], CultureInfo.InvariantCulture));
            actual.LineTotalAmount.ShouldBe(expected.PriceAmount * request.Quantity);
        }
        _response.TotalAmount.ShouldBe(_response.Lines.Sum(line => line.LineTotalAmount));
    }

    [Then("checkout commits these coordinated database changes")]
    public async Task ThenCommitted(Table table)
    {
        var expected = Values(table);
        context.RecordEvidence("Checkout persistence expectations", expected);
        var after = await context.Factory.GetCheckoutDocumentsAsync();
        AttachSnapshot("Checkout database after", after);
        _before.ShouldNotBeNull();
        _response.ShouldNotBeNull();
        foreach (var (name, prefix) in new[] { ("orders", "Orders"), ("productVersions", "Snapshots"), ("shoppingCarts", "Carts") })
        {
            _before[name].Length.ShouldBe(Number(expected, prefix + "Before"));
            after[name].Length.ShouldBe(Number(expected, prefix + "After"));
        }
        after["orders"].Length.ShouldBe(_before["orders"].Length + 1);
        after["productVersions"].Length.ShouldBe(_before["productVersions"].Length + _products.Length);
        _catalogBefore.ShouldBe(Number(expected, "CatalogRequestsBefore"));
        context.Factory.CatalogRequestCount.ShouldBe(Number(expected, "CatalogRequestsAfter"));
        (context.Factory.CatalogRequestCount - _catalogBefore).ShouldBe(1);
        foreach (var name in new[] { "orders", "productVersions" })
            foreach (var prior in _before[name]) after[name].Single(document => Id(document) == Id(prior)).ShouldBe(prior);
        foreach (var name in new[] { "invoices", "clientDataVersions" }) after[name].ShouldBe(_before[name]);

        var order = after["orders"].Single(document => Id(document) == _response.Id);
        Fields(order, "_id", "ClientId", "Lines", "CreatedAt", "UpdatedAt", "Status");
        GuidValue(order, "ClientId").ShouldBe(_clientId);
        order["CreatedAt"].ToUniversalTime().ShouldBe(Milliseconds(_response.CreatedAt));
        order["UpdatedAt"].IsBsonNull.ShouldBeTrue();
        order["Status"].AsInt32.ShouldBe((int)OrderStatus.Created);
        var storedLines = order["Lines"].AsBsonArray.Select(item => item.AsBsonDocument).ToArray();
        storedLines.Length.ShouldBe(_response.Lines.Count);
        foreach (var line in _response.Lines)
        {
            var stored = storedLines.Single(item => GuidValue(item, "ProductVersionId") == line.ProductVersionId);
            Fields(stored, "ProductVersionId", "Quantity");
            stored["Quantity"].AsInt32.ShouldBe(line.Quantity);
            var version = after["productVersions"].Single(document => Id(document) == line.ProductVersionId);
            Fields(version, "_id", "IsActive", "CreatedAt", "DeactivatedAt", "ProductId", "PriceAmount", "PriceCurrency", "Name", "Brand");
            GuidValue(version, "ProductId").ShouldBe(line.ProductVersion.ProductId);
            version["IsActive"].AsBoolean.ShouldBe(line.ProductVersion.IsActive);
            version["CreatedAt"].ToUniversalTime().ShouldBe(Milliseconds(line.ProductVersion.CreatedAt));
            version["DeactivatedAt"].IsBsonNull.ShouldBeTrue();
            version["PriceAmount"].ToDecimal().ShouldBe(line.ProductVersion.PriceAmount);
            version["PriceCurrency"].AsString.ShouldBe(line.ProductVersion.PriceCurrency);
            version["Name"].AsString.ShouldBe(line.ProductVersion.Name);
            version["Brand"].AsString.ShouldBe(line.ProductVersion.Brand);
        }
        var beforeCart = _before["shoppingCarts"].Single(document => Id(document) == _cartId);
        var cart = after["shoppingCarts"].Single(document => Id(document) == _cartId);
        Fields(cart, "_id", "ClientId", "CreatedAt", "UpdatedAt", "Lines");
        GuidValue(cart, "ClientId").ShouldBe(_clientId);
        cart["CreatedAt"].ShouldBe(beforeCart["CreatedAt"]);
        beforeCart["Lines"].AsBsonArray.Count.ShouldBe(Number(expected, "CartLinesBefore"));
        cart["Lines"].AsBsonArray.Count.ShouldBe(Number(expected, "CartLinesAfter"));
        cart["Lines"].AsBsonArray.ShouldBeEmpty();
        Required(expected, "CartId").ShouldBe("retained");
        Required(expected, "CartCreatedAt").ShouldBe("retained");
        Required(expected, "CartUpdatedAt").ShouldBe("advanced");
        cart["UpdatedAt"].ToUniversalTime().ShouldBeGreaterThan(beforeCart["UpdatedAt"].ToUniversalTime());
        InWindow(cart["UpdatedAt"].ToUniversalTime(), Milliseconds(_started), Milliseconds(_completed));
        Required(expected, "OtherClients").ShouldBe("unchanged");
        var otherBefore = _before["shoppingCarts"].Where(document => Id(document) != _cartId).ToArray();
        var otherAfter = after["shoppingCarts"].Where(document => Id(document) != _cartId).ToArray();
        otherBefore.Length.ShouldBe(1);
        otherAfter.ShouldBe(otherBefore);
        _committed = after;
        context.RecordEvidence("Checkout coordinated state verified", new { orderId = _response.Id, clientId = _clientId, snapshots = _response.Lines.Select(line => line.ProductVersionId), cartCleared = true, otherClientsPreserved = true, catalogRequests = context.Factory.CatalogRequestCount });
    }

    [Then("snapshot reads retain this committed state without catalog calls")]
    public async Task ThenReads(Table table)
    {
        var expected = Values(table);
        context.RecordEvidence("Checkout snapshot read expectations", expected);
        _response.ShouldNotBeNull();
        _committed.ShouldNotBeNull();
        _committed["orders"].Length.ShouldBe(Number(expected, "Orders"));
        _committed["productVersions"].Length.ShouldBe(Number(expected, "Snapshots"));
        Required(expected, "StoredState").ShouldBe("unchanged");
        Required(expected, "CartLines").ShouldBe("0");
        Required(expected, "OtherClients").ShouldBe("unchanged");
        var before = await context.Factory.GetCheckoutDocumentsAsync();
        AttachSnapshot("Checkout database before supporting reads", before);
        EqualSnapshot(before, _committed);
        using var byId = await context.HttpClient.GetAsync($"/orders/{_response.Id}");
        var read = await ReadOrderAsync(byId);
        EqualOrder(read, _response);
        using var byClient = await context.HttpClient.GetAsync($"/orders/client/{_clientId}");
        byClient.StatusCode.ShouldBe(HttpStatusCode.OK);
        var orders = await byClient.Content.ReadFromJsonAsync<OrderResponseDto[]>(context.JsonOptions);
        orders.ShouldNotBeNull();
        EqualOrder(orders.ShouldHaveSingleItem(), _response);
        foreach (var clientId in new[] { _clientId, _otherClientId })
        {
            using var cartRead = await context.HttpClient.GetAsync($"/shopping-carts/client/{clientId}");
            cartRead.StatusCode.ShouldBe(HttpStatusCode.OK);
            var cart = await cartRead.Content.ReadFromJsonAsync<ShoppingCartResponseDto>(context.JsonOptions);
            cart.ShouldNotBeNull();
            var stored = _committed["shoppingCarts"].Single(document => GuidValue(document, "ClientId") == clientId);
            cart.Id.ShouldBe(Id(stored));
            cart.ClientId.ShouldBe(clientId);
            cart.CreatedAt.ShouldBe(stored["CreatedAt"].ToUniversalTime());
            cart.UpdatedAt.ShouldBe(stored["UpdatedAt"].ToUniversalTime());
            cart.Lines.ShouldBe(stored["Lines"].AsBsonArray.Select(item => new ShoppingCartLineResponseDto { ProductId = GuidValue(item.AsBsonDocument, "ProductId"), Quantity = item["Quantity"].AsInt32 }), ignoreOrder: true);
        }
        var after = await context.Factory.GetCheckoutDocumentsAsync();
        AttachSnapshot("Checkout database after supporting reads", after);
        EqualSnapshot(after, _committed);
        context.Factory.CatalogRequestCount.ShouldBe(Number(expected, "CatalogRequests"));
        context.RecordEvidence("Checkout immutable reads verified", new { orderId = _response.Id, completeOrderUnchanged = true, allCollectionsUnchanged = true, catalogRequests = context.Factory.CatalogRequestCount });
    }

    [Then("the isolated Products API is stopped")]
    public async Task ThenStopCatalog()
    {
        await context.Factory.StopIsolatedCatalogAsync();
        context.RecordEvidence("Isolated Products API lifecycle", new { state = "stopped", operation = "Testcontainers.StopAsync", catalogRequests = context.Factory.CatalogRequestCount });
    }

    private BsonDocument CartDocument(Guid id, Guid clientId, IEnumerable<(Guid ProductId, int Quantity)> lines) => new()
    {
        ["_id"] = new BsonBinaryData(id, GuidRepresentation.Standard),
        ["ClientId"] = new BsonBinaryData(clientId, GuidRepresentation.Standard),
        ["CreatedAt"] = _createdAt,
        ["UpdatedAt"] = _updatedAt,
        ["Lines"] = new BsonArray(lines.Select(line => new BsonDocument { ["ProductId"] = new BsonBinaryData(line.ProductId, GuidRepresentation.Standard), ["Quantity"] = line.Quantity }))
    };

    private async Task<OrderResponseDto> ReadOrderAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var order = await response.Content.ReadFromJsonAsync<OrderResponseDto>(context.JsonOptions);
        order.ShouldNotBeNull();
        return order;
    }

    private void EqualOrder(OrderResponseDto actual, OrderResponseDto expected)
    {
        OrderResponseDto Normalize(OrderResponseDto order) => order with
        {
            CreatedAt = Milliseconds(order.CreatedAt),
            Lines = order.Lines.OrderBy(line => line.ProductVersionId).Select(line => line with
            {
                ProductVersion = line.ProductVersion with { CreatedAt = Milliseconds(line.ProductVersion.CreatedAt) }
            }).ToArray()
        };
        JsonElement.DeepEquals(JsonSerializer.SerializeToElement(Normalize(actual), context.JsonOptions), JsonSerializer.SerializeToElement(Normalize(expected), context.JsonOptions)).ShouldBeTrue();
    }

    private void AttachSnapshot(string name, Dictionary<string, BsonDocument[]> snapshot) => context.RecordEvidence(name,
        snapshot.ToDictionary(pair => pair.Key, pair => JsonSerializer.Deserialize<JsonElement>(new BsonArray(pair.Value).ToJson())));

    private static void EqualSnapshot(Dictionary<string, BsonDocument[]> actual, Dictionary<string, BsonDocument[]> expected)
    {
        actual.Keys.Order().ShouldBe(expected.Keys.Order());
        foreach (var name in expected.Keys) actual[name].ShouldBe(expected[name]);
    }

    private static Dictionary<string, string> Values(Table table) => table.Rows.ToDictionary(row => row["Field"], row => row["Value"], StringComparer.Ordinal);
    private static string Required(IReadOnlyDictionary<string, string> values, string key) => values.TryGetValue(key, out var value) ? value : throw new InvalidOperationException($"Missing explicit checkout expectation: {key}");
    private static int Number(IReadOnlyDictionary<string, string> values, string key) => int.Parse(Required(values, key), CultureInfo.InvariantCulture);
    private static decimal Amount(IReadOnlyDictionary<string, string> values, string key) => decimal.Parse(Required(values, key), CultureInfo.InvariantCulture);
    private static CatalogProduct Product(TableRow row) => new(Guid.Parse(row["ProductId"]), row["Name"], row["Brand"], decimal.Parse(row["PriceAmount"], CultureInfo.InvariantCulture), row["PriceCurrency"]);
    private static Guid Id(BsonDocument document) => GuidValue(document, "_id");
    private static Guid GuidValue(BsonDocument document, string field) => document[field].AsBsonBinaryData.ToGuid();
    private static void Fields(BsonDocument document, params string[] names) => document.Names.Order().ShouldBe(names.Order());
    private static DateTime Milliseconds(DateTime value) => new(value.Ticks - value.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
    private static void InWindow(DateTime value, DateTime start, DateTime end)
    {
        value.Kind.ShouldBe(DateTimeKind.Utc);
        value.ShouldBeGreaterThanOrEqualTo(start);
        value.ShouldBeLessThanOrEqualTo(end);
    }

    private sealed record CatalogProduct(Guid ProductId, string Name, string Brand, decimal PriceAmount, string PriceCurrency);
    private sealed record CartProduct(CatalogProduct Product, int Quantity);
}
