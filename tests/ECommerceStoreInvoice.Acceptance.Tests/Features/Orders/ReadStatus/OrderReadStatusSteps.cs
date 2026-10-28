using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using ECommerceStoreInvoice.Application.Common.RequestsDto.Orders;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using MongoDB.Bson;
using Reqnroll;
using Shouldly;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Orders.ReadStatus;

/// <summary>Explicit stored fixtures and full response/state evidence for order reads and transitions.</summary>
[Binding]
public sealed class OrderReadStatusSteps(ScenarioApiContext context)
{
    private readonly Dictionary<string, Guid> _clients = new(StringComparer.Ordinal) { ["A"] = Guid.NewGuid(), ["B"] = Guid.NewGuid() };
    private readonly Dictionary<string, Guid> _ids = new(StringComparer.Ordinal);
    private readonly Dictionary<string, OrderResponseDto> _orders = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Guid> _snapshotIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<OrderLineResponseDto>> _lines = new(StringComparer.Ordinal);
    private readonly Guid _missingId = Guid.NewGuid();
    private Dictionary<string, BsonDocument[]>? _knownState;
    private Dictionary<string, BsonDocument[]>? _before;
    private OrderResponseDto[] _actual = [];
    private OrderResponseDto? _accepted;
    private string _method = string.Empty;
    private string _orderKey = string.Empty;
    private string _clientKey = string.Empty;
    private string _requestedStatus = string.Empty;
    private string _path = string.Empty;
    private DateTime _started;
    private DateTime _completed;

    [Given("these complete stored orders exist")]
    public void GivenOrders(Table table)
    {
        context.RecordEvidence("Order profile fixture table", Rows(table));
        foreach (var row in table.Rows)
        {
            var key = row["Order"];
            var id = Guid.NewGuid();
            _ids.Add(key, id);
            _lines.Add(key, []);
            _orders.Add(key, new OrderResponseDto
            {
                Id = id,
                ClientId = _clients[row["Client"]],
                Status = row["Status"],
                CreatedAt = Date(row["CreatedAt"]),
                UpdatedAt = OptionalDate(row["UpdatedAt"]),
                TotalAmount = 0,
                TotalCurrency = string.Empty,
                Lines = []
            });
        }
    }

    [Given("the stored orders reference these complete product snapshots")]
    public void GivenSnapshots(Table table)
    {
        context.RecordEvidence("Order snapshot fixture table", Rows(table));
        foreach (var row in table.Rows)
        {
            var versionId = Guid.NewGuid();
            _snapshotIds.Add(row["Snapshot"], versionId);
            _lines[row["Order"]].Add(new OrderLineResponseDto
            {
                ProductVersionId = versionId,
                Quantity = Integer(row["Quantity"]),
                LineTotalAmount = Money(row["PriceAmount"]) * Integer(row["Quantity"]),
                ProductVersion = new ProductVersionResponseDto
                {
                    Id = versionId,
                    ProductId = Guid.Parse(row["ProductId"]),
                    IsActive = bool.Parse(row["IsActive"]),
                    CreatedAt = Date(row["CreatedAt"]),
                    DeactivatedAt = OptionalDate(row["DeactivatedAt"]),
                    Name = row["Name"],
                    Brand = row["Brand"],
                    PriceAmount = Money(row["PriceAmount"]),
                    PriceCurrency = row["PriceCurrency"]
                }
            });
        }
        foreach (var key in _orders.Keys.ToArray())
        {
            var lines = _lines[key].ToArray();
            lines.ShouldNotBeEmpty();
            _orders[key] = _orders[key] with { Lines = lines, TotalAmount = lines.Sum(line => line.LineTotalAmount), TotalCurrency = lines[0].ProductVersion.PriceCurrency };
        }
        context.RecordEvidence("Resolved complete order fixtures", _orders);
        context.RecordEvidence("Order fixture identities", new { clients = _clients, orders = _ids, snapshots = _snapshotIds, missingOrderId = _missingId });
    }

    [Given("both clients have these populated carts")]
    public void GivenCarts(Table table)
    {
        context.RecordEvidence("Order ancillary cart fixture table", Rows(table));
        var documents = table.Rows.GroupBy(row => row["Client"]).Select(group =>
        {
            var first = group.First();
            return new BsonDocument
            {
                ["_id"] = Binary(Guid.NewGuid()),
                ["ClientId"] = Binary(_clients[group.Key]),
                ["CreatedAt"] = Date(first["CreatedAt"]),
                ["UpdatedAt"] = Date(first["UpdatedAt"]),
                ["Lines"] = new BsonArray(group.Select(row => new BsonDocument { ["ProductId"] = Binary(Guid.Parse(row["ProductId"])), ["Quantity"] = Integer(row["Quantity"]) }))
            };
        }).ToArray();
        documents.Length.ShouldBe(2);
        _knownState = new Dictionary<string, BsonDocument[]>(StringComparer.Ordinal) { ["shoppingCarts"] = documents };
    }

