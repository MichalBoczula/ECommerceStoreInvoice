using ECommerceStoreInvoice.Application.Services.Abstract;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;

namespace ECommerceStoreInvoice.Application.Services.Concrete
{
    internal class ShoppingCartService (
        IShoppingCartRepository _shoppingCartRepository) 
        : IShoppingCartService
    {
    }
}
