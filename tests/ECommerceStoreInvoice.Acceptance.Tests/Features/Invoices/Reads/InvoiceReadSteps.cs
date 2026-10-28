using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using MongoDB.Bson;
using Reqnroll;
using Shouldly;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Invoices.Reads;

[Binding]
public sealed class InvoiceReadSteps(ScenarioApiContext context)
{
    private readonly Dictionary<string, Guid> _clients = new(StringComparer.Ordinal) { ["A"] = Guid.NewGuid(), ["B"] = Guid.NewGuid() };
    private readonly Dictionary<string, Guid> _orders = new(StringComparer.Ordinal) { ["target"] = Guid.NewGuid(), ["other"] = Guid.NewGuid() };
    private readonly Dictionary<string, Guid> _versions = new(StringComparer.Ordinal);
    private Guid _invoiceId = Guid.NewGuid();
    private readonly Guid _otherId = Guid.NewGuid();
    private Dictionary<string, BsonDocument[]> _known = new(StringComparer.Ordinal);
    private Dictionary<string, BsonDocument[]>? _before;
    private InvoiceReadArtifact[] _files = [];
    private InvoiceReadArtifact[] _beforeFiles = [];
    private Dictionary<string, string> _selected = new(StringComparer.Ordinal);
    private Dictionary<string, Dictionary<string, string>> _problems = new(StringComparer.Ordinal);
    private string _state = string.Empty;
    private string _route = string.Empty;
    private string _path = string.Empty;
    private string _readerFault = string.Empty;
    private string _accept = string.Empty;
    private int _readOffset;
    private int _status;
    private Dictionary<string, string>? _responseExpectations;
    private InvoiceReadStorage Storage => context.Factory.ReadStorage ?? throw new InvalidOperationException("Invoice read storage is required.");
    private InvoiceReadProbe Probe => context.Factory.ReadProbe ?? throw new InvalidOperationException("Invoice read observer is required.");

    [Given("these complete client versions support invoice reading")]
    public void GivenVersions(Table table)
    {
        context.RecordEvidence("Invoice read client fixture table", Rows(table));
        _known.Add("clientDataVersions", table.Rows.Select(row =>
        {
            var id = Guid.NewGuid();
            _versions.Add(row["Version"], id);
            var document = new BsonDocument { ["_id"] = Binary(id), ["ClientId"] = Binary(_clients[row["Client"]]) };
            foreach (var field in new[] { "ClientName", "PostalCode", "City", "Street", "BuildingNumber", "ApartmentNumber", "PhoneNumber", "PhonePrefix", "AddressEmail" }) document[field] = row[field];
            document["CreatedAt"] = Date(row["CreatedAt"]);
            return document;
        }).ToArray());
    }

    [Given("these exact paid order lines and populated carts support invoice reading")]
    public void GivenAncillary(Table table)
    {
        context.RecordEvidence("Invoice read order product and cart fixture table", Rows(table));
        var orders = new List<BsonDocument>();
        var products = new List<BsonDocument>();
        var carts = new List<BsonDocument>();
        foreach (var row in table.Rows)
        {
            var productId = Guid.Parse(row["ProductId"]);
            var versionId = Guid.NewGuid();
            row["Status"].ShouldBe("Paid");
            row["DeactivatedAt"].ShouldBe("<null>");
            orders.Add(new BsonDocument
            {
                ["_id"] = Binary(_orders[row["Order"]]),
                ["ClientId"] = Binary(_clients[row["Client"]]),
                ["Lines"] = new BsonArray { new BsonDocument { ["ProductVersionId"] = Binary(versionId), ["Quantity"] = Integer(row["Quantity"]) } },
                ["CreatedAt"] = Date(row["OrderCreatedAt"]),
                ["UpdatedAt"] = Date(row["OrderUpdatedAt"]),
                ["Status"] = 0
            });
            products.Add(new BsonDocument
            {
                ["_id"] = Binary(versionId),
                ["ProductId"] = Binary(productId),
                ["IsActive"] = bool.Parse(row["IsActive"]),
                ["CreatedAt"] = Date(row["OrderCreatedAt"]),
                ["DeactivatedAt"] = BsonNull.Value,
                ["Name"] = row["ProductName"],
                ["Brand"] = row["Brand"],
                ["PriceAmount"] = decimal.Parse(row["PriceAmount"], CultureInfo.InvariantCulture),
                ["PriceCurrency"] = row["Currency"]
            });
            carts.Add(new BsonDocument
            {
                ["_id"] = Binary(Guid.NewGuid()),
                ["ClientId"] = Binary(_clients[row["Client"]]),
                ["CreatedAt"] = Date(row["CartCreatedAt"]),
                ["UpdatedAt"] = Date(row["CartUpdatedAt"]),
                ["Lines"] = new BsonArray { new BsonDocument { ["ProductId"] = Binary(productId), ["Quantity"] = Integer(row["CartQuantity"]) } }
            });
        }
        _known.Add("orders", orders.ToArray());
        _known.Add("productVersions", products.ToArray());
        _known.Add("shoppingCarts", carts.ToArray());
    }

