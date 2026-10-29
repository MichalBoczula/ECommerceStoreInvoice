using System.Net;
using System.Text;
using System.Text.Json;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using MongoDB.Bson;
using Reqnroll;
using Shouldly;
using static ECommerceStoreInvoice.Acceptance.Tests.Features.Orders.Concurrency.OrderConcurrencySteps;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Technical;

[Binding]
public sealed class TechnicalSteps(ScenarioApiContext context, BusinessValidationSteps fixture) : IAsyncDisposable
{
    private HttpClient? _technical;
    private ApplicationFactory? _peer;
    private HttpClient? _peerClient;
    private Dictionary<string, BsonDocument[]>? _expected;
    private Dictionary<string, BsonDocument[]>? _peerExpected;
    private Dictionary<string, byte[]> _documents = new();
    private readonly Dictionary<string, string> _contract = new();
    private InvoiceReadArtifact[] _files = [];
    private Guid _invoiceId;
    private HttpClient Technical => _technical ??= context.CreateTechnicalClient(context.Factory);

    [Given("technical outcomes use this complete contract")]
    public void GivenContract(Table table)
    {
        context.RegisterCleanup(() => DisposeAsync().AsTask());
        foreach (var row in table.Rows) _contract.Add(row["Field"], row["Value"]);
        _contract["Collections"].ShouldBe("all-five-populated-unchanged");
        _contract["CatalogRequests"].ShouldBe("0");
        _contract["ProbeFormat"].ShouldBe("plain-health-status");
        context.RecordEvidence("Technical complete contract table", _contract);
        _expected = fixture.FixtureState;
    }

    [When("I request the technical health route {string}")]
    public async Task WhenHealth(string route)
    {
        var count = context.Factory.ReadinessCommands.Count;
        await Probe(route, 200);
        if (route != "/health/ready") context.Factory.ReadinessCommands.Count.ShouldBe(count);
        else context.Factory.ReadinessCommands.Skip(count).Select(c => c.Command).ShouldBe(new[] { "hello", "ping" });
        var indexes = await context.Factory.TechnicalIndexesAsync();
        Index(indexes, "shoppingCarts", "UX_ShoppingCart_ClientId", new BsonDocument("ClientId", 1), true);
        Index(indexes, "invoices", "UX_Invoice_OrderId", new BsonDocument("OrderId", 1), true);
        Index(indexes, "clientDataVersions", "IX_ClientDataVersion_ClientId_CreatedAtDesc", new BsonDocument { ["ClientId"] = 1, ["CreatedAt"] = -1 }, false);
        context.RecordEvidence("Technical actual startup index definitions", InvoiceGenerationProbe.JsonSnapshot(indexes));
    }

    [When("the owned Mongo replica stops and restarts while the host stays running")]
    public async Task WhenOutage()
    {
        context.Factory.TechnicalMongoMode.ShouldBe("replica");
        await Probe("/health/ready", 200);
        await DocumentationSnapshot();
        await context.Factory.StopTechnicalMongoAsync();
        context.RecordEvidence("Technical owned Mongo lifecycle", new { transition = "stopped", owned = true, sharedMongoTouched = false, database = context.Factory.DatabaseName, ownedContainerId = context.Factory.TechnicalMongoContainerId, sharedContainerId = context.Factory.SharedMongoContainerId });
        var commandCount = context.Factory.ReadinessCommands.Count;
        await Probe("/health/live", 200); await Probe("/health", 200);
        context.Factory.ReadinessCommands.Count.ShouldBe(commandCount);
        await Probe("/health/ready", 503);
        await DocumentationSnapshot();
        await context.Factory.RestartTechnicalMongoAsync();
        context.RecordEvidence("Technical owned Mongo lifecycle", new { transition = "restarted", owned = true, sharedMongoTouched = false, database = context.Factory.DatabaseName, ownedContainerId = context.Factory.TechnicalMongoContainerId, sharedContainerId = context.Factory.SharedMongoContainerId });
        await Probe("/health/ready", 200);
        context.RecordEvidence("Technical real readiness commands", context.Factory.ReadinessCommands.Select(c => new { c.Command, c.Database }).ToArray());
    }

