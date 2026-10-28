using System.Collections.Concurrent;
using Azure;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;

namespace ECommerceStoreInvoice.Acceptance.Tests;

internal sealed record InvoiceStorageRead(string StorageUrl, bool? Found, string? ExceptionType, int? StorageStatus);
internal sealed class InvoiceReadProbe(Action<string, object> record)
{
    internal ConcurrentQueue<InvoiceStorageRead> Reads { get; } = new();
    internal void Observe(InvoiceStorageRead value) { Reads.Enqueue(value); record("Observed actual invoice storage read", value); }
}
internal sealed class ObservedInvoiceReadService(IInvoicePdfService inner, InvoiceReadProbe probe) : IInvoicePdfService, IAsyncDisposable
{
    public Task<string> GenerateInvoicePdf(Guid invoiceId, Guid attemptId, Order order, IReadOnlyCollection<ProductVersion> products, ClientDataVersionResponseDto? client) =>
        inner.GenerateInvoicePdf(invoiceId, attemptId, order, products, client);
    public Task DeleteGeneratedPdf(Guid invoiceId, Guid attemptId) => inner.DeleteGeneratedPdf(invoiceId, attemptId);
    public async Task<Stream?> OpenReadAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            var stream = await inner.OpenReadAsync(url, cancellationToken);
            probe.Observe(new(url, stream is not null, null, null));
            return stream;
        }
        catch (Exception exception)
        {
            probe.Observe(new(url, null, exception.GetType().FullName, exception is RequestFailedException storage ? storage.Status : null));
            throw;
        }
    }
    public async ValueTask DisposeAsync() { if (inner is IAsyncDisposable disposable) await disposable.DisposeAsync(); }
}