    [Given("invoice read state {string} selects this complete record and artifact contract")]
    public async Task GivenSelectedState(string state, Table table)
    {
        _state = state;
        context.RecordEvidence("Invoice read state and artifact contract table", Rows(table));
        _selected = table.Rows.Single(row => row["State"] == state).ToDictionary(pair => pair.Key, pair => pair.Value);
        var otherUrl = await Storage.PutAsync("other", _otherId, Guid.NewGuid(), InvoiceReadStorage.BinaryPdf("Other client invoice"), false);
        var other = InvoiceDocument(_otherId, "other", "b-old", otherUrl, Date(_selected["OtherCreatedAt"]), "Completed");
        var documents = new List<BsonDocument> { other };
        var generated = state == "real-created";
        if (!generated)
        {
            var absent = _selected["Document"] == "absent";
            var missingFile = _selected["Pdf"] == "absent";
            var targetUrl = await Storage.PutAsync("target", _invoiceId, Guid.NewGuid(), InvoiceReadStorage.BinaryPdf("Target invoice binary fixture"), missingFile);
            if (!absent) documents.Add(InvoiceDocument(_invoiceId, "target", _selected["ClientVersion"], targetUrl, Date(_selected["CreatedAt"]), _selected["GenerationStatus"]));
        }
        _known.Add("invoices", documents.ToArray());
        _known = Sorted(_known);
        Attach("Invoice read expected fixture database", _known);
        await context.Factory.SeedOrderReadDocumentsAsync(_known);
        var actual = await context.Factory.GetCheckoutDocumentsAsync();
        Attach("Invoice read actual fixture database", actual);
        EqualState(actual, _known);
        if (generated)
        {
            _selected["Document"].ShouldBe("real-created");
            _selected["Pdf"].ShouldBe("real-created");
            _selected["ClientVersion"].ShouldBe("a-latest");
            var started = DateTime.UtcNow;
            using var response = await context.HttpClient.PostAsync($"/invoices/{_clients["A"]}/{_orders["target"]}", null);
            var ended = DateTime.UtcNow;
            var invoice = await ReadMetadata(response);
            invoice.Id.ShouldNotBe(Guid.Empty);
            invoice.Id.ShouldNotBe(_otherId);
            invoice.OrderId.ShouldBe(_orders["target"]);
            invoice.ClietDataVersionId.ShouldBe(_versions["a-latest"]);
            invoice.CreatedAt.ShouldBeGreaterThanOrEqualTo(Milliseconds(started));
            invoice.CreatedAt.ShouldBeLessThanOrEqualTo(ended);
            var uri = new Uri(invoice.StorageUrl);
            uri.IsFile.ShouldBeTrue();
            Regex.IsMatch(Path.GetFileName(uri.LocalPath), $"^{invoice.Id:N}-[a-f0-9]{{32}}\\.pdf$").ShouldBeTrue();
            _invoiceId = invoice.Id;
            Storage.TrackGeneratedFile("target", invoice.StorageUrl);
            var expected = Clone(_known);
            expected["invoices"] = expected["invoices"].Append(InvoiceDocument(invoice.Id, "target", "a-latest", invoice.StorageUrl, invoice.CreatedAt, "Completed")).OrderBy(Id).ToArray();
            actual = await context.Factory.GetCheckoutDocumentsAsync();
            Attach("Invoice read real setup expected database", expected);
            Attach("Invoice read real setup actual database", actual);
            EqualState(actual, expected);
            _known = actual;
            context.RecordEvidence("Verified real invoice read setup creation", new { path = $"/invoices/{_clients["A"]}/{_orders["target"]}", invoice, startedAt = started, completedAt = ended });
        }
        _files = await Storage.ReadStateAsync();
        context.RecordEvidence("Invoice read expected artifact payloads", Storage.ExpectedEvidence());
        context.RecordEvidence("Invoice read initial actual storage inventory", _files);
        var target = _files.Single(file => file.Key == "target");
        target.Exists.ShouldBe(_selected["Pdf"] is "present" or "real-created");
        foreach (var file in _files.Where(file => file.Exists))
        {
            file.Length.ShouldBe(Storage.ExpectedBytes(file.Key).LongLength);
            file.Sha256.ShouldBe(InvoiceReadStorage.Hash(Storage.ExpectedBytes(file.Key)));
            if (Storage.IsBlob) file.ContentType.ShouldBe("application/pdf");
            new Uri(file.StorageUrl).Query.ShouldBe(string.Empty);
        }
        _known.Values.ShouldAllBe(items => items.Length > 0);
        context.RecordEvidence("Invoice read fixture identities", new { clients = _clients, orders = _orders, clientVersions = _versions, invoiceId = _invoiceId, otherInvoiceId = _otherId, storageKind = Storage.IsBlob ? "private-azurite" : "local", storageAuthenticationFailure = Storage.AccessDenied });
        context.Factory.CatalogRequestCount.ShouldBe(0);
    }

