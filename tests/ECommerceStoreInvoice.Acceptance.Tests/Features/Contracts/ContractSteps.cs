using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using MongoDB.Bson;
using Reqnroll;
using Shouldly;
using static ECommerceStoreInvoice.Acceptance.Tests.Features.Orders.Concurrency.OrderConcurrencySteps;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Contracts;

[Binding]
public sealed class ContractSteps(ScenarioApiContext context, BusinessValidationSteps fixture)
{
    private Table? _fields;
    private JsonDocument? _published;
    private string _canonical = string.Empty;
    private string _path = string.Empty;
    private string _effect = "no-write";
    private Dictionary<string, BsonDocument[]>? _before;
    private Dictionary<string, BsonDocument[]>? _expected;
    private DateTime _started;
    private DateTime _finished;

    [Given("the generated invoice contract includes these fields")]
    public void GivenFields(Table table)
    {
        _fields = table;
        context.RecordEvidence("Contract authored field expectations", table.Rows.Select(row => row.ToDictionary(pair => pair.Key, pair => pair.Value)).ToArray());
    }

    [When("I read the generated invoice contract over populated data")]
    public async Task WhenRead()
    {
        await Before();
        context.RegisterCleanup(() => { _published?.Dispose(); return Task.CompletedTask; });
        using var client = context.CreateTechnicalClient(context.Factory);
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _published = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync());
        JsonElement.DeepEquals(_published.RootElement, context.OpenApiDocument.RootElement).ShouldBeTrue();
    }

    [Then("all invoice schema fields match serializer metadata and typed examples")]
    public void ThenMetadata()
    {
        _fields.ShouldNotBeNull(); _published.ShouldNotBeNull();
        var schemas = _published.RootElement.GetProperty("components").GetProperty("schemas");
        foreach (var row in _fields.Rows)
        {
            var owner = schemas.GetProperty(row["Schema"]);
            var field = owner.GetProperty("properties").GetProperty(row["Field"]);
            var shape = field;
            if (shape.TryGetProperty("allOf", out var all)) shape = all[0];
            if (shape.TryGetProperty("$ref", out var reference)) shape = schemas.GetProperty(reference.GetString()!.Split('/').Last());
            shape.GetProperty("type").GetString().ShouldBe(row["Type"]);
            owner.GetProperty("required").EnumerateArray().Any(value => value.GetString() == row["Field"]).ShouldBe(bool.Parse(row["Required"]));
            (field.TryGetProperty("nullable", out var nullable) && nullable.GetBoolean()).ShouldBe(bool.Parse(row["Nullable"]));
            (shape.TryGetProperty("format", out var format) ? format.GetString() : "<none>").ShouldBe(row["Format"]);
        }
        context.RecordEvidence("Contract complete metadata verified", OpenApiContractAssertions.Check(_published));
    }

    [Given("the canonical required client profile JSON is")]
    [Given("the canonical serializer cart JSON is")]
    public void GivenJson(string body) => _canonical = body;

    [When("I omit required nullable client member {string}")]
    public async Task WhenMissing(string field)
    {
        await Before();
        var body = JsonNode.Parse(_canonical)!.AsObject();
        body.Remove(field).ShouldBeTrue();
        _path = fixture.ResolveFixturePath("/client-data-versions/{clientId}");
        context.RecordEvidence("Contract missing nullable member request", new { field, path = _path, body = body.DeepClone(), expectedStatus = 400, nullableDoesNotMeanOptional = true });
        context.Response = await context.HttpClient.PostAsync(_path, new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"));
    }

    [Then("the omitted client member returns this complete safe problem")]
    public async Task ThenMissing(Table table)
    {
        var values = table.Rows.ToDictionary(row => row["Field"], row => row["Value"]);
        context.Response.ShouldNotBeNull(); var response = context.Response;
        ((int)response.StatusCode).ShouldBe(int.Parse(values["Status"], CultureInfo.InvariantCulture));
        response.Content.Headers.ContentType?.MediaType.ShouldBe(values["ContentType"]);
        response.Headers.Location.ShouldBeNull(); response.Content.Headers.ContentDisposition.ShouldBeNull(); response.Content.Headers.Allow.ShouldBeEmpty();
        response.Headers.CacheControl!.ToString().Split(',').Select(value => value.Trim()).Order().ShouldBe(new[] { "no-cache", "no-store" });
        using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync());
        var problem = document.RootElement;
        problem.EnumerateObject().Select(value => value.Name).Order().ShouldBe(new[] { "detail", "instance", "status", "title", "traceId", "type" });
        problem.GetProperty("status").GetInt32().ShouldBe(400);
        foreach (var name in new[] { "Type", "Title", "Detail" }) problem.GetProperty(char.ToLowerInvariant(name[0]) + name[1..]).GetString().ShouldBe(values[name]);
        problem.GetProperty("instance").GetString().ShouldBe(_path); problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        context.RecordEvidence("Contract missing nullable member problem verified", new { expected = values, actual = problem.Clone(), completeSixFields = true });
    }

    [When("I replace the cart using serializer representation {string}")]
    public async Task WhenCart(string mode)
    {
        await Before(); _effect = "replace-cart-only";
        var body = JsonNode.Parse(_canonical)!;
        if (mode == "case-and-unknown")
        {
            body["unknownRoot"] = new JsonObject { ["ignored"] = true };
            body["lines"]![0]!["unknownNested"] = new JsonArray(1, 2);
            Uppercase(body);
        }
        else if (mode == "quoted-int32")
            foreach (var line in body["lines"]!.AsArray()) line!["quantity"] = line["quantity"]!.GetValue<int>().ToString(CultureInfo.InvariantCulture);
        else throw new InvalidOperationException(mode);
        _path = fixture.ResolveFixturePath("/shopping-carts/{clientId}");
        context.RecordEvidence("Contract exact serializer request", new { mode, path = _path, body = body.DeepClone(), compatibilityInput = true, canonicalQuantityType = "integer/int32" });
        _started = DateTime.UtcNow;
        context.Response = await context.HttpClient.PutAsync(_path, new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"));
        _finished = DateTime.UtcNow;
    }

    [Then("the accepted serializer cart has these exact ordered lines")]
    public async Task ThenCart(Table table)
    {
        _before.ShouldNotBeNull(); _expected.ShouldNotBeNull(); context.Response.ShouldNotBeNull();
        context.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var client = Guid.Parse(_path.Split('/').Last());
        var expected = _expected["shoppingCarts"].Single(cart => cart["ClientId"].AsBsonBinaryData.ToGuid() == client);
        expected["Lines"] = new BsonArray(table.Rows.Select(row => new BsonDocument { ["ProductId"] = new BsonBinaryData(Guid.Parse(row["ProductId"]), GuidRepresentation.Standard), ["Quantity"] = int.Parse(row["Quantity"], CultureInfo.InvariantCulture) }));
        var after = await context.Factory.GetCheckoutDocumentsAsync();
        var actual = after["shoppingCarts"].Single(cart => cart["ClientId"].AsBsonBinaryData.ToGuid() == client);
        actual["UpdatedAt"].AsBsonDateTime.MillisecondsSinceEpoch.ShouldBeInRange(new DateTimeOffset(_started).ToUnixTimeMilliseconds(), new DateTimeOffset(_finished).ToUnixTimeMilliseconds());
        expected["UpdatedAt"] = actual["UpdatedAt"].DeepClone();
        Equal(after, _expected);
        var profile = BusinessValidationSteps.CartProfile(expected);
        using var body = JsonDocument.Parse(await context.Response.Content.ReadAsByteArrayAsync());
        // The primary response keeps the service's UTC ticks; Mongo persists milliseconds.
        var responseUpdatedAt = body.RootElement.GetProperty("updatedAt").GetDateTime();
        responseUpdatedAt.Kind.ShouldBe(DateTimeKind.Utc);
        responseUpdatedAt.ShouldBeInRange(_started, _finished);
        new DateTimeOffset(responseUpdatedAt).ToUnixTimeMilliseconds().ShouldBe(actual["UpdatedAt"].AsBsonDateTime.MillisecondsSinceEpoch);
        profile["updatedAt"] = JsonValue.Create(responseUpdatedAt);
        JsonElement.DeepEquals(body.RootElement, JsonSerializer.SerializeToElement(profile)).ShouldBeTrue();
        fixture.SetTechnicalFixtureState(_expected);
        context.RecordEvidence("Contract complete serializer cart verified", new { expected = InvoiceGenerationProbe.JsonSnapshot(_expected), actual = InvoiceGenerationProbe.JsonSnapshot(after), expectedResponse = profile, actualResponse = body.RootElement.Clone(), fromUtc = _started, toUtc = _finished, responseUpdatedAtUtc = responseUpdatedAt, storedUpdatedAtMilliseconds = actual["UpdatedAt"].AsBsonDateTime.MillisecondsSinceEpoch, onlyTargetCartLinesAndUpdatedAtChanged = true });
    }

    [Then("the contract operation preserves complete expected state and supporting reads")]
    public async Task ThenState()
    {
        _before.ShouldNotBeNull(); _expected.ShouldNotBeNull();
        Equal(await context.Factory.GetCheckoutDocumentsAsync(), _expected);
        await fixture.VerifyFixtureReadsAsync();
        var actual = await context.Factory.GetCheckoutDocumentsAsync(); Equal(actual, _expected);
        context.Factory.CatalogRequestCount.ShouldBe(0);
        context.RecordEvidence("Contract complete state verified", new { effect = _effect, before = InvoiceGenerationProbe.JsonSnapshot(_before), expected = InvoiceGenerationProbe.JsonSnapshot(_expected), actual = InvoiceGenerationProbe.JsonSnapshot(actual), catalogRequests = 0, completeSupportingReads = 9 });
    }

    private async Task Before()
    {
        _before = fixture.FixtureState; _expected = fixture.FixtureState;
        Equal(await context.Factory.GetCheckoutDocumentsAsync(), _before);
        context.RecordEvidence("Contract complete state before primary request", InvoiceGenerationProbe.JsonSnapshot(_before));
    }
    private static void Uppercase(JsonNode node)
    {
        if (node is JsonArray array) foreach (var value in array.OfType<JsonNode>()) Uppercase(value);
        if (node is not JsonObject obj) return;
        foreach (var pair in obj.ToArray()) { obj.Remove(pair.Key); if (pair.Value is not null) Uppercase(pair.Value); obj[pair.Key.ToUpperInvariant()] = pair.Value; }
    }
}