    [Given("a completed invoice and client version populate the remaining collections")]
    public async Task GivenRemainingCollections(Table table)
    {
        var values = Values(table);
        context.RecordEvidence("Order ancillary invoice and client version fixture table", values);
        _knownState.ShouldNotBeNull();
        var order = _orders[Required(values, "Order")];
        order.Status.ShouldBe("Paid");
        order.ClientId.ShouldBe(_clients[Required(values, "Client")]);
        var clientVersionId = Guid.NewGuid();
        _knownState.Add("clientDataVersions",
        [
            new BsonDocument
            {
                ["_id"] = Binary(clientVersionId), ["ClientId"] = Binary(order.ClientId),
                ["ClientName"] = Required(values, "ClientName"), ["PostalCode"] = Required(values, "PostalCode"),
                ["City"] = Required(values, "City"), ["Street"] = Required(values, "Street"),
                ["BuildingNumber"] = Required(values, "BuildingNumber"), ["ApartmentNumber"] = Required(values, "ApartmentNumber"),
                ["PhoneNumber"] = Required(values, "PhoneNumber"), ["PhonePrefix"] = Required(values, "PhonePrefix"),
                ["AddressEmail"] = Required(values, "AddressEmail"), ["CreatedAt"] = Date(Required(values, "ClientVersionCreatedAt"))
            }
        ]);
        Required(values, "GenerationStatus").ShouldBe("Completed");
        Required(values, "GenerationAttemptId").ShouldBe("<null>");
        Required(values, "GenerationLeaseUntil").ShouldBe("<null>");
        _knownState.Add("invoices",
        [
            new BsonDocument
            {
                ["_id"] = Binary(Guid.NewGuid()), ["OrderId"] = Binary(order.Id), ["ClientDataVersionId"] = Binary(clientVersionId),
                ["StorageUrl"] = Required(values, "StorageUrl"), ["CreatedAt"] = Date(Required(values, "InvoiceCreatedAt")),
                ["GenerationStatus"] = Required(values, "GenerationStatus"), ["GenerationAttemptId"] = BsonNull.Value, ["GenerationLeaseUntil"] = BsonNull.Value
            }
        ]);
        _knownState.Add("orders", _orders.Values.Select(OrderDocument).ToArray());
        _knownState.Add("productVersions", _orders.Values.SelectMany(item => item.Lines).Select(line => VersionDocument(line.ProductVersion)).ToArray());
        foreach (var name in _knownState.Keys.ToArray())
            _knownState[name] = _knownState[name].OrderBy(Id).ToArray();
        AttachSnapshot("Order complete expected fixture database", _knownState);
        await context.Factory.SeedOrderReadDocumentsAsync(_knownState);
        var actual = await context.Factory.GetCheckoutDocumentsAsync();
        AttachSnapshot("Order complete actual fixture database", actual);
        EqualSnapshot(actual, _knownState);
        actual.Values.ShouldAllBe(documents => documents.Length > 0);
        context.Factory.CatalogRequestCount.ShouldBe(0);
    }

    [When("I execute this order read or status request")]
    public async Task WhenRequest(Table table)
    {
        var values = Values(table);
        _method = Required(values, "Method");
        _orderKey = Required(values, "Order");
        _clientKey = Required(values, "Client");
        _requestedStatus = Required(values, "Status");
        var template = Required(values, "Endpoint");
        _path = template.Replace("{orderId}", ResolveOrder(_orderKey).ToString()).Replace("{clientId}", ResolveClient(_clientKey).ToString());
        _knownState.ShouldNotBeNull();
        _before = await context.Factory.GetCheckoutDocumentsAsync();
        AttachSnapshot("Order database before primary request", _before);
        EqualSnapshot(_before, _knownState);
        _actual = [];
        _accepted = null;
        context.Response?.Dispose();
        context.RecordEvidence("Resolved order request", new { method = _method, path = _path, order = _orderKey, client = _clientKey, requestedStatus = _requestedStatus });
        _started = DateTime.UtcNow;
        if (_method == "GET")
        {
            _requestedStatus.ShouldBe("<none>");
            (template is "/orders/{orderId}" or "/orders/client/{clientId}").ShouldBeTrue();
            context.Response = await context.HttpClient.GetAsync(_path);
        }
        else
        {
            _method.ShouldBe("PATCH");
            template.ShouldBe("/orders/{orderId}/status");
            context.Response = await context.HttpClient.PatchAsJsonAsync(_path, new UpdateOrderStatusRequestDto { Status = _requestedStatus }, context.JsonOptions);
        }
        _completed = DateTime.UtcNow;
        context.RecordEvidence("Order primary request window", new { method = _method, path = _path, startedAt = _started, completedAt = _completed });
    }

