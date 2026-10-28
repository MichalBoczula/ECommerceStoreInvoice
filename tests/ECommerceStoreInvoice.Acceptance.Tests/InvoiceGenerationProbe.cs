using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using MongoDB.Bson;

namespace ECommerceStoreInvoice.Acceptance.Tests;

internal sealed record InvoiceRepositoryObservation(string Operation, Invoice? Reservation, Guid AttemptId,
    InvoiceGenerationClaim? Claim, string? StorageUrl, DateTime StartedAt, DateTime CompletedAt, string? ExceptionType,
    Dictionary<string, BsonDocument[]> Before, Dictionary<string, BsonDocument[]> After);
internal sealed record InvoicePdfObservation(Guid InvoiceId, Guid AttemptId, Order Order,
    ProductVersion[] Products, ClientDataVersionResponseDto? ClientVersion, string? StorageUrl, string? ExceptionType, PdfFileObservation File);
internal sealed record PdfFileObservation(string Path, bool Exists, long Length, string? Sha256, string? Signature);

/// <summary>Scenario-local observers around real Mongo writes and real Chromium PDF generation.</summary>
internal sealed class InvoiceGenerationProbe(Action<string, object> record, Func<Task<Dictionary<string, BsonDocument[]>>> snapshot,
    bool holdPdf, bool failAfterWrite)
{
    private int _afterWriteRemaining = failAfterWrite ? 1 : 0;
    private int _holdRemaining = holdPdf ? 1 : 0;
    private readonly TaskCompletionSource _pdfEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _pdfReleased = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal ConcurrentQueue<InvoiceRepositoryObservation> Repository { get; } = new();
    internal ConcurrentQueue<InvoicePdfObservation> Pdf { get; } = new();
    internal ConcurrentQueue<(Guid InvoiceId, Guid AttemptId, PdfFileObservation Before, PdfFileObservation After)> Deletes { get; } = new();
    private readonly ConcurrentDictionary<string, byte> _files = new(StringComparer.Ordinal);
    internal Task WaitForPdfAsync() => _pdfEntered.Task.WaitAsync(TimeSpan.FromSeconds(30));
    internal void ResumePdf() => _pdfReleased.TrySetResult();
    internal void CleanupFiles() { ResumePdf(); foreach (var path in _files.Keys) File.Delete(path); }
    internal async Task PausePdfAsync()
    {
        if (Interlocked.Exchange(ref _holdRemaining, 0) != 1) return;
        _pdfEntered.TrySetResult();
        await _pdfReleased.Task.WaitAsync(TimeSpan.FromSeconds(30));
    }
    internal bool FailWrittenPdf() => Interlocked.Exchange(ref _afterWriteRemaining, 0) == 1;
    internal Task<Dictionary<string, BsonDocument[]>> Snapshot() => snapshot();
    internal void ObserveRepository(InvoiceRepositoryObservation value)
    {
        Repository.Enqueue(value);
        record("Observed real invoice repository exchange", new
        {
            value.Operation,
            value.Reservation,
            value.AttemptId,
            value.Claim,
            value.StorageUrl,
            value.StartedAt,
            value.CompletedAt,
            value.ExceptionType,
            before = JsonSnapshot(value.Before), after = JsonSnapshot(value.After)
        });
    }
    internal void ObservePdf(InvoicePdfObservation value)
    {
        Pdf.Enqueue(value);
        record("Observed real invoice PDF attempt", value);
    }
    internal void ObserveDelete(Guid invoiceId, Guid attemptId, PdfFileObservation before, PdfFileObservation after)
    {
        Deletes.Enqueue((invoiceId, attemptId, before, after));
        record("Observed real invoice PDF cleanup", new { invoiceId, attemptId, before, after });
    }
    internal PdfFileObservation FileState(Guid invoiceId, Guid attemptId)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root.Parent is not null && !File.Exists(System.IO.Path.Combine(root.FullName, "ECommerceStoreInvoice.slnx"))) root = root.Parent;
        var path = System.IO.Path.Combine(root.FullName, "Invoices", $"{invoiceId:N}-{attemptId:N}.pdf");
        _files.TryAdd(path, 0);
        if (!File.Exists(path)) return new(path, false, 0, null, null);
        var bytes = File.ReadAllBytes(path);
        return new(path, true, bytes.LongLength, Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), System.Text.Encoding.ASCII.GetString(bytes, 0, Math.Min(5, bytes.Length)));
    }
    internal static object JsonSnapshot(Dictionary<string, BsonDocument[]> state) => state.ToDictionary(pair => pair.Key,
        pair => JsonSerializer.Deserialize<JsonElement>(new BsonArray(pair.Value).ToJson()));
}

