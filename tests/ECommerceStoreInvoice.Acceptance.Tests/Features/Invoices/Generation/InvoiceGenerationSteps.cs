using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using MongoDB.Bson;
using Reqnroll;
using Shouldly;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Invoices.Generation;

[Binding]
public sealed class InvoiceGenerationSteps(ScenarioApiContext context)
{
    private readonly Dictionary<string, Guid> _clients = new(StringComparer.Ordinal)
    {
        ["A"] = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        ["B"] = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        ["empty"] = Guid.Empty
    };
    private readonly Dictionary<string, Guid> _orders = new(StringComparer.Ordinal) { ["target"] = Guid.NewGuid(), ["other"] = Guid.NewGuid(), ["missing"] = Guid.NewGuid(), ["empty"] = Guid.Empty };
    private readonly Dictionary<string, Guid> _versions = new(StringComparer.Ordinal);
    private Dictionary<string, BsonDocument[]> _known = new(StringComparer.Ordinal);
    private Dictionary<string, BsonDocument[]>? _before;
    private readonly List<BsonDocument> _snapshots = [];
    private readonly Dictionary<string, BsonDocument> _orderDocuments = new(StringComparer.Ordinal);
    private InvoiceResponseDto? _completedInvoice;
    private string _client = string.Empty;
    private string _order = string.Empty;
    private string _path = string.Empty;
    private DateTime _started;
    private DateTime _ended;
    private int _repositoryOffset;
    private int _pdfOffset;
    private int _deleteOffset;
    private InvoiceGenerationProbe Probe => context.Factory.InvoiceProbe ?? throw new InvalidOperationException("Invoice generation probe is required.");

    [Given("these full invoice client versions exist in this insertion order")]
    public void GivenClients(Table table)
    {
        context.RecordEvidence("Invoice client version fixture table", Rows(table));
        _known.Add("clientDataVersions", table.Rows.Select(row =>
        {
            var id = Guid.NewGuid();
            _versions.Add(row["Version"], id);
            return ClientDocument(row.ToDictionary(pair => pair.Key, pair => pair.Value), id, _clients[row["Client"]], Date(row["CreatedAt"]));
        }).ToArray());
    }

    [Given("these full invoice order profiles exist")]
    public void GivenOrders(Table table)
    {
        context.RecordEvidence("Invoice order fixture table", Rows(table));
        foreach (var row in table.Rows)
            _orderDocuments.Add(row["Order"], new BsonDocument
            {
                ["_id"] = Binary(_orders[row["Order"]]),
                ["ClientId"] = Binary(_clients[row["Client"]]),
                ["Lines"] = new BsonArray(),
                ["CreatedAt"] = Date(row["CreatedAt"]),
                ["UpdatedAt"] = row["UpdatedAt"] == "<null>" ? BsonNull.Value : new BsonDateTime(Date(row["UpdatedAt"])),
                ["Status"] = (int)Enum.Parse<OrderStatus>(row["Status"])
            });
    }

    [Given("these exact saved product lines belong to the invoice orders")]
    public void GivenLines(Table table)
    {
        context.RecordEvidence("Invoice saved product fixture table", Rows(table));
        foreach (var row in table.Rows)
        {
            var id = Guid.NewGuid();
            _snapshots.Add(new BsonDocument
            {
                ["_id"] = Binary(id),
                ["ProductId"] = Binary(Guid.Parse(row["ProductId"])),
                ["IsActive"] = bool.Parse(row["IsActive"]),
                ["CreatedAt"] = Date(row["CreatedAt"]),
                ["DeactivatedAt"] = row["DeactivatedAt"] == "<null>" ? BsonNull.Value : new BsonDateTime(Date(row["DeactivatedAt"])),
                ["Name"] = row["Name"],
                ["Brand"] = row["Brand"],
                ["PriceAmount"] = decimal.Parse(row["PriceAmount"], CultureInfo.InvariantCulture),
                ["PriceCurrency"] = row["PriceCurrency"]
            });
            _orderDocuments[row["Order"]]["Lines"].AsBsonArray.Add(new BsonDocument { ["ProductVersionId"] = Binary(id), ["Quantity"] = Integer(row["Quantity"]) });
        }
        _known.Add("orders", _orderDocuments.Values.ToArray());
        _known.Add("productVersions", _snapshots.ToArray());
    }

