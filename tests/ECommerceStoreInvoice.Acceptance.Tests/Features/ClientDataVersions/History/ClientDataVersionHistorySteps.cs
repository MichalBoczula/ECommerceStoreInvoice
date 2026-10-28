using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using MongoDB.Bson;
using Reqnroll;
using Shouldly;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.ClientDataVersions.History;

[Binding]
public sealed class ClientDataVersionHistorySteps(ScenarioApiContext context)
{
    private readonly Dictionary<string, Guid> _clients = new(StringComparer.Ordinal) { ["A"] = Guid.NewGuid(), ["B"] = Guid.NewGuid(), ["missing"] = Guid.NewGuid(), ["empty"] = Guid.Empty };
    private readonly Dictionary<string, ClientDataVersionResponseDto> _versions = new(StringComparer.Ordinal);
    private Dictionary<string, BsonDocument[]> _known = new(StringComparer.Ordinal);
    private Dictionary<string, BsonDocument[]>? _before;
    private CreateClientDataVersionRequestDto? _request;
    private ClientDataVersionResponseDto? _created;
    private string _method = string.Empty;
    private string _path = string.Empty;
    private string _client = string.Empty;
    private string _version = string.Empty;
    private DateTime _started;
    private DateTime _completed;

    [Given("these complete historical client data versions were inserted in this order")]
    public void GivenVersions(Table table)
    {
        context.RecordEvidence("Client version fixture table", Rows(table));
        foreach (var row in table.Rows)
        {
            var request = Request(row.ToDictionary(pair => pair.Key, pair => pair.Value));
            _versions.Add(row["Version"], Profile(request, _clients[row["Client"]], Guid.NewGuid(), Date(row["CreatedAt"])));
        }
        // Preserve table insertion order: latest must be selected by CreatedAt, not insertion or ID.
        _known.Add("clientDataVersions", _versions.Values.Select(Document).ToArray());
        context.RecordEvidence("Resolved historical client versions", _versions);
        context.RecordEvidence("Client version fixture identities", _clients);
    }

    [Given("these paid orders and completed invoices reference historical client versions")]
    public async Task GivenReferences(Table table)
    {
        context.RecordEvidence("Client version ancillary fixture table", Rows(table));
        var orders = new List<BsonDocument>();
        var invoices = new List<BsonDocument>();
        var snapshots = new List<BsonDocument>();
        var carts = new List<BsonDocument>();
        foreach (var row in table.Rows)
        {
            var clientId = _clients[row["Client"]];
            var version = _versions[row["Version"]];
            version.ClientId.ShouldBe(clientId);
            var snapshotId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var productId = Guid.Parse(row["ProductId"]);
            var created = Date(row["CreatedAt"]);
            var paid = Date(row["PaidAt"]);
            version.CreatedAt.ShouldBeLessThanOrEqualTo(created);
            row["Status"].ShouldBe("Paid");
            row["GenerationStatus"].ShouldBe("Completed");
            row["GenerationAttemptId"].ShouldBe("<null>");
            row["GenerationLeaseUntil"].ShouldBe("<null>");
            snapshots.Add(new BsonDocument
            {
                ["_id"] = Binary(snapshotId),
                ["ProductId"] = Binary(productId),
                ["IsActive"] = bool.Parse(row["IsActive"]),
                ["CreatedAt"] = created,
                ["DeactivatedAt"] = BsonNull.Value,
                ["Name"] = row["ProductName"],
                ["Brand"] = row["Brand"],
                ["PriceAmount"] = decimal.Parse(row["PriceAmount"], CultureInfo.InvariantCulture),
                ["PriceCurrency"] = row["PriceCurrency"]
            });
            orders.Add(new BsonDocument
            {
                ["_id"] = Binary(orderId),
                ["ClientId"] = Binary(clientId),
                ["Lines"] = new BsonArray { new BsonDocument { ["ProductVersionId"] = Binary(snapshotId), ["Quantity"] = Integer(row["Quantity"]) } },
                ["CreatedAt"] = created,
                ["UpdatedAt"] = paid,
                ["Status"] = 0
            });
            invoices.Add(new BsonDocument
            {
                ["_id"] = Binary(Guid.NewGuid()),
                ["OrderId"] = Binary(orderId),
                ["ClientDataVersionId"] = Binary(version.Id),
                ["StorageUrl"] = row["StorageUrl"],
                ["CreatedAt"] = Date(row["InvoiceCreatedAt"]),
                ["GenerationStatus"] = row["GenerationStatus"],
                ["GenerationAttemptId"] = BsonNull.Value,
                ["GenerationLeaseUntil"] = BsonNull.Value
            });
            carts.Add(new BsonDocument
            {
                ["_id"] = Binary(Guid.NewGuid()),
                ["ClientId"] = Binary(clientId),
                ["CreatedAt"] = created,
                ["UpdatedAt"] = paid,
                ["Lines"] = new BsonArray { new BsonDocument { ["ProductId"] = Binary(productId), ["Quantity"] = Integer(row["CartQuantity"]) } }
            });
        }
        orders.Count.ShouldBe(2);
        _known.Add("orders", orders.ToArray());
        _known.Add("invoices", invoices.ToArray());
        _known.Add("productVersions", snapshots.ToArray());
        _known.Add("shoppingCarts", carts.ToArray());
        Attach("Client version expected fixture database", Sorted(_known));
        await context.Factory.SeedOrderReadDocumentsAsync(_known);
        _known = Sorted(_known);
        var actual = await context.Factory.GetCheckoutDocumentsAsync();
        Attach("Client version actual fixture database", actual);
        EqualState(actual, _known);
        actual.Values.ShouldAllBe(documents => documents.Length > 0);
        context.Factory.CatalogRequestCount.ShouldBe(0);
    }

