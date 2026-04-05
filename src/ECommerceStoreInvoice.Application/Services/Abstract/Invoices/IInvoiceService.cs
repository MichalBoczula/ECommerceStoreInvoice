using ECommerceStoreInvoice.Application.Common.ResponsesDto;

namespace ECommerceStoreInvoice.Application.Services.Abstract.Invoices
{
    public interface IInvoiceService
    {
        Task<InvoiceResponseDto> CreateInvoiceForOrder(Guid clientId, Guid orderId);
        Task<InvoiceResponseDto> GetInvoiceById(Guid invoiceId);
    }
}