    [Given("both invoice clients have these populated carts")]
    public void GivenCarts(Table table)
    {
        context.RecordEvidence("Invoice cart fixture table", Rows(table));
        _known.Add("shoppingCarts", table.Rows.Select(row => new BsonDocument
        {
            ["_id"] = Binary(Guid.NewGuid()),
            ["ClientId"] = Binary(_clients[row["Client"]]),
            ["CreatedAt"] = Date(row["CreatedAt"]),
            ["UpdatedAt"] = Date(row["UpdatedAt"]),
            ["Lines"] = new BsonArray { new BsonDocument { ["ProductId"] = Binary(Guid.Parse(row["ProductId"])), ["Quantity"] = Integer(row["Quantity"]) } }
        }).ToArray());
    }

    [Given("these complete historical invoice records exist")]
    public async Task GivenInvoices(Table table)
    {
        context.RecordEvidence("Invoice reservation fixture table", Rows(table));
        _known.Add("invoices", table.Rows.Select(row => new BsonDocument
        {
            ["_id"] = Binary(Guid.NewGuid()),
            ["OrderId"] = Binary(_orders[row["Order"]]),
            ["ClientDataVersionId"] = Binary(_versions[row["ClientVersion"]]),
            ["StorageUrl"] = row["StorageUrl"] == "<empty>" ? string.Empty : row["StorageUrl"],
            ["CreatedAt"] = Date(row["CreatedAt"]),
            ["GenerationStatus"] = row["GenerationStatus"],
            ["GenerationAttemptId"] = row["GenerationAttemptId"] == "<null>" ? BsonNull.Value : new BsonString(row["GenerationAttemptId"]),
            ["GenerationLeaseUntil"] = row["GenerationLeaseUntil"] == "<null>" ? BsonNull.Value : new BsonDateTime(row["GenerationLeaseUntil"] == "future:+5-minutes" ? Milliseconds(DateTime.UtcNow.AddMinutes(5)) : Date(row["GenerationLeaseUntil"]))
        }).ToArray());
        await context.Factory.SeedOrderReadDocumentsAsync(_known);
        _known = Sorted(_known);
        Attach("Invoice complete expected fixture database", _known);
        var actual = await context.Factory.GetCheckoutDocumentsAsync();
        Attach("Invoice complete actual fixture database", actual);
        EqualState(actual, _known);
        actual.Values.ShouldAllBe(documents => documents.Length > 0);
        context.RecordEvidence("Invoice fixture identities", new { clients = _clients, orders = _orders, clientVersions = _versions });
        context.Factory.CatalogRequestCount.ShouldBe(0);
    }

    [When("I send this invoice generation request")]
    public async Task WhenRequest(Table table)
    {
        var values = Values(table);
        context.RecordEvidence("Invoice generation request table", values);
        values["Method"].ShouldBe("POST");
        values["Endpoint"].ShouldBe("/invoices/{clientId}/{orderId}");
        values["HasBody"].ShouldBe("false");
        await BeforeRequest(values["Client"], values["Order"]);
        context.Response = await context.HttpClient.PostAsync(_path, null);
        _ended = DateTime.UtcNow;
        Window();
    }

    private async Task BeforeRequest(string client, string order)
    {
        _client = client;
        _order = order;
        _path = $"/invoices/{_clients[client]}/{_orders[order]}";
        _completedInvoice = null;
        _before = await context.Factory.GetCheckoutDocumentsAsync();
        Attach("Invoice database before primary request", _before);
        EqualState(_before, _known);
        _repositoryOffset = Probe.Repository.Count;
        _pdfOffset = Probe.Pdf.Count;
        _deleteOffset = Probe.Deletes.Count;
        context.Response?.Dispose();
        context.RecordEvidence("Resolved invoice generation request", new { method = "POST", path = _path, client, order, clientId = _clients[client], orderId = _orders[order] });
        _started = DateTime.UtcNow;
    }
    private void Window() => context.RecordEvidence("Invoice primary request window", new { path = _path, startedAt = _started, completedAt = _ended });

