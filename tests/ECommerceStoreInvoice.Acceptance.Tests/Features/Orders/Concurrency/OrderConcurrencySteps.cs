using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using MongoDB.Bson;
using Reqnroll;
using Shouldly;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Orders.Concurrency;

[Binding]
public sealed class OrderConcurrencySteps(ScenarioApiContext context, BusinessValidationSteps fixture)
{
    private readonly Dictionary<Guid, Product> _products = new();
    private Dictionary<string, BsonDocument[]> _before = new();
    private Dictionary<string, BsonDocument[]> _winnerState = new();
    private HttpResponseMessage? _winnerResponse;
    private HttpResponseMessage? _loserResponse;
    private string _winner = string.Empty;
    private string _winningStatus = string.Empty;
    private Guid _orderId;
    private Guid _clientId;
    private DateTime _started;
    private DateTime _finished;
    private int _catalogBefore;
    private OrderConcurrencyProbe Probe => context.Factory.OrderProbe ?? throw new InvalidOperationException("Concurrency probe is missing.");

    [Given("checkout concurrency uses these exact catalog products")]
    public void GivenProducts(Table table)
    {
        context.RecordEvidence("Concurrency catalog product expectations", table.Rows.Select(row => row.ToDictionary(p => p.Key, p => p.Value)).ToArray());
        foreach (var row in table.Rows) _products.Add(Guid.Parse(row["ProductId"]), new(row["Name"], row["Brand"], decimal.Parse(row["PriceAmount"], CultureInfo.InvariantCulture), row["PriceCurrency"], int.Parse(row["Quantity"], CultureInfo.InvariantCulture)));
    }

    [When("both validated status requests compete with {string} winning")]
    public Task WhenStatus(string winner) => RunStatusAsync(Guid.Parse(fixture.ResolveFixturePath("{orderId}")), winner);

