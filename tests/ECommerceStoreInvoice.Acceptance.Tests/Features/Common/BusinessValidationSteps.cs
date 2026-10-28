using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MongoDB.Bson;
using Reqnroll;
using Shouldly;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Common;

[Binding]
public sealed class BusinessValidationSteps(ScenarioApiContext context)
{
    private static readonly string[] ClientFields = ["ClientName", "PostalCode", "City", "Street", "BuildingNumber", "ApartmentNumber", "PhoneNumber", "PhonePrefix", "AddressEmail"];
    private readonly Dictionary<string, Guid> _clients = new(StringComparer.Ordinal) { ["A"] = Guid.NewGuid(), ["B"] = Guid.NewGuid() };
    private readonly Dictionary<string, Guid> _versions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Guid> _orders = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Guid> _invoices = new(StringComparer.Ordinal);
    private Dictionary<string, BsonDocument[]> _known = new(StringComparer.Ordinal);
    private Dictionary<string, BsonDocument[]>? _before;
    private readonly Dictionary<string, Dictionary<string, string>> _defaults = new(StringComparer.Ordinal);
    private Dictionary<string, string> _signature = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Guid> _routeOverrides = new(StringComparer.Ordinal);
    private JsonObject? _body;
    private string _operation = string.Empty;
    private string _path = string.Empty;
    private DateTime _started;
    private DateTime _finished;

    [Given("these complete client histories support business validation")]
    public void GivenVersions(Table table)
    {
        context.RecordEvidence("Business client fixture table", Rows(table));
        _known.Add("clientDataVersions", table.Rows.Select(row =>
        {
            var id = Guid.NewGuid();
            _versions.Add(row["Version"], id);
            var doc = new BsonDocument { ["_id"] = Binary(id), ["ClientId"] = Binary(_clients[row["Client"]]) };
            foreach (var field in ClientFields) doc[field] = row[field];
            doc["CreatedAt"] = Date(row["CreatedAt"]);
            return doc;
        }).ToArray());
    }

    [Given("these exact stored order snapshots support business validation")]
    public void GivenOrders(Table table)
    {
        context.RecordEvidence("Business order and snapshot fixture table", Rows(table));
        var orders = new List<BsonDocument>();
        var products = new List<BsonDocument>();
        foreach (var row in table.Rows)
        {
            var id = Guid.NewGuid();
            _orders.Add(row["Order"], id);
            var versionId = Guid.NewGuid();
            orders.Add(new BsonDocument
            {
                ["_id"] = Binary(id),
                ["ClientId"] = Binary(_clients[row["Client"]]),
                ["Lines"] = new BsonArray { new BsonDocument { ["ProductVersionId"] = Binary(versionId), ["Quantity"] = Integer(row["Quantity"]) } },
                ["CreatedAt"] = Date(row["CreatedAt"]),
                ["UpdatedAt"] = NullableDate(row["UpdatedAt"]),
                ["Status"] = Status(row["Status"])
            });
            products.Add(new BsonDocument
            {
                ["_id"] = Binary(versionId),
                ["ProductId"] = Binary(Guid.Parse(row["ProductId"])),
                ["CreatedAt"] = Date(row["CreatedAt"]),
                ["DeactivatedAt"] = BsonNull.Value,
                ["IsActive"] = true,
                ["Name"] = row["Name"],
                ["Brand"] = row["Brand"],
                ["PriceAmount"] = decimal.Parse(row["PriceAmount"], CultureInfo.InvariantCulture),
                ["PriceCurrency"] = row["PriceCurrency"]
            });
        }
        _known.Add("orders", orders.ToArray());
        _known.Add("productVersions", products.ToArray());
    }