    [When("the standalone Mongo host is live but is not ready for transactions")]
    public async Task WhenStandalone()
    {
        context.Factory.TechnicalMongoMode.ShouldBe("standalone");
        var hello = await context.Factory.TechnicalHelloAsync();
        hello.Contains("setName").ShouldBeFalse();
        (hello.TryGetValue("msg", out var message) && message.IsString && message.AsString == "isdbgrid").ShouldBeFalse();
        context.RecordEvidence("Technical actual standalone hello", JsonSerializer.Deserialize<JsonElement>(hello.ToJson()));
        var count = context.Factory.ReadinessCommands.Count;
        await Probe("/health/live", 200); await Probe("/health", 200);
        context.Factory.ReadinessCommands.Count.ShouldBe(count);
        await Probe("/health/ready", 503);
        var commands = context.Factory.ReadinessCommands.Skip(count).ToArray();
        commands.Select(c => c.Command).ShouldBe(new[] { "hello" });
        context.RecordEvidence("Technical rejected topology command boundary", new { commands = commands.Select(c => new { c.Command, c.Database }), pingSkipped = true });
    }

    [When("I read complete technical documentation {string} twice")]
    public async Task WhenDocs(string kind)
    {
        byte[]? original = null;
        for (var index = 0; index < 2; index++)
        {
            using var response = await context.HttpClient.GetAsync($"/orders-documentation/{kind}");
            await TechnicalDocumentation.AssertAsync(context, response, kind);
            var bytes = await response.Content.ReadAsByteArrayAsync();
            if (original is not null) bytes.ShouldBe(original);
            original = bytes;
        }
        context.RecordEvidence("Technical documentation repeated response verified", new { kind, completeByteEquality = true });
    }

    [When("I request Swagger representation {string}")]
    public async Task WhenSwagger(string representation)
    {
        var path = representation == "json" ? "/swagger/v1/swagger.json" : "/swagger/index.html";
        using var response = await Technical.GetAsync(path);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        if (representation == "json")
        {
            response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            JsonElement.DeepEquals(body.RootElement, context.OpenApiDocument.RootElement).ShouldBeTrue();
            var operations = body.RootElement.GetProperty("paths").EnumerateObject().SelectMany(path => path.Value.EnumerateObject()).Where(p => p.Value.ValueKind == JsonValueKind.Object && p.Value.TryGetProperty("operationId", out _)).Select(p => p.Value.GetProperty("operationId").GetString()).ToArray();
            operations.Length.ShouldBe(15); operations.Distinct().Count().ShouldBe(15);
            context.RecordEvidence("Technical Swagger complete source equality", new { operations, completeSourceEqual = true });
        }
        else
        {
            representation.ShouldBe("ui"); response.Content.Headers.ContentType?.MediaType.ShouldBe("text/html");
            var html = await response.Content.ReadAsStringAsync();
            html.ShouldContain("SwaggerUIBundle"); html.ShouldContain("swagger-ui"); html.ShouldContain("v1/swagger.json");
            context.RecordEvidence("Technical Swagger UI entry verified", new { path, contentType = "text/html", realEntryServed = true, referencesGeneratedDocument = true });
        }
    }

    [Given("the completed technical invoices have real immutable PDF files")]
    public async Task GivenFiles()
    {
        _expected.ShouldNotBeNull();
        var storage = context.Factory.ReadStorage!;
        foreach (var doc in _expected["invoices"])
        {
            var id = Id(doc);
            var url = await storage.PutAsync(id.ToString(), id, Guid.NewGuid(), InvoiceReadStorage.BinaryPdf("INV12 immutable invoice " + id), false);
            doc["StorageUrl"] = url;
            await context.Factory.ReplaceInvoiceStorageFixtureAsync(id, url);
        }
        fixture.SetTechnicalFixtureState(_expected);
        Equal(await context.Factory.GetCheckoutDocumentsAsync(), _expected);
        _files = await storage.ReadStateAsync();
        _invoiceId = Guid.Parse(fixture.ResolveFixturePath("{invoiceId}"));
        context.RecordEvidence("Technical exact PDF artifact fixture", storage.ExpectedEvidence());
        context.RecordEvidence("Technical storage before cancellation", _files);
    }