    [Then("the order response contains these complete profiles")]
    public async Task ThenProfiles(Table table)
    {
        context.RecordEvidence("Order response profile expectations", Rows(table));
        context.Response.ShouldNotBeNull();
        context.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
        context.Response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        _actual = _clientKey == "<none>"
            ? [await ReadOrderAsync(context.Response)]
            : await ReadOrdersAsync(context.Response);
        _actual.ShouldNotBeNull();
        context.RecordEvidence("Complete actual order response profiles", _actual);
        _actual.Length.ShouldBe(table.Rows.Count);
        var selected = _clientKey == "<none>" ? new[] { _orders[_orderKey] } : _orders.Values.Where(order => order.ClientId == ResolveClient(_clientKey)).ToArray();
        _actual.Select(order => order.Id).Order().ShouldBe(selected.Select(order => order.Id).Order());
        table.Rows.Select(row => row["Order"]).Distinct().Count().ShouldBe(table.Rows.Count);
        foreach (var row in table.Rows)
        {
            var expected = _orders[row["Order"]];
            var actual = _actual.Single(order => order.Id == expected.Id);
            actual.ClientId.ShouldBe(_clients[row["Client"]]);
            actual.Status.ShouldBe(row["Status"]);
            actual.CreatedAt.ShouldBe(Date(row["CreatedAt"]));
            actual.TotalAmount.ShouldBe(Money(row["TotalAmount"]));
            actual.TotalCurrency.ShouldBe(row["TotalCurrency"]);
            actual.Lines.Count.ShouldBe(Integer(row["LinesCount"]));
            if (row["UpdatedAt"] == "generated")
            {
                _method.ShouldBe("PATCH");
                expected.Status.ShouldBe("Created");
                (_requestedStatus is "Paid" or "Cancelled").ShouldBeTrue();
                actual.Status.ShouldBe(_requestedStatus);
                actual.UpdatedAt.ShouldNotBeNull();
                InWindow(actual.UpdatedAt.Value);
                expected = expected with { Status = _requestedStatus, UpdatedAt = Milliseconds(actual.UpdatedAt.Value) };
                _accepted = expected;
            }
            else
                actual.UpdatedAt.ShouldBe(OptionalDate(row["UpdatedAt"]));
            EqualOrder(actual, expected);
        }
    }

    [Then("the order response contains these complete saved lines")]
    public void ThenLines(Table table)
    {
        context.RecordEvidence("Order response line expectations", Rows(table));
        _actual.Sum(order => order.Lines.Count).ShouldBe(table.Rows.Count);
        foreach (var row in table.Rows)
        {
            var order = _actual.Single(item => item.Id == _ids[row["Order"]]);
            var line = order.Lines.Single(item => item.ProductVersionId == _snapshotIds[row["Snapshot"]]);
            line.Quantity.ShouldBe(Integer(row["Quantity"]));
            line.LineTotalAmount.ShouldBe(Money(row["LineTotalAmount"]));
            var version = line.ProductVersion;
            version.Id.ShouldBe(line.ProductVersionId);
            version.ProductId.ShouldBe(Guid.Parse(row["ProductId"]));
            version.IsActive.ShouldBe(bool.Parse(row["IsActive"]));
            version.CreatedAt.ShouldBe(Date(row["CreatedAt"]));
            version.DeactivatedAt.ShouldBe(OptionalDate(row["DeactivatedAt"]));
            version.Name.ShouldBe(row["Name"]);
            version.Brand.ShouldBe(row["Brand"]);
            version.PriceAmount.ShouldBe(Money(row["PriceAmount"]));
            version.PriceCurrency.ShouldBe(row["PriceCurrency"]);
            line.LineTotalAmount.ShouldBe(version.PriceAmount * line.Quantity);
        }
    }