    [When("I send this complete client version creation request")]
    public async Task WhenCreate(Table table)
    {
        var values = Values(table);
        context.RecordEvidence("Client version creation request table", values);
        values["Method"].ShouldBe("POST");
        values["Endpoint"].ShouldBe("/client-data-versions/{clientId}");
        _request = Request(values);
        await BeforeRequest("POST", values["Client"], values["Version"]);
        // MongoDB stores milliseconds. Wait for a measured, strictly later millisecond,
        // rather than asserting an undocumented tie-break for equal CreatedAt values.
        var prior = _versions.Values.Where(item => item.ClientId == _clients[_client]).Max(item => item.CreatedAt);
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (Milliseconds(DateTime.UtcNow) <= Milliseconds(prior))
        {
            if (DateTime.UtcNow >= deadline) throw new InvalidOperationException("Clock did not advance beyond the previous stored version timestamp.");
            await Task.Delay(1);
        }
        _started = DateTime.UtcNow;
        context.Response = await context.HttpClient.PostAsJsonAsync(_path, _request, context.JsonOptions);
        _completed = DateTime.UtcNow;
        Window();
    }

    [When("I send this client version lookup request")]
    public async Task WhenGet(Table table)
    {
        var values = Values(table);
        context.RecordEvidence("Client version lookup request table", values);
        values["Method"].ShouldBe("GET");
        values["Endpoint"].ShouldBe("/client-data-versions/client/{clientId}");
        await BeforeRequest("GET", values["Client"], "<none>");
        _started = DateTime.UtcNow;
        context.Response = await context.HttpClient.GetAsync(_path);
        _completed = DateTime.UtcNow;
        Window();
    }

    private async Task BeforeRequest(string method, string client, string version)
    {
        _method = method;
        _client = client;
        _version = version;
        _created = null;
        _path = method == "POST" ? $"/client-data-versions/{_clients[client]}" : $"/client-data-versions/client/{_clients[client]}";
        _before = await context.Factory.GetCheckoutDocumentsAsync();
        Attach("Client version database before primary request", _before);
        EqualState(_before, _known);
        context.Response?.Dispose();
        context.RecordEvidence("Resolved client version request", new { method, path = _path, client, clientId = _clients[client], version, request = method == "POST" ? _request : null });
    }

    private void Window() => context.RecordEvidence("Client version primary request window", new { method = _method, path = _path, startedAt = _started, completedAt = _completed });