    [Given("these populated carts support business validation")]
    public void GivenCarts(Table table)
    {
        context.RecordEvidence("Business cart fixture table", Rows(table));
        _known.Add("shoppingCarts", table.Rows.GroupBy(row => row["Client"]).Select(group => new BsonDocument
        {
            ["_id"] = Binary(Guid.NewGuid()),
            ["ClientId"] = Binary(_clients[group.Key]),
            ["CreatedAt"] = Date(group.First()["CreatedAt"]),
            ["UpdatedAt"] = Date(group.First()["UpdatedAt"]),
            ["Lines"] = new BsonArray(group.Select(row => new BsonDocument { ["ProductId"] = Binary(Guid.Parse(row["ProductId"])), ["Quantity"] = Integer(row["Quantity"]) }))
        }).ToArray());
    }

    [Given("these completed invoice references finish the populated business fixture")]
    public async Task GivenInvoices(Table table)
    {
        context.RecordEvidence("Business invoice fixture table", Rows(table));
        _known.Add("invoices", table.Rows.Select(row =>
        {
            var id = Guid.NewGuid();
            _invoices.Add(row["Invoice"], id);
            return new BsonDocument
            {
                ["_id"] = Binary(id),
                ["OrderId"] = Binary(_orders[row["Order"]]),
                ["ClientDataVersionId"] = Binary(_versions[row["Version"]]),
                ["StorageUrl"] = row["StorageUrl"],
                ["CreatedAt"] = Date(row["CreatedAt"]),
                ["GenerationStatus"] = "Completed",
                ["GenerationAttemptId"] = BsonNull.Value,
                ["GenerationLeaseUntil"] = BsonNull.Value
            };
        }).ToArray());
        _known = Sorted(_known);
        Attach("Business expected fixture database", _known);
        await context.Factory.SeedOrderReadDocumentsAsync(_known);
        var actual = await context.Factory.GetCheckoutDocumentsAsync();
        Attach("Business actual fixture database", actual);
        EqualState(actual, _known);
        actual.Values.ShouldAllBe(documents => documents.Length > 0);
        context.RecordEvidence("Business fixture identities", new { clients = _clients, versions = _versions, orders = _orders, invoices = _invoices });
        context.Factory.CatalogRequestCount.ShouldBe(0);
    }

    [Given("business requests have these complete defaults")]
    public void GivenDefaults(Table table)
    {
        context.RecordEvidence("Business complete default request table", Rows(table));
        foreach (var row in table.Rows) _defaults.Add(row["Operation"], row.ToDictionary(pair => pair.Key, pair => pair.Value));
    }

    [Given("business validation problems have this complete signature")]
    public void GivenSignature(Table table)
    {
        _signature = Values(table);
        context.RecordEvidence("Business complete problem signature table", _signature);
    }

    [Given("the selected business request is {string}")]
    public void GivenRequest(string operation)
    {
        _operation = operation;
        _body = JsonNode.Parse(_defaults[operation]["BodyJson"])?.AsObject();
        context.RecordEvidence("Business selected default request", new { operation, request = _body });
    }

    [Given("I change exactly these business JSON values")]
    public void GivenChanges(Table table)
    {
        _body.ShouldNotBeNull();
        context.RecordEvidence("Business isolated JSON changes table", Rows(table));
        foreach (var row in table.Rows)
        {
            var parts = row["Pointer"].Split('/').Skip(1).Select(part => part.Replace("~1", "/").Replace("~0", "~")).ToArray();
            parts.Length.ShouldBeGreaterThan(0);
            JsonNode parent = _body;
            foreach (var part in parts[..^1]) parent = (parent is JsonArray items ? items[Integer(part)] : parent[part]) ?? throw new InvalidOperationException("Missing JSON pointer parent.");
            var value = JsonNode.Parse(row["JsonValue"]);
            if (parent is JsonArray array) array[Integer(parts[^1])] = value;
            else
            {
                parent.AsObject().ContainsKey(parts[^1]).ShouldBeTrue();
                parent[parts[^1]] = value;
            }
        }
        context.RecordEvidence("Business changed request JSON", _body);
    }

    [Given("business route parameter {string} is the empty identifier")]
    public void GivenEmpty(string parameter) => _routeOverrides.Add(parameter, Guid.Empty);

