namespace ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;

public interface IInvoiceGenerationRepository
{
    Task<InvoiceGenerationClaim?> TryClaimAsync(Invoice reservation, Guid attemptId);
    Task<Invoice> CompleteAsync(InvoiceGenerationClaim claim, string storageUrl);
    Task ReleaseAsync(InvoiceGenerationClaim claim);
}