    [When("two invoice requests overlap while the winning reservation is held")]
    public async Task WhenConcurrent(Table table)
    {
        var values = Values(table);
        context.RecordEvidence("Invoice overlapping request expectations", values);
        values["Requests"].ShouldBe("2");
        values["HoldAt"].ShouldBe("after-real-claim-before-PDF");
        await BeforeRequest(values["Client"], values["Order"]);
        var winner = context.HttpClient.PostAsync(_path, null);
        try
        {
            await Probe.WaitForPdfAsync();
            var held = await context.Factory.GetCheckoutDocumentsAsync();
            Attach("Invoice held live reservation database", held);
            var claim = Probe.Repository.Last().Claim;
            claim.ShouldNotBeNull();
            Target(held).ShouldNotBeNull();
            Target(held)!["GenerationStatus"].AsString.ShouldBe("Generating");
            Target(held)!["GenerationAttemptId"].AsString.ShouldBe(claim.AttemptId.ToString("N"));
            await ReadInvoice(Target(held)!, false);
            var afterReads = await context.Factory.GetCheckoutDocumentsAsync();
            Attach("Invoice held reservation after hidden reads", afterReads);
            EqualState(afterReads, held);
            using var loser = await context.HttpClient.PostAsync(_path, null);
            await Problem(loser, 409, "Conflict.", "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8",
                $"Resource Invoice generation for order '{_orders[_order]}' is already in progress or completed. identified by id {_orders[_order]} already exists in db. Error in action Invoice.", _path);
            var afterConflict = await context.Factory.GetCheckoutDocumentsAsync();
            Attach("Invoice held reservation after competing conflict", afterConflict);
            EqualState(afterConflict, held);
            context.RecordEvidence("Verified deterministic invoice competition", new { winnerInvoiceId = claim.InvoiceId, winnerAttemptId = claim.AttemptId, loserStatus = 409, hiddenReads = 2, heldStateUnchanged = true });
        }
        finally { Probe.ResumePdf(); context.Response = await winner.WaitAsync(TimeSpan.FromSeconds(30)); }
        _ended = DateTime.UtcNow;
        Window();
    }

    [Then("invoice generation returns this complete metadata")]
    public async Task ThenMetadata(Table table)
    {
        var values = Values(table);
        context.RecordEvidence("Invoice complete metadata expectations", values);
        context.Response.ShouldNotBeNull();
        values["StatusCode"].ShouldBe("200");
        values["ContentType"].ShouldBe("application/json");
        var invoice = await ReadMetadata(context.Response);
        context.RecordEvidence("Complete actual generated invoice metadata", invoice);
        invoice.OrderId.ShouldBe(_orders[values["Order"]]);
        invoice.ClietDataVersionId.ShouldBe(_versions[values["ClientVersion"]]);
        values["Id"].ShouldBe("claimed-invoice");
        values["StorageUrl"].ShouldBe("current-attempt-local-PDF");
        values["CreatedAt"].ShouldBe("reservation-timestamp");
        var events = Probe.Repository.Skip(_repositoryOffset).ToArray();
        var claim = events.Single(item => item.Operation == "TryClaim" && item.Claim is not null).Claim!;
        invoice.Id.ShouldBe(claim.InvoiceId);
        var complete = events.Single(item => item.Operation == "Complete");
        var stored = Target(complete.After)!;
        EqualInvoice(invoice, stored);
        var pdf = Probe.Pdf.Single(item => item.AttemptId == claim.AttemptId);
        invoice.StorageUrl.ShouldBe(pdf.StorageUrl);
        var file = Probe.FileState(invoice.Id, claim.AttemptId);
        AssertPdf(file);
        file.ShouldBe(pdf.File);
        new Uri(invoice.StorageUrl).IsFile.ShouldBeTrue();
        new Uri(invoice.StorageUrl).LocalPath.ShouldBe(file.Path);
        context.RecordEvidence("Verified completed attempt file", file);
        _completedInvoice = invoice;
    }

    [Then("invoice generation returns this exact safe problem")]
    public async Task ThenProblem(Table table)
    {
        var values = Values(table);
        context.RecordEvidence("Invoice problem expectations", values);
        context.Response.ShouldNotBeNull();
        values["ContentType"].ShouldBe("application/problem+json");
        values["Instance"].ShouldBe("request-path");
        values["TraceId"].ShouldBe("generated");
        await Problem(context.Response, Integer(values["StatusCode"]), values["Title"], values["Type"],
            values["Detail"].Replace("{orderId}", _orders[_order].ToString()).Replace("{clientId}", _clients[_client].ToString()), _path,
            values["ErrorsCount"] == "1" ? new[] { values["ErrorName"], values["ErrorEntity"], values["ErrorMessage"] } : null);
    }