    [Then("the order response is this exact empty collection")]
    public async Task ThenEmpty(Table table)
    {
        var values = Values(table);
        context.RecordEvidence("Empty order response expectations", values);
        Integer(Required(values, "StatusCode")).ShouldBe(200);
        Required(values, "OrdersCount").ShouldBe("0");
        Required(values, "IsEmpty").ShouldBe("true");
        Required(values, "Body").ShouldBe("[]");
        _orders.Values.ShouldNotContain(order => order.ClientId == ResolveClient(_clientKey));
        _orders.Values.ShouldNotBeEmpty();
        context.Response.ShouldNotBeNull();
        context.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await context.Response.Content.ReadAsStringAsync();
        using var actual = JsonDocument.Parse(body);
        context.RecordEvidence("Actual empty order response", actual.RootElement.Clone());
        actual.RootElement.ValueKind.ShouldBe(JsonValueKind.Array);
        actual.RootElement.GetArrayLength().ShouldBe(0);
    }

    [Then("the order request returns this complete problem")]
    public async Task ThenProblem(Table table)
    {
        var expected = Values(table);
        context.RecordEvidence("Order problem expectations", expected);
        context.Response.ShouldNotBeNull();
        ((int)context.Response.StatusCode).ShouldBe(Integer(Required(expected, "StatusCode")));
        context.Response.Content.Headers.ContentType?.MediaType.ShouldBe(Required(expected, "ContentType"));
        using var document = JsonDocument.Parse(await context.Response.Content.ReadAsStringAsync());
        var problem = document.RootElement;
        context.RecordEvidence("Complete actual order problem", problem.Clone());
        problem.GetProperty("status").GetInt32().ShouldBe(Integer(Required(expected, "StatusCode")));
        problem.GetProperty("title").GetString().ShouldBe(Required(expected, "Title"));
        problem.GetProperty("type").GetString().ShouldBe(Required(expected, "Type"));
        problem.GetProperty("detail").GetString().ShouldBe(Required(expected, "Detail").Replace("{orderId}", ResolveOrder(_orderKey).ToString()));
        Required(expected, "Instance").ShouldBe("request-path");
        problem.GetProperty("instance").GetString().ShouldBe(_path);
        Required(expected, "TraceId").ShouldBe("generated");
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        if (context.Response.StatusCode == HttpStatusCode.NotFound)
        {
            Required(expected, "ErrorsCount").ShouldBe("absent");
            problem.EnumerateObject().Select(property => property.Name).Order().ShouldBe(new[] { "type", "title", "status", "detail", "instance", "traceId" }.Order());
        }
        else
        {
            var errors = problem.GetProperty("errors");
            errors.GetArrayLength().ShouldBe(Integer(Required(expected, "ErrorsCount")));
            errors.GetArrayLength().ShouldBe(1);
            var error = errors[0];
            error.GetProperty("name").GetString().ShouldBe(Required(expected, "ErrorName"));
            error.GetProperty("entity").GetString().ShouldBe(Required(expected, "ErrorEntity"));
            error.GetProperty("message").GetString().ShouldBe(Required(expected, "ErrorMessage"));
            error.EnumerateObject().Select(property => property.Name).Order().ShouldBe(new[] { "name", "entity", "message" }.Order());
            problem.EnumerateObject().Select(property => property.Name).Order().ShouldBe(new[] { "type", "title", "status", "detail", "instance", "traceId", "errors" }.Order());
        }
    }