    [When("I submit the selected business request")]
    public async Task WhenSubmit()
    {
        var selected = _defaults[_operation];
        _path = selected["Path"];
        foreach (var pair in new Dictionary<string, Guid> { ["clientId"] = _clients["A"], ["orderId"] = _orders["target"] })
            _path = _path.Replace("{" + pair.Key + "}", (_routeOverrides.GetValueOrDefault(pair.Key, pair.Value)).ToString());
        _before = await context.Factory.GetCheckoutDocumentsAsync();
        Attach("Business database before primary request", _before);
        EqualState(_before, _known);
        using var request = new HttpRequestMessage(new HttpMethod(selected["Method"]), _path);
        if (_body is not null) request.Content = new StringContent(_body.ToJsonString(), Encoding.UTF8, "application/json");
        context.RecordEvidence("Business resolved primary request", new { operation = _operation, method = selected["Method"], path = _path, body = _body, emptyParameters = _routeOverrides });
        _started = DateTime.UtcNow;
        context.Response = await context.HttpClient.SendAsync(request);
        _finished = DateTime.UtcNow;
        context.RecordEvidence("Business primary request window", new { startedAt = _started, completedAt = _finished });
    }

    [Then("the complete business validation problem contains exactly these ordered errors")]
    public async Task ThenErrors(Table table)
    {
        context.RecordEvidence("Business exact ordered error expectations table", Rows(table));
        var response = context.Response ?? throw new InvalidOperationException("No business response.");
        ((int)response.StatusCode).ShouldBe(Integer(_signature["StatusCode"]));
        response.Content.Headers.ContentType?.MediaType.ShouldBe(_signature["ContentType"]);
        response.Content.Headers.ContentDisposition.ShouldBeNull();
        response.Headers.CacheControl!.ToString().Split(',').Select(value => value.Trim()).Order().ShouldBe(_signature["CacheControl"].Split(',').Select(value => value.Trim()).Order());
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var actual = json.RootElement;
        Names(actual, "type", "title", "status", "detail", "instance", "traceId", "errors");
        actual.GetProperty("status").GetInt32().ShouldBe(Integer(_signature["StatusCode"]));
        foreach (var field in new[] { "Title", "Type", "Detail" }) actual.GetProperty(Camel(field)).GetString().ShouldBe(_signature[field]);
        _signature["Instance"].ShouldBe("request-path");
        actual.GetProperty("instance").GetString().ShouldBe(_path);
        _signature["TraceId"].ShouldBe("generated");
        actual.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        var errors = actual.GetProperty("errors").EnumerateArray().ToArray();
        errors.Length.ShouldBe(table.Rows.Count);
        for (var index = 0; index < errors.Length; index++)
        {
            Names(errors[index], "name", "message", "entity");
            foreach (var field in new[] { "Name", "Message", "Entity" }) errors[index].GetProperty(Camel(field)).GetString().ShouldBe(table.Rows[index][field]);
        }
        context.RecordEvidence("Business complete actual validation problem", actual.Clone());
    }

    [Then("business rejection preserves every populated document and supporting read")]
    public async Task ThenRejected()
    {
        _before.ShouldNotBeNull();
        ((int)context.Response!.StatusCode).ShouldBe(400);
        await VerifyState(_before, "unchanged");
        await SupportingReads();
    }