    [Given("invoice read problems have these complete signatures")]
    public void GivenProblems(Table table)
    {
        context.RecordEvidence("Invoice read complete problem signatures table", Rows(table));
        _problems = table.Rows.ToDictionary(row => row["Route"] + "/" + row["Cause"], row => row.ToDictionary(pair => pair.Key, pair => pair.Value), StringComparer.Ordinal);
    }

    [When("I send this invoice read request")]
    public async Task WhenRequest(Table table)
    {
        var values = Values(table);
        context.RecordEvidence("Invoice read request table", values);
        values["Method"].ShouldBe("GET");
        _route = values["Route"];
        values["Endpoint"].ShouldBe(_route switch
        {
            "by-id" => "/invoices/{invoiceId}",
            "by-order" => "/invoices/by-order/{orderId}",
            "pdf" => "/invoices/{invoiceId}/pdf",
            _ => throw new InvalidOperationException("Unknown explicit invoice read route.")
        });
        _readerFault = values["ReaderFault"];
        (_readerFault == "blob-authentication").ShouldBe(Storage.AccessDenied);
        _accept = values["Accept"];
        var invalid = _state == "empty";
        var invoiceId = invalid ? Guid.Empty : _invoiceId;
        var orderId = invalid ? Guid.Empty : _orders["target"];
        _path = _route switch
        {
            "by-id" => $"/invoices/{invoiceId}",
            "by-order" => $"/invoices/by-order/{orderId}",
            "pdf" => $"/invoices/{invoiceId}/pdf",
            _ => throw new InvalidOperationException("Unknown explicit invoice read route.")
        };
        _before = await context.Factory.GetCheckoutDocumentsAsync();
        _beforeFiles = await Storage.ReadStateAsync();
        Attach("Invoice read database before primary request", _before);
        context.RecordEvidence("Invoice read storage before primary request", _beforeFiles);
        EqualState(_before, _known);
        _beforeFiles.ShouldBe(_files);
        _readOffset = Probe.Reads.Count;
        context.RecordEvidence("Resolved invoice read request", new { method = "GET", path = _path, route = _route, state = _state, readerFault = _readerFault, accept = _accept });
        context.Response?.Dispose();
        context.Response = await Send(_path, _accept);
    }