    [When("the real PDF request is cancelled {string}")]
    public async Task WhenCancel(string mode)
    {
        var probe = context.Factory.CancellationProbe!;
        probe.Mode = mode;
        using var cancellation = new CancellationTokenSource();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/invoices/{_invoiceId}/pdf");
        var captured = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        request.Options.Set(HttpEvidenceHandler.CompletionKey, captured);
        var pending = context.HttpClient.SendAsync(request, cancellation.Token);
        try
        {
            await probe.WaitEnteredAsync();
            cancellation.Cancel();
            var error = await Record.ExceptionAsync(() => pending);
            (error is OperationCanceledException or HttpRequestException).ShouldBeTrue();
            await probe.WaitDisposedAsync();
            await captured.Task.WaitAsync(TimeSpan.FromSeconds(15));
            probe.ServerCancelled.ShouldBeTrue(); probe.ActualStreamDisposed.ShouldBeTrue();
            probe.BytesRead.ShouldBe(mode == "during-body" ? 32 : 0);
            context.RecordEvidence("Technical cancelled HTTP outcome verified", new { mode, clientExceptionType = error!.GetType().Name, serverTokenCancelled = true, actualStreamDisposed = true, bytesReadFromActualFile = probe.BytesRead, httpStatus = (int?)null, fabricated499 = false });
        }
        finally
        {
            cancellation.Cancel();
            try { using var response = await pending.WaitAsync(TimeSpan.FromSeconds(20)); }
            catch (Exception exception) when (exception is OperationCanceledException or HttpRequestException) { }
        }
    }

    [Then("the cancelled PDF can be downloaded intact with all data unchanged")]
    public async Task ThenCancelled()
    {
        using var response = await context.HttpClient.GetAsync($"/invoices/{_invoiceId}/pdf");
        response.StatusCode.ShouldBe(HttpStatusCode.OK); response.Content.Headers.ContentType?.MediaType.ShouldBe("application/pdf");
        response.Content.Headers.ContentDisposition!.DispositionType.ShouldBe("attachment");
        response.Content.Headers.ContentDisposition.FileNameStar.ShouldBe($"invoice-{_invoiceId:D}.pdf");
        response.Headers.CacheControl!.ToString().Split(',').Select(s => s.Trim()).Order().ShouldBe(new[] { "no-store", "private" });
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.ShouldBe(context.Factory.ReadStorage!.ExpectedBytes(_invoiceId.ToString()));
        (await context.Factory.ReadStorage.ReadStateAsync()).ShouldBe(_files);
        context.RecordEvidence("Technical intact PDF retry verified", new { invoiceId = _invoiceId, byteLength = bytes.Length, sha256 = InvoiceReadStorage.Hash(bytes), storageUnchanged = true, completeBinaryEqual = true });
        await ThenUnchanged();
    }

    [When("another host uses the same business IDs in its own database and is disposed")]
    public async Task WhenIsolated()
    {
        _expected.ShouldNotBeNull();
        _peer = new ApplicationFactory { CatalogEvidenceHandlerFactory = context.CreateTechnicalCatalogHandler };
        await _peer.InitializeAsync();
        _peer.DatabaseName.ShouldNotBe(context.Factory.DatabaseName);
        _peerExpected = Clone(_expected);
        foreach (var d in _peerExpected["shoppingCarts"]) foreach (var line in d["Lines"].AsBsonArray) line["Quantity"] = line["Quantity"].AsInt32 + 100;
        foreach (var d in _peerExpected["orders"].Where(d => d["Status"].AsInt32 == 2))
        {
            d["Status"] = 1;
            d["UpdatedAt"] = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        }
        foreach (var d in _peerExpected["productVersions"]) d["Name"] = "Peer." + d["Name"].AsString;
        foreach (var d in _peerExpected["clientDataVersions"]) d["ClientName"] = "Peer." + d["ClientName"].AsString;
        foreach (var d in _peerExpected["invoices"]) d["StorageUrl"] = "file:///fixtures/peer-" + Id(d) + ".pdf";
        await _peer.SeedOrderReadDocumentsAsync(_peerExpected);
        _peerClient = context.CreateClient(_peer, "api-peer");
        context.RecordEvidence("Technical isolated host identity mapping", new { primaryDatabase = context.Factory.DatabaseName, peerDatabase = _peer.DatabaseName, sameBusinessIds = true, primaryRole = "api", peerRole = "api-peer", peerExpected = InvoiceGenerationProbe.JsonSnapshot(_peerExpected) });
        Equal(await _peer.GetCheckoutDocumentsAsync(), _peerExpected);
        await fixture.VerifyFixtureReadsAsync();
        fixture.SetTechnicalFixtureState(_peerExpected);
        var original = context.HttpClient;
        var primaryFactory = context.Factory;
        try { context.HttpClient = _peerClient; context.Factory = _peer; await fixture.VerifyFixtureReadsAsync(); }
        finally { context.HttpClient = original; context.Factory = primaryFactory; fixture.SetTechnicalFixtureState(_expected); }
        context.Factory.CatalogRequestCount.ShouldBe(0); _peer.CatalogRequestCount.ShouldBe(0);
        (await context.Factory.DatabaseExistsAsync(_peer.DatabaseName)).ShouldBeTrue();
        _peerClient.Dispose(); _peerClient = null;
        var peerDatabase = _peer.DatabaseName;
        await _peer.DisposeAsync(); _peer = null;
        (await context.Factory.DatabaseExistsAsync(peerDatabase)).ShouldBeFalse();
        (await context.Factory.DatabaseExistsAsync(context.Factory.DatabaseName)).ShouldBeTrue();
        Equal(await context.Factory.GetCheckoutDocumentsAsync(), _expected);
        await Probe("/health/ready", 200);
        context.RecordEvidence("Technical isolated host cleanup verified", new { peerDatabase, peerDatabaseDropped = true, primaryDatabaseRetained = true, sameBusinessIdsNeverCrossed = true, expectedPrimary = InvoiceGenerationProbe.JsonSnapshot(_expected), actualPrimary = InvoiceGenerationProbe.JsonSnapshot(await context.Factory.GetCheckoutDocumentsAsync()) });
    }