internal sealed class ObservedInvoiceGenerationRepository(IInvoiceGenerationRepository inner, InvoiceGenerationProbe probe) : IInvoiceGenerationRepository
{
    public async Task<InvoiceGenerationClaim?> TryClaimAsync(Invoice reservation, Guid attemptId)
    {
        var before = await probe.Snapshot();
        var started = DateTime.UtcNow;
        var claim = await inner.TryClaimAsync(reservation, attemptId);
        probe.ObserveRepository(new("TryClaim", reservation, attemptId, claim, null, started, DateTime.UtcNow, null, before, await probe.Snapshot()));
        return claim;
    }
    public async Task<Invoice> CompleteAsync(InvoiceGenerationClaim claim, string storageUrl)
    {
        var before = await probe.Snapshot();
        var started = DateTime.UtcNow;
        string? exceptionType = null;
        try { return await inner.CompleteAsync(claim, storageUrl); }
        catch (Exception exception) { exceptionType = exception.GetType().FullName; throw; }
        finally { probe.ObserveRepository(new("Complete", null, claim.AttemptId, claim, storageUrl, started, DateTime.UtcNow, exceptionType, before, await probe.Snapshot())); }
    }
    public async Task ReleaseAsync(InvoiceGenerationClaim claim)
    {
        var before = await probe.Snapshot();
        var started = DateTime.UtcNow;
        await inner.ReleaseAsync(claim);
        probe.ObserveRepository(new("Release", null, claim.AttemptId, claim, null, started, DateTime.UtcNow, null, before, await probe.Snapshot()));
    }
}

internal sealed class ObservedInvoicePdfService(IInvoicePdfService inner, InvoiceGenerationProbe probe) : IInvoicePdfService, IAsyncDisposable
{
    public async Task<string> GenerateInvoicePdf(Guid invoiceId, Guid attemptId, Order order,
        IReadOnlyCollection<ProductVersion> productVersions, ClientDataVersionResponseDto? clientDataVersion)
    {
        string? url = null;
        string? exceptionType = null;
        try
        {
            await probe.PausePdfAsync();
            url = await inner.GenerateInvoicePdf(invoiceId, attemptId, order, productVersions, clientDataVersion);
            if (probe.FailWrittenPdf()) throw new IOException("Simulated failure after real PDF write.");
            return url;
        }
        catch (Exception exception) { exceptionType = exception.GetType().FullName; throw; }
        finally { probe.ObservePdf(new(invoiceId, attemptId, order, productVersions.ToArray(), clientDataVersion, url, exceptionType, probe.FileState(invoiceId, attemptId))); }
    }
    public async Task DeleteGeneratedPdf(Guid invoiceId, Guid attemptId)
    {
        var before = probe.FileState(invoiceId, attemptId);
        await inner.DeleteGeneratedPdf(invoiceId, attemptId);
        probe.ObserveDelete(invoiceId, attemptId, before, probe.FileState(invoiceId, attemptId));
    }
    public Task<Stream?> OpenReadAsync(string storageUrl, CancellationToken cancellationToken = default) => inner.OpenReadAsync(storageUrl, cancellationToken);
    public async ValueTask DisposeAsync() { if (inner is IAsyncDisposable disposable) await disposable.DisposeAsync(); }
}
