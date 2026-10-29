using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MongoDB.Bson;
using Reqnroll;
using Shouldly;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Common;

[Binding]
public sealed class FrameworkFailureSteps(ScenarioApiContext context, BusinessValidationSteps fixture) : IDisposable
{
    private readonly Dictionary<int, Dictionary<string, string>> _signatures = new();
    private readonly Dictionary<string, Dictionary<string, string>> _faultRequests = new(StringComparer.Ordinal);
    private Dictionary<string, BsonDocument[]>? _before;
    private Dictionary<string, BsonDocument[]>? _after;
    private Dictionary<string, string> _request = new(StringComparer.Ordinal);
    private string _path = string.Empty;
    private HttpClient? _routingClient;

    [Given("framework problems have these complete safe signatures")]
    public void GivenSignatures(Table table)
    {
        context.RecordEvidence("Framework problem signature table", Rows(table));
        foreach (var row in table.Rows) _signatures.Add(Integer(row["Status"]), row.ToDictionary(pair => pair.Key, pair => pair.Value));
    }

    [Given("the exact framework request is")]
    public void GivenRequest(Table table)
    {
        _request = table.Rows.ToDictionary(row => row["Field"], row => row["Value"], StringComparer.Ordinal);
        context.RecordEvidence("Framework raw request contract table", _request);
    }

    [Given("controlled server faults use these complete request contracts")]
    public void GivenFaultRequests(Table table)
    {
        context.RecordEvidence("Controlled fault request contract table", Rows(table));
        foreach (var row in table.Rows) _faultRequests.Add(row["Operation"], row.ToDictionary(pair => pair.Key, pair => pair.Value));
    }

    [When("I send the exact framework request")]
    public Task WhenSend() => SendAsync(_request, _request["Mode"] == "routing" ? (_routingClient ??= context.CreateRoutingClient()) : context.HttpClient);

    internal Task SendFaultAsync(string operation, HttpClient client) => SendAsync(_faultRequests[operation], client);

    private async Task SendAsync(Dictionary<string, string> values, HttpClient client)
    {
        _path = fixture.ResolveFixturePath(values["Path"]);
        var hasContent = values["Body"] != "<no-content>";
        var bytes = values["Body"].StartsWith("hex:", StringComparison.Ordinal)
            ? Convert.FromHexString(values["Body"][4..])
            : Encoding.UTF8.GetBytes(values["Body"] is "<empty>" or "<no-content>" ? string.Empty : values["Body"]);
        using var request = new HttpRequestMessage(new HttpMethod(values["Method"]), _path);
        if (hasContent)
        {
            request.Content = new ByteArrayContent(bytes);
            if (values["ContentType"] != "<none>") request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(values["ContentType"]);
        }
        _before = await context.Factory.GetCheckoutDocumentsAsync();
        EqualState(_before, fixture.FixtureState);
        Attach("Framework database before primary request", _before);
        context.RecordEvidence("Framework resolved exact request", new
        {
            method = values["Method"],
            path = _path,
            mode = values.GetValueOrDefault("Mode", "controlled-fault"),
            hasContent,
            contentType = request.Content?.Headers.ContentType?.ToString(),
            byteLength = bytes.Length,
            bodyBytesBase64 = hasContent ? Convert.ToBase64String(bytes) : null,
            sha256 = hasContent ? Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant() : null
        });
        context.Response = await client.SendAsync(request);
        if (hasContent) (await request.Content!.ReadAsByteArrayAsync()).ShouldBe(bytes);
        else request.Content.ShouldBeNull();
        _after = await context.Factory.GetCheckoutDocumentsAsync();
        Attach("Framework database after primary request", _after);
        context.RecordEvidence("Framework exact request bytes verified", new { path = _path, hasContent, byteLength = bytes.Length });
    }

    [Then("the complete framework problem is {int} with allowed methods {string}")]
    public async Task ThenProblem(int status, string allow) => await AssertProblemAsync(status, allow);

    internal async Task AssertProblemAsync(int status, string allow)
    {
        var expected = _signatures[status];
        context.Response.ShouldNotBeNull();
        var response = context.Response;
        ((int)response.StatusCode).ShouldBe(status);
        response.Content.Headers.ContentType?.MediaType.ShouldBe(expected["ContentType"]);
        response.Headers.Location.ShouldBeNull();
        response.Content.Headers.ContentDisposition.ShouldBeNull();
        string[] allowed = allow == "<none>" ? [] : allow.Split(',').Select(value => value.Trim()).ToArray();
        response.Content.Headers.Allow.Order().ShouldBe(allowed.Order());
        response.Headers.CacheControl!.ToString().Split(',').Select(value => value.Trim()).Order().ShouldBe(new[] { "no-cache", "no-store" });
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var problem = document.RootElement;
        problem.EnumerateObject().Select(property => property.Name).Order().ShouldBe(new[] { "detail", "instance", "status", "title", "traceId", "type" });
        problem.GetProperty("status").GetInt32().ShouldBe(status);
        foreach (var field in new[] { "Type", "Title", "Detail" }) problem.GetProperty(char.ToLowerInvariant(field[0]) + field[1..]).GetString().ShouldBe(expected[field]);
        problem.GetProperty("instance").GetString().ShouldBe(_path);
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        var raw = problem.GetRawText();
        foreach (var privateText in new[] { "INVOICE_FRAMEWORK_PRIVATE_MARKER", "INVOICE_INTERNAL_FAILURE_DO_NOT_EXPOSE", "JsonException", "BadHttpRequestException", "NullReferenceException", "InvalidOperationException", "BytePositionInLine", "stackTrace" }) raw.ShouldNotContain(privateText);
        context.RecordEvidence("Framework verified complete problem", new { status, path = _path, expected, allowedMethods = allowed, actual = problem.Clone() });
    }

    [Then("the framework rejection and all complete supporting reads preserve every collection")]
    public async Task ThenUnchanged()
    {
        _before.ShouldNotBeNull();
        _after.ShouldNotBeNull();
        EqualState(_before, fixture.FixtureState);
        EqualState(_after, _before);
        var actual = await context.Factory.GetCheckoutDocumentsAsync();
        Attach("Framework database after problem verification", actual);
        EqualState(actual, _before);
        context.Factory.CatalogRequestCount.ShouldBe(0);
        await fixture.VerifyFixtureReadsAsync();
        context.RecordEvidence("Framework unchanged populated state verified", new { path = _path, collectionCounts = actual.ToDictionary(pair => pair.Key, pair => pair.Value.Length), allFieldsAndOrderedLinesUnchanged = true, catalogRequests = 0 });
    }

    private static void EqualState(Dictionary<string, BsonDocument[]> actual, Dictionary<string, BsonDocument[]> expected)
    {
        actual.Keys.Order().ShouldBe(expected.Keys.Order());
        foreach (var key in expected.Keys) actual[key].ShouldBe(expected[key]);
    }
    private void Attach(string name, Dictionary<string, BsonDocument[]> value) => context.RecordEvidence(name, value.ToDictionary(pair => pair.Key, pair => JsonSerializer.Deserialize<JsonElement>(new BsonArray(pair.Value).ToJson())));
    private static object Rows(Table table) => table.Rows.Select(row => row.ToDictionary(pair => pair.Key, pair => pair.Value)).ToArray();
    private static int Integer(string value) => int.Parse(value, CultureInfo.InvariantCulture);
    public void Dispose() => _routingClient?.Dispose();
}