    [Then("the order operation has exactly this stored effect")]
    public async Task ThenState(Table table)
    {
        var expected = Values(table);
        context.RecordEvidence("Order persistence expectations", expected);
        _before.ShouldNotBeNull();
        var after = await context.Factory.GetCheckoutDocumentsAsync();
        AttachSnapshot("Order database after primary request", after);
        var wanted = _before.ToDictionary(pair => pair.Key, pair => pair.Value.Select(document => document.DeepClone().AsBsonDocument).ToArray());
        var effect = Required(expected, "Effect");
        if (effect == "status-updated")
        {
            _method.ShouldBe("PATCH");
            _accepted.ShouldNotBeNull();
            Required(expected, "ChangedOrder").ShouldBe(_orderKey);
            var order = wanted["orders"].Single(document => Id(document) == _ids[_orderKey]);
            order["Status"].AsInt32.ShouldBe((int)OrderStatus.Created);
            order["Status"] = (int)Enum.Parse<OrderStatus>(_requestedStatus);
            order["UpdatedAt"] = _accepted.UpdatedAt!.Value;
            _orders[_orderKey] = _accepted;
        }
        else
        {
            effect.ShouldBe("unchanged");
            Required(expected, "ChangedOrder").ShouldBe("none");
        }
        EqualSnapshot(after, wanted);
        foreach (var (name, key) in new[] { ("orders", "Orders"), ("productVersions", "Snapshots"), ("shoppingCarts", "Carts"), ("invoices", "Invoices"), ("clientDataVersions", "ClientVersions") })
        {
            after[name].Length.ShouldBe(Integer(Required(expected, key)));
            after[name].Length.ShouldBe(_before[name].Length);
            after[name].ShouldNotBeEmpty();
        }
        Required(expected, "CatalogRequests").ShouldBe("0");
        context.Factory.CatalogRequestCount.ShouldBe(0);
        _knownState = after;
        context.RecordEvidence("Order primary persistence verified", new { method = _method, path = _path, effect, changedOrder = Required(expected, "ChangedOrder"), allowedChangedFields = effect == "status-updated" ? new[] { "Status", "UpdatedAt" } : Array.Empty<string>(), allOtherStateUnchanged = true, catalogRequests = context.Factory.CatalogRequestCount });
    }

    [Then("complete supporting order and cart reads preserve all stored state")]
    public async Task ThenSupportingReads()
    {
        _knownState.ShouldNotBeNull();
        var before = await context.Factory.GetCheckoutDocumentsAsync();
        AttachSnapshot("Order database before supporting reads", before);
        EqualSnapshot(before, _knownState);
        foreach (var expected in _orders.Values)
        {
            using var response = await context.HttpClient.GetAsync($"/orders/{expected.Id}");
            EqualOrder(await ReadOrderAsync(response), expected);
        }
        foreach (var clientId in _clients.Values)
        {
            using var response = await context.HttpClient.GetAsync($"/orders/client/{clientId}");
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var actual = await ReadOrdersAsync(response);
            var expected = _orders.Values.Where(order => order.ClientId == clientId).ToArray();
            actual.Select(order => order.Id).Order().ShouldBe(expected.Select(order => order.Id).Order());
            foreach (var order in actual) EqualOrder(order, expected.Single(item => item.Id == order.Id));
            using var cartResponse = await context.HttpClient.GetAsync($"/shopping-carts/client/{clientId}");
            cartResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
            var cart = await cartResponse.Content.ReadFromJsonAsync<ShoppingCartResponseDto>(context.JsonOptions);
            cart.ShouldNotBeNull();
            var stored = _knownState["shoppingCarts"].Single(document => document["ClientId"].AsBsonBinaryData.ToGuid() == clientId);
            cart.Id.ShouldBe(Id(stored));
            cart.ClientId.ShouldBe(clientId);
            cart.CreatedAt.ShouldBe(stored["CreatedAt"].ToUniversalTime());
            cart.UpdatedAt.ShouldBe(stored["UpdatedAt"].ToUniversalTime());
            cart.Lines.ShouldBe(stored["Lines"].AsBsonArray.Select(item => new ShoppingCartLineResponseDto { ProductId = item["ProductId"].AsBsonBinaryData.ToGuid(), Quantity = item["Quantity"].AsInt32 }), ignoreOrder: true);
        }
        var after = await context.Factory.GetCheckoutDocumentsAsync();
        AttachSnapshot("Order database after supporting reads", after);
        EqualSnapshot(after, _knownState);
        context.Factory.CatalogRequestCount.ShouldBe(0);
        context.RecordEvidence("Order supporting reads verified", new { orders = _orders.Keys.ToArray(), clientCounts = _clients.ToDictionary(pair => pair.Key, pair => _orders.Values.Count(order => order.ClientId == pair.Value)), allCollectionsUnchanged = true, catalogRequests = 0 });
    }