    [Then("technical operations preserve all populated collections and complete supporting reads")]
    public async Task ThenUnchanged()
    {
        _expected.ShouldNotBeNull();
        Equal(await context.Factory.GetCheckoutDocumentsAsync(), _expected);
        await fixture.VerifyFixtureReadsAsync();
        Equal(await context.Factory.GetCheckoutDocumentsAsync(), _expected);
        context.Factory.CatalogRequestCount.ShouldBe(0);
        context.RecordEvidence("Technical unchanged populated state verified", new { expected = InvoiceGenerationProbe.JsonSnapshot(_expected), actual = InvoiceGenerationProbe.JsonSnapshot(await context.Factory.GetCheckoutDocumentsAsync()), catalogRequests = 0, allFiveCollectionsUnchanged = true });
    }

    private async Task Probe(string path, int status)
    {
        using var response = await Technical.GetAsync(path);
        ((int)response.StatusCode).ShouldBe(status); response.Content.Headers.ContentType?.MediaType.ShouldBe("text/plain");
        var body = await response.Content.ReadAsByteArrayAsync();
        body.ShouldBe(Encoding.UTF8.GetBytes(status == 200 ? "Healthy" : "Unhealthy"));
        response.Headers.CacheControl!.NoCache.ShouldBeTrue(); response.Headers.CacheControl.NoStore.ShouldBeTrue();
        response.Content.Headers.ContentDisposition.ShouldBeNull(); response.Headers.Location.ShouldBeNull();
        context.RecordEvidence("Technical complete health response verified", new { path, status, body = Encoding.UTF8.GetString(body), contentType = "text/plain", noCache = true, noStore = true, businessProblem = false });
    }
    private async Task DocumentationSnapshot()
    {
        foreach (var kind in new[] { "flows", "validations" })
        {
            using var response = await context.HttpClient.GetAsync($"/orders-documentation/{kind}");
            await TechnicalDocumentation.AssertAsync(context, response, kind);
            var bytes = await response.Content.ReadAsByteArrayAsync();
            if (_documents.TryGetValue(kind, out var previous)) bytes.ShouldBe(previous);
            _documents[kind] = bytes;
        }
        context.RecordEvidence("Technical documentation independent of Mongo availability", new { completeSourceEquality = true, repeatedBytesUnchanged = true });
    }
    private static void Index(Dictionary<string, BsonDocument[]> indexes, string collection, string name, BsonDocument key, bool unique)
    {
        var index = indexes[collection].Single(d => d["name"] == name);
        index["key"].AsBsonDocument.ShouldBe(key);
        (index.TryGetValue("unique", out var value) && value.AsBoolean).ShouldBe(unique);
    }
    public async ValueTask DisposeAsync()
    {
        try { _technical?.Dispose(); _peerClient?.Dispose(); if (_peer is not null) await _peer.DisposeAsync(); }
        finally { _technical = null; _peerClient = null; _peer = null; }
    }
}
