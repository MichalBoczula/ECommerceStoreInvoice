using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using Reqnroll;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Configuration;

public sealed class HttpEvidenceTests
{
    [Theory]
    [InlineData("application/json", "{\"name\":\"żółć\",\"active\":false}", "json")]
    [InlineData("application/json", "{bad json", "invalid-json")]
    [InlineData("application/json", "null", "json")]
    [InlineData("application/problem+json", "{\"status\":400,\"code\":\"invalid_json\"}", "json")]
    [InlineData("text/plain", "Unhealthy", "text")]
    [InlineData("application/json", "", "empty")]
    public async Task BodiesAreCapturedWithoutChangingWhatTheServerOrCallerReads(string media, string raw, string format)
    {
        var evidence = Evidence();
        using var client = Client(evidence, async (request, cancellation) =>
        {
            Assert.Equal(raw, await request.Content!.ReadAsStringAsync(cancellation));
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(raw, Encoding.UTF8, media) };
            response.Headers.Location = new Uri("/invoices/created", UriKind.Relative);
            return response;
        });
        using var response = await client.PostAsync("http://localhost/invoices?amount=0", new StringContent(raw, Encoding.UTF8, media));
        Assert.Equal(raw, await response.Content.ReadAsStringAsync());
        var items = Drain(evidence);
        Assert.Equal(2, items.Count);
        var request = items[0];
        Assert.Equal("POST", request.GetProperty("method").GetString());
        Assert.Equal("invoices", request.GetProperty("service").GetString());
        Assert.Equal("When", request.GetProperty("stepPhase").GetString());
        Assert.Equal("/invoices", request.GetProperty("path").GetString());
        Assert.Equal("?amount=0", request.GetProperty("query").GetString());
        Assert.Equal("INV01_CreateInvoiceForOrder_01", request.GetProperty("scenarioId").GetString());
        Assert.Equal(request.GetProperty("exchangeId").GetString(), items[1].GetProperty("exchangeId").GetString());
        foreach (var item in items)
        {
            var content = item.GetProperty("content");
            Assert.Equal(raw, content.GetProperty("rawBody").GetString());
            Assert.Equal(format, content.GetProperty("bodyFormat").GetString());
            Assert.Equal(Encoding.UTF8.GetByteCount(raw), content.GetProperty("byteLength").GetInt32());
            Assert.Equal(Encoding.UTF8.GetBytes(raw), Convert.FromBase64String(content.GetProperty("bodyBytesBase64").GetString()!));
            if (format == "json")
            {
                using var expected = JsonDocument.Parse(raw);
                Assert.True(JsonElement.DeepEquals(expected.RootElement, content.GetProperty("body")));
            }
            else if (format != "empty")
                Assert.Equal(raw, content.GetProperty("body").GetString());
        }
        Assert.Equal(400, items[1].GetProperty("status").GetInt32());
        Assert.Equal("/invoices/created", items[1].GetProperty("location").GetString());
    }

    [Fact]
    public async Task CatalogAndApiExchangesShareIdentityAndSequenceWhileRetainingTheirTargets()
    {
        var evidence = Evidence();
        using var api = Client(evidence, (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        using var products = new HttpClient(new HttpEvidenceHandler(evidence, "products", "checkout-catalog")
        {
            InnerHandler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent("[]", Encoding.UTF8, "application/json") }))
        });
        using var apiResponse = await api.GetAsync("http://localhost/orders");
        using var productsResponse = await products.PostAsync("http://products/mobile-phones/by-ids", new StringContent("[]", Encoding.UTF8, "application/json"));
        var items = Drain(evidence);
        Assert.Equal(4, items.Count);
        Assert.NotEqual(items[0].GetProperty("exchangeId").GetString(), items[2].GetProperty("exchangeId").GetString());
        Assert.All(items, item => Assert.Equal(evidence.InstanceId, item.GetProperty("instanceId").GetString()));
        Assert.All(items.Take(2), item => Assert.Equal("invoices", item.GetProperty("targetService").GetString()));
        Assert.All(items.Skip(2), item => Assert.Equal("products", item.GetProperty("targetService").GetString()));
        Assert.All(items.Skip(2), item => Assert.Equal("checkout-catalog", item.GetProperty("exchangeRole").GetString()));
        Assert.Equal("/mobile-phones/by-ids", items[2].GetProperty("path").GetString());
        Assert.Equal(items[2].GetProperty("exchangeId").GetString(), items[3].GetProperty("exchangeId").GetString());
    }

    [Fact]
    public async Task BodylessRequestsHaveNoInventedJsonAndMultipleClientsShareTheSequence()
    {
        var evidence = Evidence();
        using var first = Client(evidence, (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));
        using var second = Client(evidence, (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));
        using var get = await first.GetAsync("http://localhost/health/live");
        using var delete = await second.DeleteAsync("http://localhost/invoices/id");
        var items = Drain(evidence);
        Assert.Equal(4, items.Count);
        Assert.NotEqual(items[0].GetProperty("exchangeId").GetString(), items[2].GetProperty("exchangeId").GetString());
        Assert.Equal("none", items[0].GetProperty("content").GetProperty("bodyFormat").GetString());
        Assert.Equal(JsonValueKind.Null, items[2].GetProperty("content").GetProperty("body").ValueKind);
        Assert.Equal("empty", items[1].GetProperty("content").GetProperty("bodyFormat").GetString());
    }

    [Fact]
    public async Task NestedServerCallsWithoutTheCallersExecutionContextKeepTheirEvidence()
    {
        var evidence = Evidence();
        using var inner = Client(evidence, (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        using var outer = Client(evidence, async (_, _) =>
        {
            Task<HttpResponseMessage> pending;
            using (ExecutionContext.SuppressFlow())
                pending = Task.Run(() => inner.GetAsync("http://localhost/invoices/winner"));
            using var nested = await pending;
            return new HttpResponseMessage(HttpStatusCode.Conflict);
        });
        using var response = await outer.DeleteAsync("http://localhost/invoices/loser");
        var items = Drain(evidence);
        Assert.Equal(4, items.Count);
        Assert.Equal(2, items.Select(item => item.GetProperty("exchangeId").GetString()).Distinct().Count());
        Assert.All(items, item => Assert.Equal(evidence.ScenarioId, item.GetProperty("scenarioId").GetString()));
        Assert.Equal(409, items[3].GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task EvidenceSurvivesAnOuterValidationFailure()
    {
        var evidence = Evidence();
        using var client = new HttpClient(new RejectResponse
        {
            InnerHandler = new HttpEvidenceHandler(evidence)
            {
                InnerHandler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent("{\"id\":42}", Encoding.UTF8, "application/json") }))
            }
        });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("http://localhost/invoices"));
        Assert.Equal(200, Drain(evidence)[1].GetProperty("status").GetInt32());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TransportErrorsAreRecordedAndRethrownWithoutLeakingExceptionMessages(bool cancelled)
    {
        var evidence = Evidence();
        using var client = Client(evidence, (_, _) => throw (cancelled
            ? new OperationCanceledException("sensitive message") : new HttpRequestException("sensitive message")));
        var error = await Record.ExceptionAsync(() => client.GetAsync("http://localhost/invoices"));
        Assert.True(cancelled ? error is OperationCanceledException : error is HttpRequestException);
        var items = Drain(evidence);
        Assert.Equal(2, items.Count);
        Assert.Equal(cancelled, items[1].GetProperty("cancelled").GetBoolean());
        Assert.DoesNotContain("sensitive message", items[1].GetRawText());
        Assert.Empty(Drain(evidence));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelledResponseBufferingRecordsFailureBeforeSignallingCompletion(bool transportAbort)
    {
        var evidence = Evidence();
        using var cancellation = new CancellationTokenSource();
        var body = new CancelledResponseContent(cancellation, transportAbort);
        using var client = Client(evidence, (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = body }));
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/invoices");
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var headers = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        request.Options.Set(HttpEvidenceHandler.CompletionKey, completed);
        request.Options.Set(HttpEvidenceHandler.ResponseHeadersKey, headers);
        var error = await Record.ExceptionAsync(() => client.SendAsync(request, cancellation.Token));
        Assert.True(error is OperationCanceledException or HttpRequestException);
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(headers.Task.IsCompletedSuccessfully);
        Assert.True(body.WasDisposed);
        Assert.True(cancellation.IsCancellationRequested);
        var items = Drain(evidence);
        Assert.Equal(2, items.Count);
        Assert.Equal(items[0].GetProperty("exchangeId").GetString(), items[1].GetProperty("exchangeId").GetString());
        Assert.True(items[1].GetProperty("cancelled").GetBoolean());
        Assert.True(items[1].GetProperty("cancellationRequested").GetBoolean());
        Assert.Equal("response-body", items[1].GetProperty("failureStage").GetString());
        Assert.Equal(transportAbort ? "HttpRequestException" : "OperationCanceledException", items[1].GetProperty("exceptionType").GetString());
        Assert.False(items[1].TryGetProperty("status", out _));
        Assert.DoesNotContain("sensitive message", items[1].GetRawText());
    }

    [Fact]
    public void ExplicitIdentityIncludesExamplesWithoutDependingOnScenarioWording()
    {
        var arguments = new OrderedDictionary { ["scenarioId"] = "INV01_GetInvoiceByOrderId_01", ["amount"] = "0" };
        var first = ScenarioHttpEvidence.From(new ScenarioInfo("old title", "", [], arguments), "Invoices");
        var renamed = ScenarioHttpEvidence.From(new ScenarioInfo("new title", "", [], arguments), "Invoices");
        Assert.Equal("INV01_GetInvoiceByOrderId_01", first.ScenarioId);
        Assert.Equal(first.InstanceId, renamed.InstanceId);
        arguments["amount"] = "-1";
        var otherExample = ScenarioHttpEvidence.From(new ScenarioInfo("old title", "", [], arguments), "Invoices");
        Assert.NotEqual(first.InstanceId, otherExample.InstanceId);
        Assert.Throws<InvalidOperationException>(() => ScenarioHttpEvidence.From(new ScenarioInfo("missing ID", "", [], new OrderedDictionary()), "Invoices"));
        var tagged = ScenarioHttpEvidence.From(new ScenarioInfo("tagged", "", ["allure.label.scenarioId:INV01_GetFlowDocumentation_01"], new OrderedDictionary()), "Health");
        Assert.Equal("INV01_GetFlowDocumentation_01", tagged.ScenarioId);
        Assert.Throws<InvalidOperationException>(() => ScenarioHttpEvidence.From(
            new ScenarioInfo("ambiguous", "", ["allure.label.scenarioId:INV01_GetFlowDocumentation_01"], arguments), "Health"));
    }

    [Fact]
    public async Task ScenarioRecordersNeverShareParametersOrExchangeSequences()
    {
        var parameters = new SortedDictionary<string, string?> { ["run"] = "first" };
        var first = new ScenarioHttpEvidence("INV01_CreateShoppingCart_01", "Isolation", "Run", parameters);
        parameters["run"] = "second";
        var second = new ScenarioHttpEvidence("INV01_CreateShoppingCart_01", "Isolation", "Run", parameters);
        Assert.Equal("first", first.Parameters["run"]);
        Assert.NotEqual(first.InstanceId, second.InstanceId);
        using var firstClient = Client(first, (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        using var secondClient = Client(second, (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        using var one = await firstClient.GetAsync("http://localhost/health");
        using var two = await secondClient.GetAsync("http://localhost/health");
        var firstExchange = Drain(first)[0].GetProperty("exchangeId").GetString();
        var secondExchange = Drain(second)[0].GetProperty("exchangeId").GetString();
        Assert.Equal(first.InstanceId + "/1", firstExchange);
        Assert.Equal(second.InstanceId + "/1", secondExchange);
    }

    [Fact]
    public async Task BinaryPdfRetainsExactBytesHeadersAndDownloadableAttachment()
    {
        byte[] pdf = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37, 0x0A, 0xFF, 0x00, 0xC3, 0x28];
        var evidence = Evidence();
        using var client = Client(evidence, (_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(pdf) };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment") { FileNameStar = "invoice-test.pdf" };
            response.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { Private = true, NoStore = true };
            return Task.FromResult(response);
        });
        using var response = await client.GetAsync("http://localhost/invoices/id/pdf");
        Assert.Equal(pdf, await response.Content.ReadAsByteArrayAsync());
        var items = new List<JsonElement>();
        var files = new List<(string Name, byte[] Bytes)>();
        evidence.Flush((_, value) => items.Add(JsonSerializer.SerializeToElement(value, ScenarioHttpEvidence.JsonOptions)),
            (name, bytes) => files.Add((name, bytes)));
        Assert.Equal(2, items.Count);
        var actual = items[1];
        var content = actual.GetProperty("content");
        Assert.Equal("binary", content.GetProperty("bodyFormat").GetString());
        Assert.Equal(JsonValueKind.Null, content.GetProperty("rawBody").ValueKind);
        Assert.Equal(JsonValueKind.Null, content.GetProperty("body").ValueKind);
        Assert.Equal(pdf.Length, content.GetProperty("byteLength").GetInt32());
        Assert.Equal(pdf, Convert.FromBase64String(content.GetProperty("bodyBytesBase64").GetString()!));
        Assert.Equal(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(pdf)).ToLowerInvariant(), content.GetProperty("sha256").GetString());
        Assert.Equal(response.Content.Headers.ContentDisposition!.ToString(), actual.GetProperty("contentDisposition").GetString());
        Assert.Equal(response.Headers.CacheControl!.ToString(), actual.GetProperty("cacheControl").GetString());
        var file = Assert.Single(files);
        Assert.Equal(pdf, file.Bytes);
        Assert.Contains(actual.GetProperty("exchangeId").GetString()!, file.Name);
    }

    [Fact]
    public async Task ParallelRequestsKeepDistinctCompletePairs()
    {
        var evidence = Evidence();
        var entered = 0;
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var client = Client(evidence, async (_, _) =>
        {
            if (Interlocked.Increment(ref entered) == 16) release.TrySetResult();
            await release.Task.WaitAsync(TimeSpan.FromSeconds(5));
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var responses = await Task.WhenAll(Enumerable.Range(0, 16).Select(i => client.GetAsync($"http://localhost/invoices/{i}")));
        foreach (var response in responses) response.Dispose();
        var items = Drain(evidence);
        Assert.Equal(32, items.Count);
        var exchanges = items.GroupBy(item => item.GetProperty("exchangeId").GetString()).ToArray();
        Assert.Equal(16, exchanges.Length);
        foreach (var exchange in exchanges)
        {
            Assert.Equal(2, exchange.Count());
            Assert.Single(exchange, item => item.TryGetProperty("status", out _));
            Assert.Single(exchange.Select(item => item.GetProperty("path").GetString()).Distinct());
            Assert.All(exchange, item => Assert.Equal("When", item.GetProperty("stepPhase").GetString()));
        }
    }

    [Fact]
    public async Task UnreadableRequestHasTruthfulUnavailableBodyAndNoInventedResponse()
    {
        var evidence = Evidence();
        var sent = false;
        using var client = Client(evidence, (_, _) => { sent = true; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)); });
        var error = await Record.ExceptionAsync(() => client.PostAsync("http://localhost/invoices", new BrokenRequestContent()));
        Assert.NotNull(error);
        Assert.False(sent);
        var items = Drain(evidence);
        Assert.Equal(2, items.Count);
        Assert.Equal("unavailable", items[0].GetProperty("content").GetProperty("bodyFormat").GetString());
        Assert.Equal(JsonValueKind.Null, items[0].GetProperty("content").GetProperty("byteLength").ValueKind);
        Assert.Equal(JsonValueKind.Null, items[0].GetProperty("content").GetProperty("bodyBytesBase64").ValueKind);
        Assert.Equal("request-body", items[1].GetProperty("failureStage").GetString());
        Assert.False(items[1].TryGetProperty("status", out _));
        Assert.DoesNotContain("sensitive request", string.Join("", items.Select(item => item.GetRawText())));
    }

    private sealed class BrokenRequestContent : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            Task.FromException(new IOException("sensitive request bytes"));
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
    }

    [Fact]
    public async Task GroupedObservationsRetainRepeatedNamesAndIndividualHttpPairs()
    {
        var evidence = new ScenarioHttpEvidence("INV10_Framework_Grouping", "Framework", "Grouping", new(), groupObservations: true) { StepPhase = "When" };
        evidence.RecordObservation("State", new { position = "before", value = 1 });
        using var client = Client(evidence, (_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.MethodNotAllowed) { Content = new StringContent("{}") };
            response.Content.Headers.Allow.Add("GET");
            response.Content.Headers.Allow.Add("POST");
            return Task.FromResult(response);
        });
        using var request = new HttpRequestMessage(HttpMethod.Delete, "http://localhost/orders/id");
        request.Headers.Add("X-Bdd-Attempt", "second");
        using var response = await client.SendAsync(request);
        evidence.RecordObservation("State", new { position = "after", value = 1 });
        var items = Drain(evidence);
        Assert.Equal(3, items.Count);
        Assert.Equal(items[0].GetProperty("exchangeId").GetString(), items[1].GetProperty("exchangeId").GetString());
        Assert.Equal(new[] { "GET", "POST" }, items[1].GetProperty("allow").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal("second", items[0].GetProperty("requestAttempt").GetString());
        Assert.Equal("second", items[1].GetProperty("requestAttempt").GetString());
        var observations = items[2].GetProperty("observations").EnumerateArray().ToArray();
        Assert.Equal(new[] { "State", "State" }, observations.Select(value => value.GetProperty("name").GetString()));
        Assert.Equal(new[] { "before", "after" }, observations.Select(value => value.GetProperty("value").GetProperty("position").GetString()));
        Assert.Equal(evidence.ScenarioId, items[2].GetProperty("scenarioId").GetString());
        Assert.Equal(evidence.InstanceId, items[2].GetProperty("instanceId").GetString());
        Assert.All(observations, value => Assert.Equal("When", value.GetProperty("stepPhase").GetString()));
        Assert.Empty(Drain(evidence));
    }

    [Fact]
    public void GroupingKeepsIdentityAndStepBoundaries()
    {
        var flat = Evidence();
        var grouped = new ScenarioHttpEvidence(flat.ScenarioId, flat.Feature, flat.Scenario, flat.Parameters, groupObservations: true) { StepPhase = "Given" };
        Assert.Equal(flat.InstanceId, grouped.InstanceId);
        var mutable = new Dictionary<string, int> { ["count"] = 2 };
        grouped.RecordObservation("Fixture", mutable);
        mutable["count"] = 9;
        var intermediate = new List<object>();
        grouped.Flush((_, value) => intermediate.Add(value), flushObservations: false);
        Assert.Empty(intermediate);
        grouped.StepPhase = "Then";
        grouped.RecordObservation("Check", new { count = 2 });
        var group = Assert.Single(Drain(grouped));
        var entries = group.GetProperty("observations").EnumerateArray().ToArray();
        Assert.Equal(2, entries.Length);
        Assert.Equal("Given", entries[0].GetProperty("stepPhase").GetString());
        Assert.Equal("Then", entries[1].GetProperty("stepPhase").GetString());
        Assert.Equal("Fixture", entries[0].GetProperty("name").GetString());
        Assert.Equal("Check", entries[1].GetProperty("name").GetString());
        Assert.Equal(2, entries[0].GetProperty("value").GetProperty("count").GetInt32());
        Assert.Empty(Drain(grouped));
    }

    [Fact]
    public async Task ConcurrentObservationsStayCompleteAndScenarioLocal()
    {
        var first = new ScenarioHttpEvidence("INV10_Framework_Grouping", "Framework", "Grouping", new() { ["owner"] = "A" }, groupObservations: true);
        var second = new ScenarioHttpEvidence("INV10_Framework_Grouping", "Framework", "Grouping", new() { ["owner"] = "B" }, groupObservations: true);
        await Task.WhenAll(Enumerable.Range(0, 16).Select(index => Task.Run(() =>
        {
            first.RecordObservation("Entry", new { owner = "A", index });
            second.RecordObservation("Entry", new { owner = "B", index });
        })));
        foreach (var pair in new[] { (Evidence: first, Owner: "A"), (Evidence: second, Owner: "B") })
        {
            var group = Assert.Single(Drain(pair.Evidence));
            var entries = group.GetProperty("observations").EnumerateArray().ToArray();
            Assert.Equal(16, entries.Length);
            Assert.Equal(Enumerable.Range(0, 16), entries.Select(entry => entry.GetProperty("value").GetProperty("index").GetInt32()).Order());
            Assert.All(entries, entry => Assert.Equal(pair.Owner, entry.GetProperty("value").GetProperty("owner").GetString()));
            Assert.Equal(pair.Evidence.InstanceId, group.GetProperty("instanceId").GetString());
        }
    }

    private static ScenarioHttpEvidence Evidence() => new("INV01_CreateInvoiceForOrder_01", "Invoices", "Create invoice", new()) { StepPhase = "When" };
    private static HttpClient Client(ScenarioHttpEvidence evidence, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        => new(new HttpEvidenceHandler(evidence) { InnerHandler = new StubHandler(send) });
    private static List<JsonElement> Drain(ScenarioHttpEvidence evidence)
    {
        var items = new List<JsonElement>();
        evidence.Flush((_, value) => items.Add(JsonSerializer.SerializeToElement(value, ScenarioHttpEvidence.JsonOptions)));
        return items;
    }

    private sealed class CancelledResponseContent(CancellationTokenSource cancellation, bool transportAbort) : HttpContent
    {
        internal bool WasDisposed { get; private set; }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            cancellation.Cancel();
            return Task.FromException(transportAbort
                ? new HttpRequestException("sensitive message") : new OperationCanceledException("sensitive message"));
        }
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override void Dispose(bool disposing) { WasDisposed = true; base.Dispose(disposing); }
    }

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }
    private sealed class RejectResponse : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using var response = await base.SendAsync(request, cancellationToken);
            throw new InvalidOperationException("Contract mismatch");
        }
    }
}

