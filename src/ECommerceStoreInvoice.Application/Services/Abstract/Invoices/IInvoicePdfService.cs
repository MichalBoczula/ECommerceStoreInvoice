using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate;

namespace ECommerceStoreInvoice.Application.Services.Abstract.Invoices
{
    public interface IInvoicePdfService
    {
        Task<string> GenerateInvoicePdf(Order order, ShoppingCart? shoppingCart);
    }
}
