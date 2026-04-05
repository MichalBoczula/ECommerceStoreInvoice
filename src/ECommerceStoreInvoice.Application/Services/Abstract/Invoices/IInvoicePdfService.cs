using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;

namespace ECommerceStoreInvoice.Application.Services.Abstract.Invoices
{
    public interface IInvoicePdfService
    {
        Task<string> GenerateInvoicePdf(Order order, ShoppingCart? shoppingCart, ClientDataVersionResponseDto? clientDataVersion);
    }
}