    [Then("the invoice read returns this complete outcome")]
    public async Task ThenOutcome(Table table)
    {
        _responseExpectations = Values(table);
        context.RecordEvidence("Invoice read complete outcome expectations", _responseExpectations);
        _status = Integer(_responseExpectations["StatusCode"]);
        context.Response.ShouldNotBeNull();
        await AssertOutcome(context.Response, _path, _route, _status, _responseExpectations);
        VerifyStorageReads(Probe.Reads.Skip(_readOffset).ToArray(), 1);
    }

    [Then("invoice reading preserves every populated document and storage artifact")]
    public async Task ThenUnchanged(Table table)
    {
        var values = Values(table);
        context.RecordEvidence("Invoice read persistence expectations", values);
        values["Collections"].ShouldBe("all-five-fields-unchanged");
        values["Artifacts"].ShouldBe("bytes-and-metadata-unchanged");
        values["CatalogRequests"].ShouldBe("0");
        var after = await context.Factory.GetCheckoutDocumentsAsync();
        var files = await Storage.ReadStateAsync();
        Attach("Invoice read database after primary request", after);
        context.RecordEvidence("Invoice read storage after primary request", files);
        _before.ShouldNotBeNull();
        EqualState(after, _before);
        files.ShouldBe(_beforeFiles);
        context.Factory.CatalogRequestCount.ShouldBe(0);
        context.RecordEvidence("Invoice read primary unchanged state verified", new { allFiveCollectionsUnchanged = true, allArtifactsAndMetadataUnchanged = true, catalogRequests = 0, storageReads = Probe.Reads.Count - _readOffset });
    }

    [Then("both invoice metadata routes and a repeated primary read preserve the same state")]
    public async Task ThenSupporting(Table table)
    {
        var values = Values(table);
        context.RecordEvidence("Invoice read supporting expectations", values);
        values["MetadataRoutes"].ShouldBe("both-complete-and-hidden");
        values["Repeat"].ShouldBe("same-complete-outcome");
        values["GeneratedSetupPdf"].ShouldBe("exact-real-rendered-download");
        var before = await context.Factory.GetCheckoutDocumentsAsync();
        var filesBefore = await Storage.ReadStateAsync();
        Attach("Invoice read database before supporting requests", before);
        context.RecordEvidence("Invoice read storage before supporting requests", filesBefore);
        EqualState(before, _known);
        filesBefore.ShouldBe(_files);
        foreach (var stored in before["invoices"])
        {
            var visible = !stored.Contains("GenerationStatus") || stored["GenerationStatus"].AsString == "Completed";
            foreach (var (path, route) in new[] { ($"/invoices/{Id(stored)}", "by-id"), ($"/invoices/by-order/{GuidField(stored, "OrderId")}", "by-order") })
            {
                using var response = await Send(path, "application/json");
                if (visible) EqualMetadata(await ReadMetadata(response), stored);
                else await AssertProblem(response, path, route, 404, false);
            }
        }
        if (Target(before) is null)
        {
            foreach (var (path, route) in new[] { ($"/invoices/{_invoiceId}", "by-id"), ($"/invoices/by-order/{_orders["target"]}", "by-order") })
            {
                using var response = await Send(path, "application/json");
                await AssertProblem(response, path, route, 404, false);
            }
        }
        _responseExpectations.ShouldNotBeNull();
        using var repeated = await Send(_path, _accept);
        await AssertOutcome(repeated, _path, _route, _status, _responseExpectations);
        var generatedDownloads = 0;
        if (_state == "real-created")
        {
            var path = $"/invoices/{_invoiceId}/pdf";
            using var pdf = await Send(path, "application/pdf");
            await AssertOutcome(pdf, path, "pdf", 200, _responseExpectations);
            generatedDownloads = 1;
        }
        VerifyStorageReads(Probe.Reads.Skip(_readOffset).ToArray(), 2, generatedDownloads);
        var after = await context.Factory.GetCheckoutDocumentsAsync();
        var filesAfter = await Storage.ReadStateAsync();
        Attach("Invoice read database after supporting requests", after);
        context.RecordEvidence("Invoice read storage after supporting requests", filesAfter);
        EqualState(after, before);
        filesAfter.ShouldBe(filesBefore);
        context.Factory.CatalogRequestCount.ShouldBe(0);
        context.RecordEvidence("Invoice read supporting requests verified", new { bothMetadataRoutesVerified = true, repeatedOutcomeVerified = true, allFiveCollectionsUnchanged = true, allArtifactsAndMetadataUnchanged = true, catalogRequests = 0 });
    }

