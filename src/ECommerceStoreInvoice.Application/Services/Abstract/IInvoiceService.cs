using ECommerceStoreInvoice.Application.Common.ResponsesDto;

namespace ECommerceStoreInvoice.Application.Services.Abstract
{
    public interface IInvoiceService
    {
        Task<InvoiceResponseDto> CreateInvoiceForOrder(Guid orderId);
        Task<InvoiceResponseDto> GetInvoiceById(Guid invoiceId);
    }
}