    [Then("invoice generation has exactly this database and attempt effect")]
    public async Task ThenState(Table table)
    {
        var values = Values(table);
        context.RecordEvidence("Invoice state and attempt expectations", values);
        _before.ShouldNotBeNull();
        var after = await context.Factory.GetCheckoutDocumentsAsync();
        Attach("Invoice database after primary request", after);
        var events = Probe.Repository.Skip(_repositoryOffset).ToArray();
        var pdfs = Probe.Pdf.Skip(_pdfOffset).ToArray();
        var deletes = Probe.Deletes.Skip(_deleteOffset).ToArray();
        events.Count(item => item.Operation == "TryClaim").ShouldBe(Integer(values["ClaimsAttempted"]));
        events.Count(item => item.Operation == "TryClaim" && item.Claim is not null).ShouldBe(Integer(values["ClaimsWon"]));
        events.Count(item => item.Operation == "Complete").ShouldBe(Integer(values["Completions"]));
        events.Count(item => item.Operation == "Complete" && item.ExceptionType is not null).ShouldBe(Integer(values["CompletionAcknowledgementsLost"]));
        events.Count(item => item.Operation == "Release").ShouldBe(Integer(values["Releases"]));
        pdfs.Length.ShouldBe(Integer(values["PdfAttempts"]));
        deletes.Length.ShouldBe(Integer(values["Deletes"]));
        var previous = _before;
        foreach (var item in events)
        {
            EqualState(item.Before, previous);
            VerifyRepositoryExchange(item);
            previous = item.After;
        }
        EqualState(after, previous);
        foreach (var pdf in pdfs) VerifyPdfInput(pdf);
        foreach (var deleted in deletes)
        {
            var failed = pdfs.Single(item => item.InvoiceId == deleted.InvoiceId && item.AttemptId == deleted.AttemptId);
            failed.ExceptionType.ShouldBe(typeof(IOException).FullName);
            deleted.Before.ShouldBe(failed.File);
            deleted.After.Exists.ShouldBeFalse();
            Probe.FileState(deleted.InvoiceId, deleted.AttemptId).ShouldBe(deleted.After);
        }
        if (values["Effect"] == "completed")
        {
            _completedInvoice.ShouldNotBeNull();
            Target(after)!["GenerationStatus"].AsString.ShouldBe("Completed");
            pdfs.ShouldHaveSingleItem();
            pdfs[0].ExceptionType.ShouldBeNull();
        }
        else if (values["Effect"] == "failed")
        {
            Target(after)!["GenerationStatus"].AsString.ShouldBe("Failed");
            pdfs.ShouldHaveSingleItem();
            (pdfs[0].File.Exists ? "written" : "not-written").ShouldBe(values["FailedPdf"]);
        }
        else
        {
            values["Effect"].ShouldBe("unchanged");
            EqualState(after, _before);
            pdfs.ShouldBeEmpty();
        }
        foreach (var earlier in Probe.Pdf.Where(item => item.ExceptionType is null))
            Probe.FileState(earlier.InvoiceId, earlier.AttemptId).ShouldBe(earlier.File);
        foreach (var deleted in Probe.Deletes)
            Probe.FileState(deleted.InvoiceId, deleted.AttemptId).Exists.ShouldBeFalse();
        after["invoices"].Length.ShouldBe(Integer(values["Invoices"]));
        values["OtherCollections"].ShouldBe("all-fields-unchanged");
        foreach (var name in after.Keys.Where(name => name != "invoices")) after[name].ShouldBe(_before[name]);
        context.Factory.CatalogRequestCount.ShouldBe(0);
        values["CatalogRequests"].ShouldBe("0");
        _known = after;
        context.RecordEvidence("Invoice primary persistence verified", new { effect = values["Effect"], invoices = after["invoices"].Length, repositoryExchanges = events.Length, pdfAttempts = pdfs.Length, deletes = deletes.Length, allOtherFieldsUnchanged = true, catalogRequests = 0 });
    }

