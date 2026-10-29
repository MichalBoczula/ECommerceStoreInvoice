using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Orders.Concurrency;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Reqnroll;
using Shouldly;
using static ECommerceStoreInvoice.Acceptance.Tests.Features.Orders.Concurrency.OrderConcurrencySteps;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Invoices.Generation;

[Binding]
public sealed class StaleInvoiceAttemptSteps(ScenarioApiContext context, InvoiceGenerationSteps fixture)
{
    private Dictionary<string, BsonDocument[]>? _expected;
    private PdfFileObservation? _winnerFile;
    private Guid _invoiceId;
    private Guid _attemptId;
    [When("an expired held invoice attempt resumes after its replacement completes")]
    public async Task WhenStale(Table table)
    {
        var values = table.Rows.ToDictionary(r => r["Field"], r => r["Value"]);
        context.RecordEvidence("Stale invoice attempt expectations", values);
        values["LeaseFixture"].ShouldBe("expire-only-held-lease");
        values["Completion"].ShouldBe("real-replacement-before-old-resumes");
        values["Cleanup"].ShouldBe("explicit-old-attempt-only");
        var probe = context.Factory.InvoiceProbe!;
        var before = fixture.FixtureState;
        var path = $"/invoices/{fixture.TargetClientId}/{fixture.TargetOrderId}";
        context.RecordEvidence("Stale invoice complete fixture", InvoiceGenerationProbe.JsonSnapshot(before));
        var oldRequest = context.HttpClient.PostAsync(path, null);
        try
        {
            await probe.WaitForPdfAsync();
            var first = probe.Repository.Single(r => r.Operation == "TryClaim").Claim!;
            var held = await context.Factory.GetCheckoutDocumentsAsync();
            var initial = held["invoices"].Single(d => Id(d) == first.InvoiceId);
            initial["GenerationStatus"].AsString.ShouldBe("Generating");
            initial["GenerationAttemptId"].AsString.ShouldBe(first.AttemptId.ToString("N"));
            initial["StorageUrl"].AsString.ShouldBeEmpty();
            var firstEvent = probe.Repository.Single(r => r.Operation == "TryClaim");
            var reservation = firstEvent.Reservation!;
            Id(initial).ShouldBe(first.InvoiceId); GuidField(initial, "OrderId").ShouldBe(fixture.TargetOrderId);
            GuidField(initial, "ClientDataVersionId").ShouldBe(first.ClientDataVersionId);
            initial["CreatedAt"].ToUniversalTime().ShouldBe(new DateTime(reservation.CreatedAt.Ticks - reservation.CreatedAt.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc));
            initial["GenerationLeaseUntil"].ToUniversalTime().ShouldBeGreaterThan(firstEvent.StartedAt.AddMinutes(9));
            initial.Names.Order().ShouldBe(new[] { "_id", "OrderId", "ClientDataVersionId", "StorageUrl", "CreatedAt", "GenerationStatus", "GenerationAttemptId", "GenerationLeaseUntil" }.Order());
            var expectedHeld = Clone(before);
            expectedHeld["invoices"] = expectedHeld["invoices"].Append(initial).OrderBy(Id).ToArray();
            Equal(held, expectedHeld);
            await Hidden(first.InvoiceId, fixture.TargetOrderId);
            await context.Factory.ExpireInvoiceLeaseAsync(first.InvoiceId);
            var expired = Clone(held);
            expired["invoices"].Single(d => Id(d) == first.InvoiceId)["GenerationLeaseUntil"] = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Equal(await context.Factory.GetCheckoutDocumentsAsync(), expired);
            context.RecordEvidence("Explicit expired invoice lease fixture", new { invoiceId = first.InvoiceId, attemptId = first.AttemptId, before = InvoiceGenerationProbe.JsonSnapshot(held), after = InvoiceGenerationProbe.JsonSnapshot(expired), onlyLeaseChanged = true });
            using var winnerResponse = await context.HttpClient.PostAsync(path, null);
            winnerResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
            var claims = probe.Repository.Where(r => r.Operation == "TryClaim").ToArray();
            claims.Length.ShouldBe(2); claims.ShouldAllBe(r => r.Claim != null);
            var second = claims[1].Claim!;
            second.InvoiceId.ShouldBe(first.InvoiceId); second.AttemptId.ShouldNotBe(first.AttemptId);
            second.OrderId.ShouldBe(fixture.TargetOrderId); second.ClientDataVersionId.ShouldBe(first.ClientDataVersionId);
            var latest = before["clientDataVersions"].Where(d => GuidField(d, "ClientId") == fixture.TargetClientId).MaxBy(d => d["CreatedAt"].ToUniversalTime())!;
            second.ClientDataVersionId.ShouldBe(Id(latest));
            var reclaimed = claims[1].After["invoices"].Single(d => Id(d) == second.InvoiceId);
            var expectedClaim = Clone(expired);
            var claimDocument = expectedClaim["invoices"].Single(d => Id(d) == second.InvoiceId);
            claimDocument["GenerationAttemptId"] = second.AttemptId.ToString("N");
            claimDocument["GenerationLeaseUntil"] = reclaimed["GenerationLeaseUntil"];
            reclaimed["GenerationLeaseUntil"].ToUniversalTime().ShouldBeGreaterThan(claims[1].StartedAt.AddMinutes(9));
            Equal(claims[1].After, expectedClaim);
            var completed = await context.Factory.GetCheckoutDocumentsAsync();
            var stored = completed["invoices"].Single(d => Id(d) == second.InvoiceId);
            var pdf = probe.Pdf.Single(); pdf.AttemptId.ShouldBe(second.AttemptId);
            var expected = Clone(expectedClaim);
            var invoice = expected["invoices"].Single(d => Id(d) == second.InvoiceId);
            invoice["GenerationStatus"] = "Completed"; invoice["GenerationAttemptId"] = BsonNull.Value; invoice["GenerationLeaseUntil"] = BsonNull.Value; invoice["StorageUrl"] = pdf.StorageUrl;
            Equal(completed, expected);
            using var winnerBody = JsonDocument.Parse(await winnerResponse.Content.ReadAsStringAsync());
            JsonEqual(winnerBody.RootElement, BusinessValidationSteps.InvoiceProfile(stored));
            var winnerFile = probe.FileState(second.InvoiceId, second.AttemptId);
            winnerFile.Exists.ShouldBeTrue(); winnerFile.Signature.ShouldBe("%PDF-"); winnerFile.Length.ShouldBeGreaterThan(0);
            probe.ResumePdf();
            using var staleResponse = await oldRequest.WaitAsync(TimeSpan.FromSeconds(45));
            await Problem(staleResponse, 500, path);
            var rejected = probe.Repository.Single(r => r.Operation == "Complete" && r.AttemptId == first.AttemptId);
            rejected.ExceptionType.ShouldBe("System.InvalidOperationException");
            Equal(rejected.Before, expected); Equal(rejected.After, expected);
            var release = probe.Repository.Single(r => r.Operation == "Release");
            release.AttemptId.ShouldBe(first.AttemptId); Equal(release.Before, expected); Equal(release.After, expected);
            Equal(await context.Factory.GetCheckoutDocumentsAsync(), expected);
            probe.FileState(second.InvoiceId, second.AttemptId).ShouldBe(winnerFile);
            var oldFile = probe.FileState(first.InvoiceId, first.AttemptId);
            oldFile.Exists.ShouldBeTrue(); oldFile.Path.ShouldNotBe(winnerFile.Path);
            // Completion was attempted: production deliberately preserves the ambiguous old PDF.
            // This explicit cleanup call tests storage attempt fencing, not an automatic orphan sweeper.
            probe.Deletes.ShouldBeEmpty();
            await using (var scope = context.Factory.Services.CreateAsyncScope())
                await scope.ServiceProvider.GetRequiredService<IInvoicePdfService>().DeleteGeneratedPdf(first.InvoiceId, first.AttemptId);
            var deletion = probe.Deletes.Single();
            deletion.AttemptId.ShouldBe(first.AttemptId); deletion.Before.ShouldBe(oldFile); deletion.After.Exists.ShouldBeFalse();
            probe.FileState(second.InvoiceId, second.AttemptId).ShouldBe(winnerFile);
            foreach (var route in new[] { $"/invoices/{second.InvoiceId}", $"/invoices/by-order/{second.OrderId}" })
            {
                using var response = await context.HttpClient.GetAsync(route); response.StatusCode.ShouldBe(HttpStatusCode.OK);
                using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); JsonEqual(body.RootElement, BusinessValidationSteps.InvoiceProfile(stored));
            }
            using var download = await context.HttpClient.GetAsync($"/invoices/{second.InvoiceId}/pdf");
            download.StatusCode.ShouldBe(HttpStatusCode.OK); download.Content.Headers.ContentType?.MediaType.ShouldBe("application/pdf");
            download.Content.Headers.ContentDisposition!.DispositionType.ShouldBe("attachment");
            download.Content.Headers.ContentDisposition.FileNameStar.ShouldBe($"invoice-{second.InvoiceId:D}.pdf");
            download.Headers.CacheControl!.ToString().Split(',').Select(s => s.Trim()).Order().ShouldBe(new[] { "no-store", "private" });
            var bytes = await download.Content.ReadAsByteArrayAsync();
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant().ShouldBe(winnerFile.Sha256);
            bytes.LongLength.ShouldBe(winnerFile.Length);
            Equal(await context.Factory.GetCheckoutDocumentsAsync(), expected);
            context.Factory.CatalogRequestCount.ShouldBe(0);
            _expected = expected; _winnerFile = winnerFile; _invoiceId = second.InvoiceId; _attemptId = second.AttemptId;
            context.RecordEvidence("Verified invoice stale attempt fencing and explicit cleanup", new { oldClaim = first, winningClaim = second, oldFile, winnerFile, loserStatus = 500, staleCompletionRejected = true, staleReleaseNoOp = true, ambiguousOldPdfPreservedByProduction = true, explicitCleanupDeletedOnlyOldAttempt = true, winningPdfUnchanged = true, expected = InvoiceGenerationProbe.JsonSnapshot(expected), actual = InvoiceGenerationProbe.JsonSnapshot(await context.Factory.GetCheckoutDocumentsAsync()) });
        }
        finally { probe.ResumePdf(); using var response = await oldRequest.WaitAsync(TimeSpan.FromSeconds(45)); }
    }

    [Then("the replacement invoice and PDF survive stale completion release and cleanup")]
    public async Task ThenWinner()
    {
        _expected.ShouldNotBeNull(); _winnerFile.ShouldNotBeNull();
        Equal(await context.Factory.GetCheckoutDocumentsAsync(), _expected);
        context.Factory.InvoiceProbe!.FileState(_invoiceId, _attemptId).ShouldBe(_winnerFile);
        context.Factory.CatalogRequestCount.ShouldBe(0);
        context.RecordEvidence("Stale invoice final winning state", new { expected = InvoiceGenerationProbe.JsonSnapshot(_expected), actual = InvoiceGenerationProbe.JsonSnapshot(await context.Factory.GetCheckoutDocumentsAsync()), winningFile = _winnerFile, catalogRequests = 0 });
    }

    private async Task Hidden(Guid invoiceId, Guid orderId)
    {
        foreach (var route in new[] { $"/invoices/{invoiceId}", $"/invoices/by-order/{orderId}" })
        {
            using var response = await context.HttpClient.GetAsync(route); response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        }
    }
}
