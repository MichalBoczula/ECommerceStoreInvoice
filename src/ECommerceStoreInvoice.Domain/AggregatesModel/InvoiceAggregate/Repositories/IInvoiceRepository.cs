namespace ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories
{
    public interface IInvoiceRepository
    {
        Task<Invoice> CreateInvoice(Invoice invoice);
        Task<Invoice?> GetInvoiceById(Guid invoiceId);
        Task<Invoice?> GetInvoiceByOrderId(Guid orderId);
    }
}