    [Then("the complete business success has exactly this effect")]
    public async Task ThenAccepted(Table table)
    {
        var values = Values(table);
        context.RecordEvidence("Business success and persistence contract table", values);
        values["StatusCode"].ShouldBe("200");
        values["ContentType"].ShouldBe("application/json");
        _before.ShouldNotBeNull();
        _body.ShouldNotBeNull();
        using var actual = await SuccessJson(context.Response!);
        var expected = Clone(_before);
        var root = actual.RootElement;
        if (values["Effect"] == "append-client-version")
        {
            _operation.ShouldBe("CreateClientDataVersion");
            var id = root.GetProperty("id").GetGuid();
            id.ShouldNotBe(Guid.Empty);
            expected["clientDataVersions"].ShouldNotContain(doc => Id(doc) == id);
            var created = GeneratedDate(root, "createdAt");
            var profile = _body.DeepClone().AsObject();
            profile.Add("id", id.ToString());
            profile.Add("clientId", _clients["A"].ToString());
            profile.Add("createdAt", JsonValue.Create(created));
            EqualJson(root, profile);
            var doc = new BsonDocument { ["_id"] = Binary(id), ["ClientId"] = Binary(_clients["A"]) };
            foreach (var field in ClientFields) doc[field] = _body[Camel(field)] is { } value ? new BsonString(value.GetValue<string>()) : BsonNull.Value;
            doc["CreatedAt"] = Milliseconds(created);
            expected["clientDataVersions"] = expected["clientDataVersions"].Append(doc).OrderBy(Id).ToArray();
        }
        else if (values["Effect"] == "replace-cart-lines")
        {
            _operation.ShouldBe("UpdateShoppingCart");
            var cart = expected["shoppingCarts"].Single(doc => GuidField(doc, "ClientId") == _clients["A"]);
            var updated = GeneratedDate(root, "updatedAt");
            cart["UpdatedAt"] = Milliseconds(updated);
            cart["Lines"] = new BsonArray(_body["lines"]!.AsArray().Select(line => new BsonDocument
            {
                ["ProductId"] = Binary(Guid.Parse(line!["productId"]!.GetValue<string>())),
                ["Quantity"] = line["quantity"]!.GetValue<int>()
            }));
            var profile = CartProfile(cart);
            profile["updatedAt"] = JsonValue.Create(updated);
            EqualJson(root, profile);
        }
        else
        {
            values["Effect"].ShouldBe("update-order-status");
            _operation.ShouldBe("UpdateOrderStatus");
            var order = expected["orders"].Single(doc => Id(doc) == _orders["target"]);
            var updated = GeneratedDate(root, "updatedAt");
            order["UpdatedAt"] = Milliseconds(updated);
            new[] { "Paid", "Cancelled" }.ShouldContain(values["SavedStatus"]);
            order["Status"] = Status(values["SavedStatus"]);
            var profile = OrderProfile(order, expected);
            profile["updatedAt"] = JsonValue.Create(updated);
            EqualJson(root, profile);
        }
        context.RecordEvidence("Business complete actual success response", root.Clone());
        await VerifyState(expected, values["Effect"]);
        await SupportingReads();
    }

    private DateTime GeneratedDate(JsonElement value, string field)
    {
        var date = value.GetProperty(field).GetDateTime();
        date.Kind.ShouldBe(DateTimeKind.Utc);
        date.ShouldBeGreaterThanOrEqualTo(_started);
        date.ShouldBeLessThanOrEqualTo(_finished);
        return date;
    }

    private async Task VerifyState(Dictionary<string, BsonDocument[]> expected, string effect)
    {
        Attach("Business expected database after primary request", expected);
        var actual = await context.Factory.GetCheckoutDocumentsAsync();
        Attach("Business actual database after primary request", actual);
        EqualState(actual, expected);
        context.Factory.CatalogRequestCount.ShouldBe(0);
        _known = expected;
        context.RecordEvidence("Business primary persistence verified", new { operation = _operation, effect, collectionCounts = actual.ToDictionary(pair => pair.Key, pair => pair.Value.Length), catalogRequests = 0 });
    }