    private void VerifyRepositoryExchange(InvoiceRepositoryObservation item)
    {
        var expected = Clone(item.Before);
        var original = Target(expected);
        if (item.Operation == "TryClaim" && item.Claim is null)
        {
            original.ShouldNotBeNull();
            original["GenerationStatus"].AsString.ShouldBe("Generating");
            original["GenerationLeaseUntil"].ToUniversalTime().ShouldBeGreaterThan(item.CompletedAt);
            EqualState(item.After, item.Before);
            return;
        }
        item.Claim.ShouldNotBeNull();
        var claim = item.Claim;
        claim.OrderId.ShouldBe(_orders[_order]);
        claim.AttemptId.ShouldNotBe(Guid.Empty);
        claim.AttemptId.ShouldBe(item.AttemptId);
        var actual = Target(item.After)!;
        actual.Names.Order().ShouldBe(new[] { "_id", "OrderId", "ClientDataVersionId", "StorageUrl", "CreatedAt", "GenerationStatus", "GenerationAttemptId", "GenerationLeaseUntil" }.Order());
        if (item.Operation == "TryClaim")
        {
            item.Reservation.ShouldNotBeNull();
            var latest = item.Before["clientDataVersions"].Where(document => GuidField(document, "ClientId") == _clients[_client]).MaxBy(document => document["CreatedAt"].ToUniversalTime())!;
            claim.ClientDataVersionId.ShouldBe(Id(latest));
            item.Reservation.ClientDataVersionId.ShouldBe(Id(latest));
            item.Reservation.OrderId.ShouldBe(claim.OrderId);
            item.Reservation.StorageUrl.ShouldBe(string.Empty);
            item.Reservation.CreatedAt.ShouldBeGreaterThanOrEqualTo(_started);
            item.Reservation.CreatedAt.ShouldBeLessThanOrEqualTo(item.CompletedAt);
            if (original is null)
            {
                claim.InvoiceId.ShouldBe(item.Reservation.Id);
                original = new BsonDocument
                {
                    ["_id"] = Binary(claim.InvoiceId),
                    ["OrderId"] = Binary(claim.OrderId),
                    ["ClientDataVersionId"] = Binary(claim.ClientDataVersionId),
                    ["StorageUrl"] = string.Empty,
                    ["CreatedAt"] = item.Reservation.CreatedAt,
                    ["GenerationStatus"] = "Generating",
                    ["GenerationAttemptId"] = claim.AttemptId.ToString("N"),
                    ["GenerationLeaseUntil"] = actual["GenerationLeaseUntil"].DeepClone()
                };
                expected["invoices"] = expected["invoices"].Append(original).OrderBy(Id).ToArray();
            }
            else
            {
                (original["GenerationStatus"].AsString == "Failed" || original["GenerationStatus"].AsString == "Generating" && original["GenerationLeaseUntil"].ToUniversalTime() <= item.StartedAt).ShouldBeTrue();
                original["GenerationAttemptId"].AsString.ShouldNotBe(claim.AttemptId.ToString("N"));
                original["ClientDataVersionId"] = Binary(claim.ClientDataVersionId);
                original["StorageUrl"] = string.Empty;
                original["GenerationStatus"] = "Generating";
                original["GenerationAttemptId"] = claim.AttemptId.ToString("N");
                original["GenerationLeaseUntil"] = actual["GenerationLeaseUntil"].DeepClone();
            }
            var lease = actual["GenerationLeaseUntil"].ToUniversalTime();
            lease.ShouldBeGreaterThanOrEqualTo(Milliseconds(item.StartedAt.AddMinutes(10)));
            lease.ShouldBeLessThanOrEqualTo(item.CompletedAt.AddMinutes(10));
        }
        else
        {
            original.ShouldNotBeNull();
            Id(original).ShouldBe(claim.InvoiceId);
            GuidField(original, "ClientDataVersionId").ShouldBe(claim.ClientDataVersionId);
            original["GenerationStatus"].AsString.ShouldBe("Generating");
            original["GenerationAttemptId"].AsString.ShouldBe(claim.AttemptId.ToString("N"));
            if (item.Operation == "Complete")
            {
                original["StorageUrl"] = item.StorageUrl;
                original["GenerationStatus"] = "Completed";
                original["GenerationAttemptId"] = BsonNull.Value;
            }
            else
            {
                item.Operation.ShouldBe("Release");
                original["GenerationStatus"] = "Failed";
            }
            original["GenerationLeaseUntil"] = BsonNull.Value;
        }
        EqualState(item.After, expected);
    }

