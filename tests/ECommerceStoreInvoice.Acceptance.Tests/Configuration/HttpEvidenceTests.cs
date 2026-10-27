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
        request.Options.Set(HttpEvidenceHandler.CompletionKey, completed);
        var error = await Record.ExceptionAsync(() => client.SendAsync(request, cancellation.Token));
        Assert.True(error is OperationCanceledException or HttpRequestException);
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(body.WasDisposed);
        Assert.True(cancellation.IsCancellationRequested);
        var items = Drain(evidence);
        Assert.Equal(2, items.Count);
        Assert.Equal(items[0].GetProperty("exchangeId").GetString(), items[1].GetProperty("exchangeId").GetString());
        Assert.True(items[1].GetProperty("cancelled").GetBoolean());
        Assert.True(items[1].GetProperty("cancellationRequested").GetBoolean());
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
            Assert.Single(exchange.Where(item => item.TryGetProperty("status", out _)));
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