    [Then("the client version response is this complete profile")]
    public async Task ThenProfile(Table table)
    {
        var values = Values(table);
        context.RecordEvidence("Client version response expectations", values);
        values["StatusCode"].ShouldBe("200");
        values["ContentType"].ShouldBe("application/json");
        values["Client"].ShouldBe(_client);
        context.Response.ShouldNotBeNull();
        var actual = await ReadProfile(context.Response);
        context.RecordEvidence("Complete actual client version response", actual);
        var expectedRequest = Request(values);
        ClientDataVersionResponseDto expected;
        if (_method == "POST")
        {
            values["Version"].ShouldBe(_version);
            values["CreatedAt"].ShouldBe("generated");
            values["Id"].ShouldBe("generated-distinct");
            actual.Id.ShouldNotBe(Guid.Empty);
            _versions.Values.ShouldNotContain(item => item.Id == actual.Id);
            actual.CreatedAt.Kind.ShouldBe(DateTimeKind.Utc);
            actual.CreatedAt.ShouldBeGreaterThanOrEqualTo(_started);
            actual.CreatedAt.ShouldBeLessThanOrEqualTo(_completed);
            actual.CreatedAt.ShouldBeGreaterThan(_versions.Values.Where(item => item.ClientId == _clients[_client]).Max(item => item.CreatedAt));
            JsonElement.DeepEquals(JsonSerializer.SerializeToElement(expectedRequest), JsonSerializer.SerializeToElement(_request)).ShouldBeTrue();
            expected = Profile(expectedRequest, _clients[_client], actual.Id, actual.CreatedAt);
            _created = expected with { CreatedAt = Milliseconds(expected.CreatedAt) };
        }
        else
        {
            values["Id"].ShouldBe("saved-version");
            expected = _versions[values["Version"]];
            expected.CreatedAt.ShouldBe(Date(values["CreatedAt"]));
            Profile(expectedRequest, expected.ClientId, expected.Id, expected.CreatedAt).ShouldBe(expected);
            var latest = _versions.Values.Where(item => item.ClientId == _clients[_client]).MaxBy(item => item.CreatedAt);
            latest.ShouldBe(expected);
        }
        EqualProfile(actual, expected);
    }

    [Then("the client version request returns this complete problem")]
    public async Task ThenProblem(Table table)
    {
        var values = Values(table);
        context.RecordEvidence("Client version problem expectations", values);
        context.Response.ShouldNotBeNull();
        ((int)context.Response.StatusCode).ShouldBe(Integer(values["StatusCode"]));
        context.Response.Content.Headers.ContentType?.MediaType.ShouldBe(values["ContentType"]);
        using var document = JsonDocument.Parse(await context.Response.Content.ReadAsStringAsync());
        var problem = document.RootElement;
        context.RecordEvidence("Complete actual client version problem", problem.Clone());
        problem.GetProperty("status").GetInt32().ShouldBe(Integer(values["StatusCode"]));
        foreach (var key in new[] { "Title", "Type", "Detail" })
            problem.GetProperty(char.ToLowerInvariant(key[0]) + key[1..]).GetString().ShouldBe(values[key].Replace("{clientId}", _clients[_client].ToString()));
        values["Instance"].ShouldBe("request-path");
        problem.GetProperty("instance").GetString().ShouldBe(_path);
        values["TraceId"].ShouldBe("generated");
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        if (values["ErrorsCount"] == "absent")
            Names(problem, "type", "title", "status", "detail", "instance", "traceId");
        else
        {
            Names(problem, "type", "title", "status", "detail", "instance", "traceId", "errors");
            var errors = problem.GetProperty("errors");
            errors.GetArrayLength().ShouldBe(Integer(values["ErrorsCount"]));
            errors.GetArrayLength().ShouldBe(1);
            Names(errors[0], "name", "entity", "message");
            errors[0].GetProperty("name").GetString().ShouldBe(values["ErrorName"]);
            errors[0].GetProperty("entity").GetString().ShouldBe(values["ErrorEntity"]);
            errors[0].GetProperty("message").GetString().ShouldBe(values["ErrorMessage"]);
        }
    }