    private void VerifyPdfInput(InvoicePdfObservation pdf)
    {
        var claim = Probe.Repository.Single(item => item.Operation == "TryClaim" && item.AttemptId == pdf.AttemptId).Claim!;
        pdf.InvoiceId.ShouldBe(claim.InvoiceId);
        var order = _known["orders"].Single(document => Id(document) == claim.OrderId);
        pdf.Order.Id.ShouldBe(Id(order));
        pdf.Order.ClientId.ShouldBe(GuidField(order, "ClientId"));
        pdf.Order.Status.ShouldBe(OrderStatus.Paid);
        pdf.Order.CreatedAt.ShouldBe(order["CreatedAt"].ToUniversalTime());
        pdf.Order.UpdatedAt.ShouldBe(order["UpdatedAt"].IsBsonNull ? null : order["UpdatedAt"].ToUniversalTime());
        pdf.Order.Lines.Count.ShouldBe(order["Lines"].AsBsonArray.Count);
        pdf.Products.Length.ShouldBe(pdf.Order.Lines.Count);
        foreach (var line in pdf.Order.Lines)
        {
            var savedLine = order["Lines"].AsBsonArray.Single(item => GuidField(item.AsBsonDocument, "ProductVersionId") == line.ProductVersionId);
            line.Quantity.ShouldBe(savedLine["Quantity"].AsInt32);
            var product = pdf.Products.Single(item => item.Id == line.ProductVersionId);
            var stored = _known["productVersions"].Single(item => Id(item) == line.ProductVersionId);
            product.ProductId.ShouldBe(GuidField(stored, "ProductId"));
            product.Name.ShouldBe(stored["Name"].AsString);
            product.Brand.ShouldBe(stored["Brand"].AsString);
            product.Price.Amount.ShouldBe(stored["PriceAmount"].AsDecimal);
            product.Price.Currency.ShouldBe(stored["PriceCurrency"].AsString);
            product.IsActive.ShouldBe(stored["IsActive"].AsBoolean);
            product.CreatedAt.ShouldBe(stored["CreatedAt"].ToUniversalTime());
            product.DeactivatedAt.ShouldBe(stored["DeactivatedAt"].IsBsonNull ? null : stored["DeactivatedAt"].ToUniversalTime());
        }
        pdf.ClientVersion.ShouldNotBeNull();
        pdf.ClientVersion.Id.ShouldBe(claim.ClientDataVersionId);
        EqualClient(pdf.ClientVersion, _known["clientDataVersions"].Single(document => Id(document) == claim.ClientDataVersionId));
        if (pdf.File.Exists) AssertPdf(pdf.File);
        else pdf.ExceptionType.ShouldBe(typeof(IOException).FullName);
        context.RecordEvidence("Verified complete invoice rendering inputs", new { pdf.InvoiceId, pdf.AttemptId, order = pdf.Order, products = pdf.Products, clientVersion = pdf.ClientVersion, total = pdf.Order.Lines.Sum(line => line.Quantity * pdf.Products.Single(item => item.Id == line.ProductVersionId).Price.Amount) });
    }

    [Then("all invoice visibility and client version reads preserve stored state")]
    public async Task ThenSupporting(Table table)
    {
        var values = Values(table);
        context.RecordEvidence("Invoice supporting read expectations", values);
        values["InvoiceRoutes"].ShouldBe("by-id-and-by-order");
        values["Incomplete"].ShouldBe("hidden");
        values["Collections"].ShouldBe("all-five-unchanged");
        var before = await context.Factory.GetCheckoutDocumentsAsync();
        Attach("Invoice database before supporting reads", before);
        EqualState(before, _known);
        foreach (var invoice in before["invoices"]) await ReadInvoice(invoice, invoice["GenerationStatus"].AsString == "Completed");
        if (Target(before) is null)
        {
            var path = $"/invoices/by-order/{_orders["target"]}";
            using var response = await context.HttpClient.GetAsync(path);
            await Problem(response, 404, "Resource not found.", "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4",
                $"Resource Invoice identified by id {_orders["target"]} cannot be found in database during action LoadInvoiceByOrderId.", path);
        }
        foreach (var client in new[] { "A", "B" })
        {
            var path = $"/client-data-versions/client/{_clients[client]}";
            using var response = await context.HttpClient.GetAsync(path);
            var expected = before["clientDataVersions"].Where(document => GuidField(document, "ClientId") == _clients[client]).MaxBy(document => document["CreatedAt"].ToUniversalTime());
            if (expected is null)
                await Problem(response, 404, "Resource not found.", "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4",
                    $"Resource ClientDataVersion identified by id {_clients[client]} cannot be found in database during action Load.", path);
            else
            {
                response.StatusCode.ShouldBe(HttpStatusCode.OK);
                response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                CompleteClientNames(json.RootElement);
                var profile = JsonSerializer.Deserialize<ClientDataVersionResponseDto>(json.RootElement, context.JsonOptions);
                profile.ShouldNotBeNull();
                EqualClient(profile, expected);
            }
        }
        var after = await context.Factory.GetCheckoutDocumentsAsync();
        Attach("Invoice database after supporting reads", after);
        EqualState(after, before);
        context.Factory.CatalogRequestCount.ShouldBe(0);
        context.RecordEvidence("Invoice supporting reads verified", new { allCollectionsUnchanged = true, incompleteInvoicesHidden = true, completeMetadataChecked = true, catalogRequests = 0 });
    }

