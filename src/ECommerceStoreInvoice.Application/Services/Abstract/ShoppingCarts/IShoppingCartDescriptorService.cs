using ECommerceStoreInvoice.Application.Common.FlowDescriptors;

namespace ECommerceStoreInvoice.Application.Services.Abstract.ShoppingCarts
{
    public interface IShoppingCartDescriptorService
    {
        FlowDescriptor GetUpdateShoppingCartDescriptor();
        FlowDescriptor GetShoppingCartByClientIdDescriptor();
        FlowDescriptor GetCreateShoppingCartDescriptor();
    }
}
