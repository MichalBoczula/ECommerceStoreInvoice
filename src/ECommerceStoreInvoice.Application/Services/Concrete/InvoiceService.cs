using ECommerceStoreInvoice.Application.Services.Abstract;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;

namespace ECommerceStoreInvoice.Application.Services.Concrete
{
    internal class InvoiceService(
        IInvoiceRepository _invoiceRepository) 
        : IInvoiceService
    {
    }
}