    [Then("the client version operation has exactly this stored effect")]
    public async Task ThenState(Table table)
    {
        var values = Values(table);
        context.RecordEvidence("Client version persistence expectations", values);
        _before.ShouldNotBeNull();
        var after = await context.Factory.GetCheckoutDocumentsAsync();
        Attach("Client version database after primary request", after);
        var wanted = _before.ToDictionary(pair => pair.Key, pair => pair.Value.Select(document => document.DeepClone().AsBsonDocument).ToArray());
        if (values["Effect"] == "append-version")
        {
            _method.ShouldBe("POST");
            _created.ShouldNotBeNull();
            values["AddedVersion"].ShouldBe(_version);
            wanted["clientDataVersions"] = wanted["clientDataVersions"].Append(Document(_created)).OrderBy(Id).ToArray();
            _versions.Add(_version, _created);
        }
        else
        {
            values["Effect"].ShouldBe("unchanged");
            values["AddedVersion"].ShouldBe("none");
        }
        EqualState(after, wanted);
        after["clientDataVersions"].Length.ShouldBe(Integer(values["ClientVersions"]));
        foreach (var name in new[] { "orders", "productVersions", "shoppingCarts", "invoices" })
            after[name].Length.ShouldBe(Integer(values[name]));
        values["CatalogRequests"].ShouldBe("0");
        context.Factory.CatalogRequestCount.ShouldBe(0);
        _known = after;
        context.RecordEvidence("Client version primary persistence verified", new { method = _method, path = _path, effect = values["Effect"], addedVersion = values["AddedVersion"], historicalVersionsAndInvoiceReferencesUnchanged = true, catalogRequests = 0 });
    }

    [Then("these complete latest lookups and historical invoice reads preserve the database")]
    public async Task ThenSupporting(Table table)
    {
        context.RecordEvidence("Client version supporting lookup expectations", Rows(table));
        var before = await context.Factory.GetCheckoutDocumentsAsync();
        Attach("Client version database before supporting reads", before);
        EqualState(before, _known);
        table.Rows.Count.ShouldBe(2);
        table.Rows.Select(row => row["Client"]).Order().ShouldBe(new[] { "A", "B" });
        foreach (var row in table.Rows)
        {
            var expected = _versions[row["Version"]];
            expected.ClientId.ShouldBe(_clients[row["Client"]]);
            _versions.Values.Where(item => item.ClientId == expected.ClientId).MaxBy(item => item.CreatedAt).ShouldBe(expected);
            using var response = await context.HttpClient.GetAsync($"/client-data-versions/client/{expected.ClientId}");
            EqualProfile(await ReadProfile(response), expected);
        }
        foreach (var stored in _known["invoices"])
        {
            var invoiceId = Id(stored);
            var orderId = stored["OrderId"].AsBsonBinaryData.ToGuid();
            var referenced = _versions.Values.Single(item => item.Id == stored["ClientDataVersionId"].AsBsonBinaryData.ToGuid());
            var order = _known["orders"].Single(item => Id(item) == orderId);
            referenced.ClientId.ShouldBe(order["ClientId"].AsBsonBinaryData.ToGuid());
            foreach (var path in new[] { $"/invoices/{invoiceId}", $"/invoices/by-order/{orderId}" })
            {
                using var response = await context.HttpClient.GetAsync(path);
                response.StatusCode.ShouldBe(HttpStatusCode.OK);
                response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                Names(json.RootElement, "id", "orderId", "clietDataVersionId", "storageUrl", "createdAt");
                var invoice = JsonSerializer.Deserialize<InvoiceResponseDto>(json.RootElement, context.JsonOptions);
                invoice.ShouldNotBeNull();
                invoice.Id.ShouldBe(invoiceId);
                invoice.OrderId.ShouldBe(orderId);
                invoice.ClietDataVersionId.ShouldBe(referenced.Id);
                invoice.StorageUrl.ShouldBe(stored["StorageUrl"].AsString);
                invoice.CreatedAt.ShouldBe(stored["CreatedAt"].ToUniversalTime());
                context.RecordEvidence("Verified unchanged historical invoice metadata", new { path, invoice, referencedVersion = referenced });
            }
        }
        var after = await context.Factory.GetCheckoutDocumentsAsync();
        Attach("Client version database after supporting reads", after);
        EqualState(after, _known);
        context.Factory.CatalogRequestCount.ShouldBe(0);
        context.RecordEvidence("Client version supporting reads verified", new { completeLatestLookups = 2, completeHistoricalInvoiceReads = 4, allFiveCollectionsUnchanged = true, catalogRequests = 0 });
    }