    [When("the invoice owner appends this new client version before retrying")]
    public async Task WhenNewVersion(Table table)
    {
        var values = Values(table);
        context.RecordEvidence("Invoice retry new client version table", values);
        var request = new CreateClientDataVersionRequestDto
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
        var before = await context.Factory.GetCheckoutDocumentsAsync();
        Attach("Invoice retry database before client version append", before);
        EqualState(before, _known);
        var started = DateTime.UtcNow;
        using var response = await context.HttpClient.PostAsJsonAsync($"/client-data-versions/{_clients[values["Client"]]}", request, context.JsonOptions);
        var ended = DateTime.UtcNow;
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        CompleteClientNames(json.RootElement);
        var actual = JsonSerializer.Deserialize<ClientDataVersionResponseDto>(json.RootElement, context.JsonOptions);
        actual.ShouldNotBeNull();
        actual.Id.ShouldNotBe(Guid.Empty);
        _versions.Values.ShouldNotContain(actual.Id);
        actual.CreatedAt.Kind.ShouldBe(DateTimeKind.Utc);
        actual.CreatedAt.ShouldBeGreaterThanOrEqualTo(started);
        actual.CreatedAt.ShouldBeLessThanOrEqualTo(ended);
        var expectedDocument = ClientDocument(values, actual.Id, _clients[values["Client"]], actual.CreatedAt);
        EqualClient(actual, expectedDocument);
        var expected = Clone(before);
        expected["clientDataVersions"] = expected["clientDataVersions"].Append(expectedDocument).OrderBy(Id).ToArray();
        var after = await context.Factory.GetCheckoutDocumentsAsync();
        Attach("Invoice retry database after client version append", after);
        EqualState(after, expected);
        _versions.Add(values["Version"], actual.Id);
        _known = after;
        context.RecordEvidence("Verified retry client version append", new { request, response = actual, startedAt = started, completedAt = ended, onlyOneClientVersionAdded = true });
    }

    private async Task ReadInvoice(BsonDocument stored, bool visible)
    {
        var id = Id(stored);
        var orderId = GuidField(stored, "OrderId");
        foreach (var path in new[] { $"/invoices/{id}", $"/invoices/by-order/{orderId}" })
        {
            using var response = await context.HttpClient.GetAsync(path);
            if (visible) EqualInvoice(await ReadMetadata(response), stored);
            else
            {
                var byOrder = path.Contains("/by-order/", StringComparison.Ordinal);
                await Problem(response, 404, "Resource not found.", "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4",
                    $"Resource Invoice identified by id {(byOrder ? orderId : id)} cannot be found in database during action {(byOrder ? "LoadInvoiceByOrderId" : "LoadInvoiceById")}.", path);
            }
        }
    }