    private async Task AssertOutcome(HttpResponseMessage response, string path, string route, int status, IReadOnlyDictionary<string, string> expected)
    {
        ((int)response.StatusCode).ShouldBe(status);
        if (status != 200)
        {
            await AssertProblem(response, path, route, status, route == "pdf" && _selected["Pdf"] == "absent" && _selected["Document"] == "present");
            return;
        }
        var stored = Target(_known);
        stored.ShouldNotBeNull();
        if (route != "pdf")
        {
            expected["Metadata"].ShouldBe("all-five-fields-exact");
            expected["ClientVersion"].ShouldBe("declared-stored-reference");
            var actual = await ReadMetadata(response);
            EqualMetadata(actual, stored);
            actual.Id.ShouldBe(_invoiceId);
            actual.OrderId.ShouldBe(_orders["target"]);
            actual.ClietDataVersionId.ShouldBe(_versions[_selected["ClientVersion"]]);
            context.RecordEvidence("Verified complete invoice read metadata", new { path, actual });
        }
        else
        {
            expected["PdfBytes"].ShouldBe("exact-declared-artifact");
            expected["ContentType"].ShouldBe("application/pdf");
            expected["ContentDisposition"].ShouldBe("attachment; invoice-{invoiceId}.pdf");
            expected["CacheControl"].ShouldBe("private, no-store");
            expected["ContentLength"].ShouldBe("exact-if-present");
            response.Content.Headers.ContentType?.MediaType.ShouldBe("application/pdf");
            response.Content.Headers.ContentType?.CharSet.ShouldBeNull();
            var disposition = response.Content.Headers.ContentDisposition;
            disposition.ShouldNotBeNull();
            disposition.DispositionType.ShouldBe("attachment");
            disposition.FileNameStar.ShouldBe($"invoice-{_invoiceId:D}.pdf");
            disposition.FileName?.Trim('"').ShouldBe($"invoice-{_invoiceId:D}.pdf");
            response.Headers.GetValues("Cache-Control").Single().ShouldBe("private, no-store");
            response.Headers.CacheControl?.Private.ShouldBeTrue();
            response.Headers.CacheControl?.NoStore.ShouldBeTrue();
            var bytes = await response.Content.ReadAsByteArrayAsync();
            var wanted = Storage.ExpectedBytes("target");
            bytes.SequenceEqual(wanted).ShouldBeTrue();
            bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8).ShouldBeTrue();
            if (response.Content.Headers.ContentLength is { } length) length.ShouldBe(wanted.LongLength);
            context.RecordEvidence("Verified binary invoice download", new
            {
                path,
                byteLength = bytes.Length,
                sha256 = InvoiceReadStorage.Hash(bytes),
                expectedSha256 = InvoiceReadStorage.Hash(wanted),
                contentType = response.Content.Headers.ContentType.ToString(),
                contentDisposition = disposition.ToString(),
                cacheControl = response.Headers.GetValues("Cache-Control").Single(),
                contentLength = response.Content.Headers.ContentLength,
                exactBytes = true
            });
        }
    }

    private void VerifyStorageReads(InvoiceStorageRead[] reads, int repetitions, int generatedDownloads = 0)
    {
        var completed = _selected["Document"] != "absent" && _selected["GenerationStatus"] is "Completed" or "absent";
        var expectedCalls = (_route == "pdf" && completed ? repetitions : 0) + generatedDownloads;
        reads.Length.ShouldBe(expectedCalls);
        foreach (var read in reads)
        {
            read.StorageUrl.ShouldBe(Target(_known)!["StorageUrl"].AsString);
            if (_readerFault == "simulated-reader")
            {
                read.Found.ShouldBeNull();
                read.ExceptionType.ShouldBe(typeof(IOException).FullName);
                read.StorageStatus.ShouldBeNull();
            }
            else if (_readerFault == "blob-authentication")
            {
                Storage.IsBlob.ShouldBeTrue();
                Storage.AccessDenied.ShouldBeTrue();
                read.Found.ShouldBeNull();
                read.ExceptionType.ShouldBe("Azure.RequestFailedException");
                read.StorageStatus.ShouldBe(403);
            }
            else
            {
                _readerFault.ShouldBe("none");
                read.ExceptionType.ShouldBeNull();
                read.StorageStatus.ShouldBeNull();
                read.Found.ShouldBe(_selected["Pdf"] is "present" or "real-created");
            }
        }
        context.RecordEvidence("Verified invoice storage read boundaries", new { route = _route, state = _state, readerFault = _readerFault, repetitions, generatedDownloads, expectedCalls, actualCalls = reads.Length, reads });
    }

    private async Task AssertProblem(HttpResponseMessage response, string path, string route, int status, bool missingPdf)
    {
        var cause = status switch { 400 => "InvalidId", 500 => "StorageFailure", 404 when missingPdf => "MissingPdf", 404 => "HiddenOrMissing", _ => throw new InvalidOperationException("Unknown invoice read problem.") };
        var expected = _problems[route + "/" + cause];
        context.RecordEvidence("Selected complete invoice read problem expectation", new { path, route, cause, expected });
        ((int)response.StatusCode).ShouldBe(Integer(expected["StatusCode"]));
        response.Content.Headers.ContentType?.MediaType.ShouldBe(expected["ContentType"]);
        response.Content.Headers.ContentDisposition.ShouldBeNull();
        response.Headers.CacheControl.ShouldBeNull();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var value = json.RootElement;
        context.RecordEvidence("Complete actual invoice read problem", new { path, problem = value.Clone() });
        var segments = path.Split('/');
        var id = route == "pdf" ? segments[^2] : segments[^1];
        value.GetProperty("status").GetInt32().ShouldBe(status);
        value.GetProperty("title").GetString().ShouldBe(expected["Title"]);
        value.GetProperty("type").GetString().ShouldBe(expected["Type"]);
        value.GetProperty("detail").GetString().ShouldBe(expected["Detail"].Replace("{id}", id));
        expected["Instance"].ShouldBe("request-path");
        expected["TraceId"].ShouldBe("generated");
        value.GetProperty("instance").GetString().ShouldBe(path);
        value.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        if (expected["ErrorsCount"] == "absent") Names(value, "type", "title", "status", "detail", "instance", "traceId");
        else
        {
            expected["ErrorsCount"].ShouldBe("1");
            Names(value, "type", "title", "status", "detail", "instance", "traceId", "errors");
            var errors = value.GetProperty("errors");
            errors.GetArrayLength().ShouldBe(1);
            Names(errors[0], "name", "entity", "message");
            errors[0].GetProperty("name").GetString().ShouldBe(expected["ErrorName"]);
            errors[0].GetProperty("entity").GetString().ShouldBe(expected["ErrorEntity"]);
            errors[0].GetProperty("message").GetString().ShouldBe(expected["ErrorMessage"]);
        }
    }

    private async Task<InvoiceResponseDto> ReadMetadata(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        response.Content.Headers.ContentDisposition.ShouldBeNull();
        response.Headers.CacheControl.ShouldBeNull();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Names(json.RootElement, "id", "orderId", "clietDataVersionId", "storageUrl", "createdAt");
        foreach (var property in json.RootElement.EnumerateObject()) property.Value.ValueKind.ShouldBe(JsonValueKind.String);
        var invoice = JsonSerializer.Deserialize<InvoiceResponseDto>(json.RootElement, context.JsonOptions);
        invoice.ShouldNotBeNull();
        invoice.CreatedAt.Kind.ShouldBe(DateTimeKind.Utc);
        return invoice;
    }
    private static void EqualMetadata(InvoiceResponseDto actual, BsonDocument stored)
    {
        actual.Id.ShouldBe(Id(stored));
        actual.OrderId.ShouldBe(GuidField(stored, "OrderId"));
        actual.ClietDataVersionId.ShouldBe(GuidField(stored, "ClientDataVersionId"));
        actual.StorageUrl.ShouldBe(stored["StorageUrl"].AsString);
        Milliseconds(actual.CreatedAt).ShouldBe(stored["CreatedAt"].ToUniversalTime());
    }
    private Task<HttpResponseMessage> Send(string path, string accept)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));
        return context.HttpClient.SendAsync(request);
    }
    private BsonDocument InvoiceDocument(Guid id, string order, string version, string url, DateTime created, string state)
    {
        var result = new BsonDocument
        {
            ["_id"] = Binary(id),
            ["OrderId"] = Binary(_orders[order]),
            ["ClientDataVersionId"] = Binary(_versions[version]),
            ["StorageUrl"] = url,
            ["CreatedAt"] = created
        };
        if (state != "absent")
        {
            result["GenerationStatus"] = state;
            result["GenerationAttemptId"] = state == "Completed" ? BsonNull.Value : new BsonString("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            result["GenerationLeaseUntil"] = state == "Generating" ? new BsonDateTime(DateTime.UtcNow.AddMinutes(5)) : BsonNull.Value;
        }
        return result;
    }
    private BsonDocument? Target(Dictionary<string, BsonDocument[]> state) => state["invoices"].SingleOrDefault(document => Id(document) == _invoiceId);
    private static void Names(JsonElement value, params string[] names)
    {
        value.ValueKind.ShouldBe(JsonValueKind.Object);
        value.EnumerateObject().Select(property => property.Name).Order().ShouldBe(names.Order());
    }
    private static Dictionary<string, BsonDocument[]> Clone(Dictionary<string, BsonDocument[]> value) => value.ToDictionary(pair => pair.Key, pair => pair.Value.Select(document => document.DeepClone().AsBsonDocument).ToArray());
    private static Dictionary<string, BsonDocument[]> Sorted(Dictionary<string, BsonDocument[]> value) => value.ToDictionary(pair => pair.Key, pair => pair.Value.OrderBy(Id).ToArray());
    private static void EqualState(Dictionary<string, BsonDocument[]> actual, Dictionary<string, BsonDocument[]> expected)
    {
        actual.Keys.Order().ShouldBe(expected.Keys.Order());
        foreach (var name in expected.Keys) actual[name].ShouldBe(expected[name]);
    }
    private void Attach(string name, Dictionary<string, BsonDocument[]> state) => context.RecordEvidence(name, InvoiceGenerationProbe.JsonSnapshot(state));
    private static Guid Id(BsonDocument value) => GuidField(value, "_id");
    private static Guid GuidField(BsonDocument value, string name) => value[name].AsBsonBinaryData.ToGuid();
    private static BsonBinaryData Binary(Guid value) => new(value, GuidRepresentation.Standard);
    private static DateTime Date(string value) => DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    private static DateTime Milliseconds(DateTime value) => new(value.Ticks - value.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
    private static int Integer(string value) => int.Parse(value, CultureInfo.InvariantCulture);
    private static Dictionary<string, string> Values(Table table) => table.Rows.ToDictionary(row => row["Field"], row => row["Value"], StringComparer.Ordinal);
    private static object Rows(Table table) => table.Rows.Select(row => row.ToDictionary(pair => pair.Key, pair => pair.Value)).ToArray();
}