    private async Task<ClientDataVersionResponseDto> ReadProfile(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Names(json.RootElement, "id", "clientId", "clientName", "postalCode", "city", "street", "buildingNumber", "apartmentNumber", "phoneNumber", "phonePrefix", "addressEmail", "createdAt");
        foreach (var property in json.RootElement.EnumerateObject()) property.Value.ValueKind.ShouldBe(JsonValueKind.String);
        var actual = JsonSerializer.Deserialize<ClientDataVersionResponseDto>(json.RootElement, context.JsonOptions);
        actual.ShouldNotBeNull();
        actual.CreatedAt.Kind.ShouldBe(DateTimeKind.Utc);
        return actual;
    }

    private static void EqualProfile(ClientDataVersionResponseDto actual, ClientDataVersionResponseDto expected) =>
        (actual with { CreatedAt = Milliseconds(actual.CreatedAt) }).ShouldBe(expected with { CreatedAt = Milliseconds(expected.CreatedAt) });
    private static void Names(JsonElement value, params string[] names)
    {
        value.ValueKind.ShouldBe(JsonValueKind.Object);
        value.EnumerateObject().Select(property => property.Name).Order().ShouldBe(names.Order());
    }
    private static Dictionary<string, BsonDocument[]> Sorted(Dictionary<string, BsonDocument[]> values) => values.ToDictionary(pair => pair.Key, pair => pair.Value.OrderBy(Id).ToArray());
    private static void EqualState(Dictionary<string, BsonDocument[]> actual, Dictionary<string, BsonDocument[]> expected)
    {
        actual.Keys.Order().ShouldBe(expected.Keys.Order());
        foreach (var name in expected.Keys) actual[name].ShouldBe(expected[name]);
    }
    private void Attach(string name, Dictionary<string, BsonDocument[]> value) => context.RecordEvidence(name, value.ToDictionary(pair => pair.Key, pair => JsonSerializer.Deserialize<JsonElement>(new BsonArray(pair.Value).ToJson())));
    private static object Rows(Table table) => table.Rows.Select(row => row.ToDictionary(pair => pair.Key, pair => pair.Value)).ToArray();
    private static Dictionary<string, string> Values(Table table) => table.Rows.ToDictionary(row => row["Field"], row => row["Value"], StringComparer.Ordinal);
    private static int Integer(string value) => int.Parse(value, CultureInfo.InvariantCulture);
    private static DateTime Date(string value) => DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    private static DateTime Milliseconds(DateTime value) => new(value.Ticks - value.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
    private static BsonBinaryData Binary(Guid value) => new(value, GuidRepresentation.Standard);
    private static Guid Id(BsonDocument value) => value["_id"].AsBsonBinaryData.ToGuid();
    private static CreateClientDataVersionRequestDto Request(IReadOnlyDictionary<string, string> values) => new()
    {
        ClientName = values["ClientName"],
        PostalCode = values["PostalCode"],
        City = values["City"],
        Street = values["Street"],
        BuildingNumber = values["BuildingNumber"],
        ApartmentNumber = values["ApartmentNumber"],
        PhoneNumber = values["PhoneNumber"],
        PhonePrefix = values["PhonePrefix"],
        AddressEmail = values["AddressEmail"]
    };
    private static ClientDataVersionResponseDto Profile(CreateClientDataVersionRequestDto request, Guid clientId, Guid id, DateTime createdAt) => new()
    {
        Id = id,
        ClientId = clientId,
        ClientName = request.ClientName,
        PostalCode = request.PostalCode,
        City = request.City,
        Street = request.Street,
        BuildingNumber = request.BuildingNumber,
        ApartmentNumber = request.ApartmentNumber,
        PhoneNumber = request.PhoneNumber,
        PhonePrefix = request.PhonePrefix,
        AddressEmail = request.AddressEmail,
        CreatedAt = createdAt
    };
    private static BsonDocument Document(ClientDataVersionResponseDto value) => new()
    {
        ["_id"] = Binary(value.Id),
        ["ClientId"] = Binary(value.ClientId),
        ["ClientName"] = value.ClientName,
        ["PostalCode"] = value.PostalCode,
        ["City"] = value.City,
        ["Street"] = value.Street,
        ["BuildingNumber"] = value.BuildingNumber,
        ["ApartmentNumber"] = value.ApartmentNumber,
        ["PhoneNumber"] = value.PhoneNumber,
        ["PhonePrefix"] = value.PhonePrefix,
        ["AddressEmail"] = value.AddressEmail,
        ["CreatedAt"] = value.CreatedAt
    };
}
