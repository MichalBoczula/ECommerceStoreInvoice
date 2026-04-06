using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using ECommerceStoreInvoice.Infrastructure.Persistence.Invoices;

namespace ECommerceStoreInvoice.Infrastructure.Mapping
{
    internal static class InvoiceMapping
    {
        internal static InvoiceDocument MapToDocument(Invoice invoice)
        {
            return new InvoiceDocument
            {
                Id = invoice.Id,
                OrderId = invoice.OrderId,
                ClientDataVersionId = invoice.ClientDataVersionId,
                StorageUrl = invoice.StorageUrl,
                CreatedAt = invoice.CreatedAt
            };
        }

        internal static Invoice MapToDomain(InvoiceDocument document)
        {
            return Invoice.Rehydrate(
                document.Id,
                document.OrderId,
                document.ClientDataVersionId,
                document.StorageUrl,
                document.CreatedAt);
        }
    }
}