    private async Task<InvoiceResponseDto> ReadMetadata(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Names(json.RootElement, "id", "orderId", "clietDataVersionId", "storageUrl", "createdAt");
        foreach (var property in json.RootElement.EnumerateObject()) property.Value.ValueKind.ShouldBe(JsonValueKind.String);
        var invoice = JsonSerializer.Deserialize<InvoiceResponseDto>(json.RootElement, context.JsonOptions);
        invoice.ShouldNotBeNull();
        invoice.CreatedAt.Kind.ShouldBe(DateTimeKind.Utc);
        return invoice;
    }
    private static void EqualInvoice(InvoiceResponseDto actual, BsonDocument expected)
    {
        actual.Id.ShouldBe(Id(expected));
        actual.OrderId.ShouldBe(GuidField(expected, "OrderId"));
        actual.ClietDataVersionId.ShouldBe(GuidField(expected, "ClientDataVersionId"));
        actual.StorageUrl.ShouldBe(expected["StorageUrl"].AsString);
        Milliseconds(actual.CreatedAt).ShouldBe(expected["CreatedAt"].ToUniversalTime());
    }
    private async Task Problem(HttpResponseMessage response, int status, string title, string type, string detail, string path, string[]? error = null)
    {
        ((int)response.StatusCode).ShouldBe(status);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var value = json.RootElement;
        context.RecordEvidence("Complete actual invoice problem", new { path, problem = value.Clone() });
        value.GetProperty("status").GetInt32().ShouldBe(status);
        value.GetProperty("title").GetString().ShouldBe(title);
        value.GetProperty("type").GetString().ShouldBe(type);
        value.GetProperty("detail").GetString().ShouldBe(detail);
        value.GetProperty("instance").GetString().ShouldBe(path);
        value.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        if (error is null) Names(value, "type", "title", "status", "detail", "instance", "traceId");
        else
        {
            Names(value, "type", "title", "status", "detail", "instance", "traceId", "errors");
            var errors = value.GetProperty("errors");
            errors.GetArrayLength().ShouldBe(1);
            Names(errors[0], "name", "entity", "message");
            errors[0].GetProperty("name").GetString().ShouldBe(error[0]);
            errors[0].GetProperty("entity").GetString().ShouldBe(error[1]);
            errors[0].GetProperty("message").GetString().ShouldBe(error[2]);
        }
    }
    private static void EqualClient(ClientDataVersionResponseDto actual, BsonDocument expected)
    {
        actual.Id.ShouldBe(Id(expected));
        actual.ClientId.ShouldBe(GuidField(expected, "ClientId"));
        Milliseconds(actual.CreatedAt).ShouldBe(expected["CreatedAt"].ToUniversalTime());
        actual.ClientName.ShouldBe(expected["ClientName"].AsString);
        actual.PostalCode.ShouldBe(expected["PostalCode"].AsString);
        actual.City.ShouldBe(expected["City"].AsString);
        actual.Street.ShouldBe(expected["Street"].AsString);
        actual.BuildingNumber.ShouldBe(expected["BuildingNumber"].AsString);
        actual.ApartmentNumber.ShouldBe(expected["ApartmentNumber"].AsString);
        actual.PhoneNumber.ShouldBe(expected["PhoneNumber"].AsString);
        actual.PhonePrefix.ShouldBe(expected["PhonePrefix"].AsString);
        actual.AddressEmail.ShouldBe(expected["AddressEmail"].AsString);
    }
    private static void CompleteClientNames(JsonElement value) => Names(value, "id", "clientId", "clientName", "postalCode", "city", "street", "buildingNumber", "apartmentNumber", "phoneNumber", "phonePrefix", "addressEmail", "createdAt");
    private static void Names(JsonElement value, params string[] names)
    {
        value.ValueKind.ShouldBe(JsonValueKind.Object);
        value.EnumerateObject().Select(property => property.Name).Order().ShouldBe(names.Order());
    }
    private static void AssertPdf(PdfFileObservation file)
    {
        file.Exists.ShouldBeTrue();
        file.Length.ShouldBeGreaterThan(5L);
        file.Signature.ShouldBe("%PDF-");
        file.Sha256.ShouldNotBeNullOrWhiteSpace();
        file.Sha256.Length.ShouldBe(64);
    }
    private BsonDocument? Target(Dictionary<string, BsonDocument[]> state) => state["invoices"].SingleOrDefault(document => GuidField(document, "OrderId") == _orders["target"]);
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
    private static BsonDocument ClientDocument(IReadOnlyDictionary<string, string> values, Guid id, Guid clientId, DateTime createdAt) => new()
    {
        ["_id"] = Binary(id),
        ["ClientId"] = Binary(clientId),
        ["ClientName"] = values["ClientName"],
        ["PostalCode"] = values["PostalCode"],
        ["City"] = values["City"],
        ["Street"] = values["Street"],
        ["BuildingNumber"] = values["BuildingNumber"],
        ["ApartmentNumber"] = values["ApartmentNumber"],
        ["PhoneNumber"] = values["PhoneNumber"],
        ["PhonePrefix"] = values["PhonePrefix"],
        ["AddressEmail"] = values["AddressEmail"],
        ["CreatedAt"] = createdAt
    };
}