    private async Task SupportingReads()
    {
        Attach("Business database before supporting reads", await context.Factory.GetCheckoutDocumentsAsync());
        var expectations = new List<(string Path, JsonObject Expected)>();
        foreach (var cart in _known["shoppingCarts"])
            expectations.Add(($"/shopping-carts/client/{GuidField(cart, "ClientId")}", CartProfile(cart)));
        foreach (var client in _clients.Values)
        {
            var version = _known["clientDataVersions"].Where(doc => GuidField(doc, "ClientId") == client).MaxBy(doc => doc["CreatedAt"].ToUniversalTime())!;
            expectations.Add(($"/client-data-versions/client/{client}", ClientProfile(version)));
        }
        foreach (var order in _known["orders"])
            expectations.Add(($"/orders/{Id(order)}", OrderProfile(order, _known)));
        foreach (var invoice in _known["invoices"])
            expectations.Add(($"/invoices/{Id(invoice)}", InvoiceProfile(invoice)));
        expectations.Count.ShouldBe(9);
        context.RecordEvidence("Business supporting response expectations", expectations.Select(item => new { path = item.Path, expected = item.Expected }).ToArray());
        foreach (var item in expectations) await VerifyRead(item.Path, item.Expected);
        var after = await context.Factory.GetCheckoutDocumentsAsync();
        Attach("Business database after supporting reads", after);
        EqualState(after, _known);
        context.Factory.CatalogRequestCount.ShouldBe(0);
        context.RecordEvidence("Business supporting reads verified", new { completeCartReads = 2, completeLatestClientReads = 2, completeOrderReads = 3, completeHistoricalInvoiceReads = 2, allFiveCollectionsUnchanged = true, catalogRequests = 0 });
    }

    private async Task VerifyRead(string path, JsonObject expected)
    {
        using var response = await context.HttpClient.GetAsync(path);
        using var actual = await SuccessJson(response);
        EqualJson(actual.RootElement, expected);
    }