    internal async Task RunStatusAsync(Guid orderId, string winningStatus)
    {
        _orderId = orderId;
        _winningStatus = winningStatus;
        _winner = winningStatus == "Paid" ? "first" : "second";
        _before = await context.Factory.GetCheckoutDocumentsAsync();
        _catalogBefore = context.Factory.CatalogRequestCount;
        Attach("Concurrency database before", _before);
        Probe.Configure("status", orderId);
        _started = DateTime.UtcNow;
        var first = SendAsync(HttpMethod.Patch, $"/orders/{orderId}/status", "first", "Paid");
        var second = SendAsync(HttpMethod.Patch, $"/orders/{orderId}/status", "second", "Cancelled");
        try
        {
            await Task.WhenAll(Probe.WaitAsync("first"), Probe.WaitAsync("second"));
            var held = await context.Factory.GetCheckoutDocumentsAsync();
            Attach("Status both validated before either write", held);
            Equal(held, _before);
            Probe.Events.Count(e => e.Stage == "ReadCreated:Created").ShouldBe(2);
            Probe.Events.Count(e => e.Stage == "StatusValidated").ShouldBe(2);
            Probe.Resume(_winner);
            _winnerResponse = await (_winner == "first" ? first : second).WaitAsync(TimeSpan.FromSeconds(45));
            _winnerResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
            _winnerState = await context.Factory.GetCheckoutDocumentsAsync();
            Attach("Status winning database before stale write", _winnerState);
            Probe.Resume(_winner == "first" ? "second" : "first");
            _loserResponse = await (_winner == "first" ? second : first).WaitAsync(TimeSpan.FromSeconds(45));
        }
        finally { Probe.ResumeAll(); await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(45)); }
        _finished = DateTime.UtcNow;
        context.RecordEvidence("Concurrency request window", new { startedAt = _started, completedAt = _finished, winner = _winner, winningStatus });
    }

    [Then("only the winning complete status profile is persisted and the stale request returns 409")]
    public async Task ThenStatus()
    {
        _winnerResponse.ShouldNotBeNull();
        _loserResponse.ShouldNotBeNull();
        await Problem(_loserResponse, 409, $"/orders/{_orderId}/status");
        var expected = Clone(_before);
        var order = expected["orders"].Single(d => Id(d) == _orderId);
        using var body = JsonDocument.Parse(await _winnerResponse.Content.ReadAsStringAsync());
        var updated = body.RootElement.GetProperty("updatedAt").GetDateTime();
        Window(updated);
        order["Status"] = _winningStatus == "Paid" ? 0 : 1;
        order["UpdatedAt"] = Milliseconds(updated);
        var profile = BusinessValidationSteps.OrderProfile(order, expected);
        profile["updatedAt"] = JsonValue.Create(updated);
        JsonEqual(body.RootElement, profile);
        context.RecordEvidence("Status complete expected winner response", profile);
        Equal(_winnerState, expected);
        var actual = await context.Factory.GetCheckoutDocumentsAsync();
        Attach("Status database after rejected stale write", actual);
        Equal(actual, expected);
        Probe.Events.Single(e => e.Stage == "StatusRejected").ExceptionType.ShouldBe("ECommerceStoreInvoice.Domain.Validation.Common.OrderWriteConflictException");
        Probe.Events.Single(e => e.Stage == "StatusCommitted").Attempt.ShouldBe(_winner);
        Probe.Events.Where(e => e.Stage == "StatusValidated").Select(e => e.ScopeId).Distinct().Count().ShouldBe(2);
        Probe.Events.ShouldAllBe(e => !e.SessionActive);
        await VerifyReads(expected, _catalogBefore);
        context.RecordEvidence("Verified deterministic status winning state", new { orderId = _orderId, winner = _winner, winningStatus = _winningStatus, loserStatus = 409, bothLoadedCreated = true, allFiveCollectionsVerified = true, catalogRequests = _catalogBefore });
        _winnerResponse.Dispose(); _loserResponse.Dispose();
    }

    [When("checkout fails after the real {string} write and is retried")]
    public async Task WhenRollback(string stage)
    {
        await BeforeCheckout("rollback", stage);
        var failed = SendAsync(HttpMethod.Post, $"/orders/client/{_clientId}", "first");
        try
        {
            await Probe.WaitAsync("first");
            var held = await context.Factory.GetCheckoutDocumentsAsync();
            Attach("Rollback outside database while real writes are staged", held);
            Equal(held, _before);
            var stages = new[] { "Snapshots", "Order", "Cart" };
            foreach (var item in Probe.Events.Where(e => stages.Contains(e.Stage))) VerifyStaged(item);
            Probe.Events.Last().Stage.ShouldBe(stage);
        }
        finally { Probe.ResumeAll(); _loserResponse = await failed.WaitAsync(TimeSpan.FromSeconds(45)); }
        await Problem(_loserResponse, 500, $"/orders/client/{_clientId}");
        var after = await context.Factory.GetCheckoutDocumentsAsync();
        Attach("Rollback database after failed HTTP request", after);
        Equal(after, _before);
        Probe.Events.Count(e => e.Stage == "Begin").ShouldBe(1);
        Probe.Events.Count(e => e.Stage == "Commit").ShouldBe(0);
        var rollback = Probe.Events.Single(e => e.Stage == "Rollback");
        rollback.SessionId.ShouldBeNull(); rollback.SessionActive.ShouldBeFalse(); rollback.Inside.ShouldBeNull();
        context.Factory.CatalogRequestCount.ShouldBe(_catalogBefore + 1);
        Probe.Configure(string.Empty, Guid.Empty);
        _winnerResponse = await context.HttpClient.PostAsync($"/orders/client/{_clientId}", null);
        _finished = DateTime.UtcNow;
        _winnerState = await context.Factory.GetCheckoutDocumentsAsync();
        context.RecordEvidence("Verified real checkout rollback and retry", new { stage, actualSessionWrites = true, rollback = true, beforeRetryAllCollectionsEqual = true, sessionCleared = true, retryStatus = (int)_winnerResponse.StatusCode, catalogRequests = context.Factory.CatalogRequestCount });
    }

    [When("two real checkout transactions compete with {string} winning")]
    public async Task WhenCheckoutRace(string winner)
    {
        await BeforeCheckout("checkout");
        _winner = winner;
        var first = SendAsync(HttpMethod.Post, $"/orders/client/{_clientId}", "first");
        var second = SendAsync(HttpMethod.Post, $"/orders/client/{_clientId}", "second");
        try
        {
            await Task.WhenAll(Probe.WaitAsync("first"), Probe.WaitAsync("second"));
            var held = await context.Factory.GetCheckoutDocumentsAsync();
            Attach("Checkout both transactions staged but externally invisible", held);
            Equal(held, _before);
            foreach (var item in Probe.Events.Where(e => e.Stage is "Snapshots" or "Order" or "BeforeCart")) VerifyStaged(item);
            Probe.Events.Where(e => e.Stage == "BeforeCart").Select(e => e.SessionId).Distinct().Count().ShouldBe(2);
            Probe.Resume(winner);
            _winnerResponse = await (winner == "first" ? first : second).WaitAsync(TimeSpan.FromSeconds(45));
            _winnerResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
            _winnerState = await context.Factory.GetCheckoutDocumentsAsync();
            Attach("Checkout committed winner before losing cart write", _winnerState);
            Probe.Resume(winner == "first" ? "second" : "first");
            _loserResponse = await (winner == "first" ? second : first).WaitAsync(TimeSpan.FromSeconds(45));
        }
        finally { Probe.ResumeAll(); await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(45)); }
        _finished = DateTime.UtcNow;
        await Problem(_loserResponse!, 500, $"/orders/client/{_clientId}");
        Probe.Events.Count(e => e.Stage == "Begin").ShouldBe(2);
        Probe.Events.Single(e => e.Stage == "Commit").Attempt.ShouldBe(winner);
        var rollback = Probe.Events.Single(e => e.Stage == "Rollback");
        rollback.Attempt.ShouldNotBe(winner); rollback.SessionActive.ShouldBeFalse(); rollback.SessionId.ShouldBeNull();
        Probe.Events.Single(e => e.Stage == "CartWriteConflict").ExceptionType.ShouldStartWith("MongoDB.Driver.Mongo");
        var actual = await context.Factory.GetCheckoutDocumentsAsync();
        Attach("Checkout winning database after losing transaction rollback", actual);
        Equal(actual, _winnerState);
        context.RecordEvidence("Verified competing checkout transaction outcome", new { winner, loserStatus = 500, realMongoWriteConflict = true, oneCommit = true, oneRollback = true, winningStateUnchanged = true, distinctSessions = true });
    }

    [Then("checkout preserves the exact winning order snapshots and cleared cart")]
    public async Task ThenCheckout()
    {
        _winnerResponse.ShouldNotBeNull(); _winnerResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var newOrder = _winnerState["orders"].Single(d => !_before["orders"].Any(old => Id(old) == Id(d)));
        var synthetic = new OrderWriteObservation("winner", Guid.Empty, "Cart", null, true, null, _winnerState, _before);
        VerifyStaged(synthetic);
        using var document = JsonDocument.Parse(await _winnerResponse.Content.ReadAsStringAsync());
        var expected = BusinessValidationSteps.OrderProfile(newOrder, _winnerState);
        NormalizePrimaryDates(document.RootElement, expected);
        JsonEqual(document.RootElement, expected);
        context.RecordEvidence("Checkout complete expected winning response", expected);
        var expectedCatalog = _catalogBefore + 2;
        context.Factory.CatalogRequestCount.ShouldBe(expectedCatalog);
        await VerifyReads(_winnerState, expectedCatalog);
        // A further checkout sees the committed empty cart; it does not create another order.
        Probe.Configure(string.Empty, Guid.Empty);
        using var empty = await context.HttpClient.PostAsync($"/orders/client/{_clientId}", null);
        empty.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        empty.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        using var problem = JsonDocument.Parse(await empty.Content.ReadAsStringAsync());
        var root = problem.RootElement;
        root.EnumerateObject().Select(p => p.Name).Order().ShouldBe(new[] { "detail", "errors", "instance", "status", "title", "traceId", "type" });
        root.GetProperty("status").GetInt32().ShouldBe(400);
        root.GetProperty("title").GetString().ShouldBe("Validation failed.");
        root.GetProperty("detail").GetString().ShouldBe("One or more validation errors occurred.");
        root.GetProperty("type").GetString().ShouldBe("https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1");
        root.GetProperty("instance").GetString().ShouldBe($"/orders/client/{_clientId}");
        root.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        JsonEqual(root.GetProperty("errors"), JsonNode.Parse("[{\"name\":\"OrderLinesValidationRule\",\"message\":\"Order lines cannot be empty.\",\"entity\":\"Order\"}]")!);
        Equal(await context.Factory.GetCheckoutDocumentsAsync(), _winnerState);
        context.Factory.CatalogRequestCount.ShouldBe(expectedCatalog);
        context.RecordEvidence("Checkout final complete state verified", new { orderId = Id(newOrder), newSnapshots = 2, exactQuantitiesPricesAndTotals = true, allFiveCollectionsVerified = true, emptyRetryStatus = 400, catalogRequests = context.Factory.CatalogRequestCount });
        _winnerResponse.Dispose(); _loserResponse?.Dispose();
    }

    private async Task BeforeCheckout(string mode, string stage = "")
    {
        _clientId = Guid.Parse(fixture.ResolveFixturePath("{clientId}"));
        _before = await context.Factory.GetCheckoutDocumentsAsync();
        Attach("Concurrency database before", _before);
        _catalogBefore = context.Factory.CatalogRequestCount;
        _catalogBefore.ShouldBe(0);
        _started = DateTime.UtcNow;
        Probe.Configure(mode, _clientId, stage);
    }

    private void VerifyStaged(OrderWriteObservation item)
    {
        item.SessionActive.ShouldBeTrue(); item.Inside.ShouldNotBeNull();
        var state = item.Inside; var expected = Clone(_before);
        var versions = state["productVersions"].Where(d => !_before["productVersions"].Any(old => Id(old) == Id(d))).ToArray();
        versions.Length.ShouldBe(2);
        versions.Select(d => GuidField(d, "ProductId")).Order().ShouldBe(_products.Keys.Order());
        foreach (var version in versions)
        {
            var productId = GuidField(version, "ProductId"); var p = _products[productId];
            Window(version["CreatedAt"].ToUniversalTime(), true);
            version.ShouldBe(new BsonDocument { ["_id"] = Binary(Id(version)), ["ProductId"] = Binary(productId), ["CreatedAt"] = version["CreatedAt"], ["DeactivatedAt"] = BsonNull.Value, ["IsActive"] = true, ["Name"] = p.Name, ["Brand"] = p.Brand, ["PriceAmount"] = p.Amount, ["PriceCurrency"] = p.Currency }, ignoreOrder: true);
        }
        expected["productVersions"] = expected["productVersions"].Concat(versions).OrderBy(Id).ToArray();
        if (item.Stage != "Snapshots")
        {
            var order = state["orders"].Single(d => !_before["orders"].Any(old => Id(old) == Id(d)));
            Window(order["CreatedAt"].ToUniversalTime(), true);
            var cart = _before["shoppingCarts"].Single(d => GuidField(d, "ClientId") == _clientId);
            var lines = cart["Lines"].AsBsonArray.Select(line => new BsonDocument { ["ProductVersionId"] = Binary(Id(versions.Single(v => GuidField(v, "ProductId") == GuidField(line.AsBsonDocument, "ProductId")))), ["Quantity"] = line["Quantity"] }).ToArray();
            order.ShouldBe(new BsonDocument { ["_id"] = Binary(Id(order)), ["ClientId"] = Binary(_clientId), ["CreatedAt"] = order["CreatedAt"], ["UpdatedAt"] = BsonNull.Value, ["Status"] = 2, ["Lines"] = new BsonArray(lines) }, ignoreOrder: true);
            expected["orders"] = expected["orders"].Append(order).OrderBy(Id).ToArray();
        }
        if (item.Stage == "Cart")
        {
            var cart = expected["shoppingCarts"].Single(d => GuidField(d, "ClientId") == _clientId);
            var actual = state["shoppingCarts"].Single(d => Id(d) == Id(cart));
            var updated = actual["UpdatedAt"].ToUniversalTime(); Window(updated, true);
            updated.ShouldBeGreaterThan(cart["UpdatedAt"].ToUniversalTime());
            cart["Lines"] = new BsonArray(); cart["UpdatedAt"] = actual["UpdatedAt"];
        }
        Equal(state, expected); Equal(item.Outside, _before);
    }

    private void NormalizePrimaryDates(JsonElement actual, JsonObject expected)
    {
        var created = actual.GetProperty("createdAt").GetDateTime(); Window(created);
        Milliseconds(created).ShouldBe(expected["createdAt"]!.GetValue<DateTime>()); expected["createdAt"] = JsonValue.Create(created);
        foreach (var line in actual.GetProperty("lines").EnumerateArray())
        {
            var target = expected["lines"]!.AsArray().Single(n => n!["productVersionId"]!.GetValue<string>() == line.GetProperty("productVersionId").GetString())!;
            var date = line.GetProperty("productVersion").GetProperty("createdAt").GetDateTime(); Window(date);
            Milliseconds(date).ShouldBe(target["productVersion"]!["createdAt"]!.GetValue<DateTime>()); target["productVersion"]!["createdAt"] = JsonValue.Create(date);
        }
    }

    private async Task VerifyReads(Dictionary<string, BsonDocument[]> expected, int catalog)
    {
        var reads = new List<(string Path, JsonNode Body)>();
        foreach (var d in expected["shoppingCarts"]) reads.Add(($"/shopping-carts/client/{GuidField(d, "ClientId")}", BusinessValidationSteps.CartProfile(d)));
        foreach (var group in expected["clientDataVersions"].GroupBy(d => GuidField(d, "ClientId"))) reads.Add(($"/client-data-versions/client/{group.Key}", BusinessValidationSteps.ClientProfile(group.MaxBy(d => d["CreatedAt"].ToUniversalTime())!)));
        foreach (var d in expected["orders"]) reads.Add(($"/orders/{Id(d)}", BusinessValidationSteps.OrderProfile(d, expected)));
        foreach (var d in expected["invoices"]) reads.Add(($"/invoices/{Id(d)}", BusinessValidationSteps.InvoiceProfile(d)));
        foreach (var group in expected["orders"].GroupBy(d => GuidField(d, "ClientId"))) reads.Add(($"/orders/client/{group.Key}", new JsonArray(group.OrderBy(Id).Select(d => (JsonNode)BusinessValidationSteps.OrderProfile(d, expected)).ToArray())));
        context.RecordEvidence("Concurrency complete supporting response expectations", reads.Select(r => new { path = r.Path, expected = r.Body }).ToArray());
        foreach (var item in reads)
        {
            using var response = await context.HttpClient.GetAsync(item.Path);
            response.StatusCode.ShouldBe(HttpStatusCode.OK); response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (body.RootElement.ValueKind == JsonValueKind.Array)
            {
                var sorted = new JsonArray(body.RootElement.EnumerateArray().OrderBy(d => d.GetProperty("id").GetGuid()).Select(d => JsonNode.Parse(d.GetRawText())).ToArray());
                JsonNode.DeepEquals(sorted, item.Body).ShouldBeTrue();
            }
            else JsonEqual(body.RootElement, item.Body);
        }
        var actual = await context.Factory.GetCheckoutDocumentsAsync(); Attach("Concurrency database after supporting reads", actual); Equal(actual, expected);
        context.Factory.CatalogRequestCount.ShouldBe(catalog);
        context.RecordEvidence("Concurrency complete supporting reads verified", new { reads = reads.Count, catalogRequests = catalog, allFiveCollectionsUnchanged = true });
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string attempt, string? status = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-Bdd-Attempt", attempt);
        if (status is not null) request.Content = JsonContent.Create(new { status });
        context.RecordEvidence("Concurrency resolved request", new { attempt, method = method.Method, path, status });
        return await context.HttpClient.SendAsync(request);
    }
    internal static async Task Problem(HttpResponseMessage response, int status, string path)
    {
        ((int)response.StatusCode).ShouldBe(status); response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        response.Headers.Location.ShouldBeNull(); response.Content.Headers.ContentDisposition.ShouldBeNull(); response.Content.Headers.Allow.ShouldBeEmpty();
        response.Headers.CacheControl!.ToString().Split(',').Select(s => s.Trim()).Order().ShouldBe(new[] { "no-cache", "no-store" });
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var body = document.RootElement;
        body.EnumerateObject().Select(p => p.Name).Order().ShouldBe(new[] { "detail", "instance", "status", "title", "traceId", "type" });
        body.GetProperty("status").GetInt32().ShouldBe(status); body.GetProperty("instance").GetString().ShouldBe(path); body.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        body.GetProperty("title").GetString().ShouldBe(status == 409 ? "Conflict." : "Server error.");
        body.GetProperty("detail").GetString().ShouldBe(status == 409 ? "Order was changed by another request. Reload it before trying again." : "An unexpected error occurred.");
        body.GetProperty("type").GetString().ShouldBe("https://datatracker.ietf.org/doc/html/rfc7231#section-" + (status == 409 ? "6.5.8" : "6.6.1"));
        body.GetRawText().ShouldNotContain("INV11_PRIVATE"); body.GetRawText().ShouldNotContain("Mongo");
    }
    private void Window(DateTime value, bool milliseconds = false) { value.Kind.ShouldBe(DateTimeKind.Utc); value.ShouldBeGreaterThanOrEqualTo(milliseconds ? Milliseconds(_started) : _started); value.ShouldBeLessThanOrEqualTo(_finished == default ? DateTime.UtcNow : _finished); }
    private void Attach(string name, Dictionary<string, BsonDocument[]> value) => context.RecordEvidence(name, InvoiceGenerationProbe.JsonSnapshot(value));
    internal static void Equal(Dictionary<string, BsonDocument[]> actual, Dictionary<string, BsonDocument[]> expected) { actual.Keys.Order().ShouldBe(expected.Keys.Order()); foreach (var key in expected.Keys) actual[key].ShouldBe(expected[key]); }
    internal static Dictionary<string, BsonDocument[]> Clone(Dictionary<string, BsonDocument[]> value) => value.ToDictionary(p => p.Key, p => p.Value.Select(d => d.DeepClone().AsBsonDocument).ToArray());
    internal static Guid Id(BsonDocument d) => GuidField(d, "_id");
    internal static Guid GuidField(BsonDocument d, string key) => d[key].AsBsonBinaryData.ToGuid();
    private static BsonBinaryData Binary(Guid id) => new(id, GuidRepresentation.Standard);
    private static DateTime Milliseconds(DateTime d) => new(d.Ticks - d.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
    internal static void JsonEqual(JsonElement actual, JsonNode expected) { using var document = JsonDocument.Parse(expected.ToJsonString()); JsonElement.DeepEquals(actual, document.RootElement).ShouldBeTrue($"Expected {expected}; actual {actual}"); }
    private sealed record Product(string Name, string Brand, decimal Amount, string Currency, int Quantity);
}