    private Guid ResolveOrder(string key) => key switch { "<none>" => Guid.Empty, "missing" => _missingId, "empty" => Guid.Empty, _ => _ids[key] };
    private Guid ResolveClient(string key) => key == "<none>" ? Guid.Empty : _clients[key];
    private static BsonBinaryData Binary(Guid id) => new(id, GuidRepresentation.Standard);
    private static Guid Id(BsonDocument document) => document["_id"].AsBsonBinaryData.ToGuid();
    private static int Integer(string value) => int.Parse(value, CultureInfo.InvariantCulture);
    private static decimal Money(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);
    private static DateTime Date(string value) => DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    private static DateTime? OptionalDate(string value) => value == "<null>" ? null : Date(value);
    private static Dictionary<string, string> Values(Table table) => table.Rows.ToDictionary(row => row["Field"], row => row["Value"], StringComparer.Ordinal);
    private static object Rows(Table table) => table.Rows.Select(row => row.ToDictionary(pair => pair.Key, pair => pair.Value)).ToArray();
    private static string Required(IReadOnlyDictionary<string, string> values, string key) => values.TryGetValue(key, out var value) ? value : throw new InvalidOperationException($"Missing explicit order expectation: {key}");
    private static DateTime Milliseconds(DateTime value) => new(value.Ticks - value.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
    private static DateTime? Milliseconds(DateTime? value) => value is null ? null : Milliseconds(value.Value);
    private void InWindow(DateTime value)
    {
        value.Kind.ShouldBe(DateTimeKind.Utc);
        value.ShouldBeGreaterThanOrEqualTo(_started);
        value.ShouldBeLessThanOrEqualTo(_completed);
    }

    private async Task<OrderResponseDto> ReadOrderAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        CompleteProfile(document.RootElement);
        var order = JsonSerializer.Deserialize<OrderResponseDto>(document.RootElement, context.JsonOptions);
        order.ShouldNotBeNull();
        return order;
    }

    private async Task<OrderResponseDto[]> ReadOrdersAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.ValueKind.ShouldBe(JsonValueKind.Array);
        foreach (var order in document.RootElement.EnumerateArray()) CompleteProfile(order);
        var orders = JsonSerializer.Deserialize<OrderResponseDto[]>(document.RootElement, context.JsonOptions);
        orders.ShouldNotBeNull();
        return orders;
    }

    private static void CompleteProfile(JsonElement order)
    {
        Names(order, "id", "clientId", "createdAt", "updatedAt", "status", "totalAmount", "totalCurrency", "lines");
        order.GetProperty("lines").ValueKind.ShouldBe(JsonValueKind.Array);
        foreach (var line in order.GetProperty("lines").EnumerateArray())
        {
            Names(line, "productVersionId", "quantity", "productVersion", "lineTotalAmount");
            Names(line.GetProperty("productVersion"), "id", "productId", "isActive", "createdAt", "deactivatedAt", "name", "brand", "priceAmount", "priceCurrency");
        }
    }

    private static void Names(JsonElement value, params string[] names)
    {
        value.ValueKind.ShouldBe(JsonValueKind.Object);
        value.EnumerateObject().Select(property => property.Name).Order().ShouldBe(names.Order());
    }

    private void EqualOrder(OrderResponseDto actual, OrderResponseDto expected)
    {
        OrderResponseDto Normalize(OrderResponseDto order) => order with
        {
            CreatedAt = Milliseconds(order.CreatedAt),
            UpdatedAt = Milliseconds(order.UpdatedAt),
            Lines = order.Lines.OrderBy(line => line.ProductVersionId).Select(line => line with
            {
                ProductVersion = line.ProductVersion with { CreatedAt = Milliseconds(line.ProductVersion.CreatedAt), DeactivatedAt = Milliseconds(line.ProductVersion.DeactivatedAt) }
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

    private static BsonDocument OrderDocument(OrderResponseDto order) => new()
    {
        ["_id"] = Binary(order.Id),
        ["ClientId"] = Binary(order.ClientId),
        ["Lines"] = new BsonArray(order.Lines.Select(line => new BsonDocument { ["ProductVersionId"] = Binary(line.ProductVersionId), ["Quantity"] = line.Quantity })),
        ["CreatedAt"] = order.CreatedAt,
        ["UpdatedAt"] = order.UpdatedAt is { } updated ? new BsonDateTime(updated) : BsonNull.Value,
        ["Status"] = (int)Enum.Parse<OrderStatus>(order.Status)
    };

    private static BsonDocument VersionDocument(ProductVersionResponseDto version) => new()
    {
        ["_id"] = Binary(version.Id),
        ["ProductId"] = Binary(version.ProductId),
        ["IsActive"] = version.IsActive,
        ["CreatedAt"] = version.CreatedAt,
        ["DeactivatedAt"] = version.DeactivatedAt is { } deactivated ? new BsonDateTime(deactivated) : BsonNull.Value,
        ["Name"] = version.Name,
        ["Brand"] = version.Brand,
        ["PriceAmount"] = version.PriceAmount,
        ["PriceCurrency"] = version.PriceCurrency
    };
}