    private static async Task<JsonDocument> SuccessJson(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    private static JsonObject CartProfile(BsonDocument doc) => new()
    {
        ["id"] = Id(doc).ToString(),
        ["clientId"] = GuidField(doc, "ClientId").ToString(),
        ["createdAt"] = JsonValue.Create(doc["CreatedAt"].ToUniversalTime()),
        ["updatedAt"] = JsonValue.Create(doc["UpdatedAt"].ToUniversalTime()),
        ["lines"] = new JsonArray(doc["Lines"].AsBsonArray.Select(line => (JsonNode)new JsonObject { ["productId"] = GuidField(line.AsBsonDocument, "ProductId").ToString(), ["quantity"] = line["Quantity"].AsInt32 }).ToArray())
    };

    private static JsonObject ClientProfile(BsonDocument doc)
    {
        var result = new JsonObject { ["id"] = Id(doc).ToString(), ["clientId"] = GuidField(doc, "ClientId").ToString(), ["createdAt"] = JsonValue.Create(doc["CreatedAt"].ToUniversalTime()) };
        foreach (var field in ClientFields) result.Add(Camel(field), doc[field].IsBsonNull ? null : JsonValue.Create(doc[field].AsString));
        return result;
    }

    private static JsonObject InvoiceProfile(BsonDocument doc) => new()
    {
        ["id"] = Id(doc).ToString(),
        ["orderId"] = GuidField(doc, "OrderId").ToString(),
        ["clietDataVersionId"] = GuidField(doc, "ClientDataVersionId").ToString(),
        ["storageUrl"] = doc["StorageUrl"].AsString,
        ["createdAt"] = JsonValue.Create(doc["CreatedAt"].ToUniversalTime())
    };

    private static JsonObject OrderProfile(BsonDocument order, Dictionary<string, BsonDocument[]> state)
    {
        var lines = new JsonArray();
        decimal total = 0;
        string? currency = null;
        foreach (var value in order["Lines"].AsBsonArray)
        {
            var line = value.AsBsonDocument;
            var product = state["productVersions"].Single(doc => Id(doc) == GuidField(line, "ProductVersionId"));
            var quantity = line["Quantity"].AsInt32;
            var amount = product["PriceAmount"].ToDecimal();
            total += amount * quantity;
            currency ??= product["PriceCurrency"].AsString;
            lines.Add(new JsonObject
            {
                ["productVersionId"] = Id(product).ToString(),
                ["quantity"] = quantity,
                ["lineTotalAmount"] = amount * quantity,
                ["productVersion"] = new JsonObject
                {
                    ["id"] = Id(product).ToString(),
                    ["productId"] = GuidField(product, "ProductId").ToString(),
                    ["isActive"] = product["IsActive"].AsBoolean,
                    ["createdAt"] = JsonValue.Create(product["CreatedAt"].ToUniversalTime()),
                    ["deactivatedAt"] = null,
                    ["name"] = product["Name"].AsString,
                    ["brand"] = product["Brand"].AsString,
                    ["priceAmount"] = amount,
                    ["priceCurrency"] = product["PriceCurrency"].AsString
                }
            });
        }
        return new JsonObject
        {
            ["id"] = Id(order).ToString(),
            ["clientId"] = GuidField(order, "ClientId").ToString(),
            ["createdAt"] = JsonValue.Create(order["CreatedAt"].ToUniversalTime()),
            ["updatedAt"] = order["UpdatedAt"].IsBsonNull ? null : JsonValue.Create(order["UpdatedAt"].ToUniversalTime()),
            ["status"] = order["Status"].AsInt32 switch { 0 => "Paid", 1 => "Cancelled", 2 => "Created", _ => throw new InvalidOperationException("Unexpected fixture status.") },
            ["totalAmount"] = total,
            ["totalCurrency"] = currency ?? "USD",
            ["lines"] = lines
        };
    }

    private static void EqualJson(JsonElement actual, JsonObject expected)
    {
        using var document = JsonDocument.Parse(expected.ToJsonString());
        JsonElement.DeepEquals(actual, document.RootElement).ShouldBeTrue($"Expected {document.RootElement}, actual {actual}");
    }
    private static void Names(JsonElement value, params string[] fields) => value.EnumerateObject().Select(property => property.Name).Order().ShouldBe(fields.Order());
    private void Attach(string name, Dictionary<string, BsonDocument[]> value) => context.RecordEvidence(name, value.ToDictionary(pair => pair.Key, pair => JsonSerializer.Deserialize<JsonElement>(new BsonArray(pair.Value).ToJson())));
    private static void EqualState(Dictionary<string, BsonDocument[]> actual, Dictionary<string, BsonDocument[]> expected)
    {
        actual.Keys.Order().ShouldBe(expected.Keys.Order());
        foreach (var name in expected.Keys) actual[name].ShouldBe(expected[name]);
    }
    private static Dictionary<string, BsonDocument[]> Sorted(Dictionary<string, BsonDocument[]> value) => value.ToDictionary(pair => pair.Key, pair => pair.Value.OrderBy(Id).ToArray());
    private static Dictionary<string, BsonDocument[]> Clone(Dictionary<string, BsonDocument[]> value) => value.ToDictionary(pair => pair.Key, pair => pair.Value.Select(doc => doc.DeepClone().AsBsonDocument).ToArray());
    private static object Rows(Table table) => table.Rows.Select(row => row.ToDictionary(pair => pair.Key, pair => pair.Value)).ToArray();
    private static Dictionary<string, string> Values(Table table) => table.Rows.ToDictionary(row => row["Field"], row => row["Value"], StringComparer.Ordinal);
    private static BsonBinaryData Binary(Guid value) => new(value, GuidRepresentation.Standard);
    private static Guid Id(BsonDocument value) => GuidField(value, "_id");
    private static Guid GuidField(BsonDocument value, string field) => value[field].AsBsonBinaryData.ToGuid();
    private static int Status(string value) => value switch { "Paid" => 0, "Cancelled" => 1, "Created" => 2, _ => throw new InvalidOperationException("Unknown fixture status.") };
    private static int Integer(string value) => int.Parse(value, CultureInfo.InvariantCulture);
    private static DateTime Date(string value) => DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    private static BsonValue NullableDate(string value) => value == "<null>" ? BsonNull.Value : new BsonDateTime(Date(value));
    private static DateTime Milliseconds(DateTime value) => new(value.Ticks - value.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
    private static string Camel(string value) => char.ToLowerInvariant(value[0]) + value[1..];
}
