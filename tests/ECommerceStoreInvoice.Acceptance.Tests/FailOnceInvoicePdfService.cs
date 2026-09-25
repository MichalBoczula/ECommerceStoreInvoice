using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;

namespace ECommerceStoreInvoice.Acceptance.Tests;

internal sealed class PdfFailureState
{
    private int _failuresRemaining = 1;
    public bool FailNextGeneration() => Interlocked.Exchange(ref _failuresRemaining, 0) == 1;
}

internal sealed class FailOnceInvoicePdfService(IInvoicePdfService inner, PdfFailureState state)
    : IInvoicePdfService, IAsyncDisposable
{
    public Task<string> GenerateInvoicePdf(
        Guid invoiceId,
        Guid attemptId,
        Order order,
        IReadOnlyCollection<ProductVersion> productVersions,
        ClientDataVersionResponseDto? clientDataVersion)
    {
        if (state.FailNextGeneration())
            throw new IOException("Simulated PDF generation failure.");

        return inner.GenerateInvoicePdf(invoiceId, attemptId, order, productVersions, clientDataVersion);
    }

    public Task DeleteGeneratedPdf(Guid invoiceId, Guid attemptId) =>
        inner.DeleteGeneratedPdf(invoiceId, attemptId);

    public async ValueTask DisposeAsync()
    {
        if (inner is IAsyncDisposable disposable)
            await disposable.DisposeAsync();
    }
}

internal sealed class CompletionAcknowledgementState
{
    private int _failuresRemaining = 1;
    public bool LoseNextAcknowledgement() => Interlocked.Exchange(ref _failuresRemaining, 0) == 1;
}

internal sealed class LoseOnceInvoiceCompletionAcknowledgement(
    IInvoiceGenerationRepository inner, CompletionAcknowledgementState state) : IInvoiceGenerationRepository
{
    public Task<InvoiceGenerationClaim?> TryClaimAsync(Invoice reservation, Guid attemptId) =>
        inner.TryClaimAsync(reservation, attemptId);

    public async Task<Invoice> CompleteAsync(InvoiceGenerationClaim claim, string storageUrl)
    {
        var saved = await inner.CompleteAsync(claim, storageUrl);
        if (state.LoseNextAcknowledgement())
            throw new IOException("Simulated lost Mongo completion acknowledgement.");
        return saved;
    }

    public Task ReleaseAsync(InvoiceGenerationClaim claim) => inner.ReleaseAsync(claim);
}
