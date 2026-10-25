namespace ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;

public interface IInvoiceDocumentStorage
{
    Task<string> UploadAsync(Guid invoiceId, Guid attemptId, Stream content, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid invoiceId, Guid attemptId, CancellationToken cancellationToken = default);
    Task<Stream?> OpenReadAsync(string storageUrl, CancellationToken cancellationToken = default);
}
