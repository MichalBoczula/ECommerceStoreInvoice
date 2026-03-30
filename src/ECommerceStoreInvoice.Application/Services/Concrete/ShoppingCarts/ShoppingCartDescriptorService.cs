using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Descriptors.ShoppingCarts;
using ECommerceStoreInvoice.Application.Services.Abstract.ShoppingCarts;

namespace ECommerceStoreInvoice.Application.Services.Concrete.ShoppingCarts
{
    internal class ShoppingCartDescriptorService : IShoppingCartDescriptorService
    {
        public FlowDescriptor GetShoppingCartByClientIdDescriptor()
        {
            var descriptor = new GetShoppingCartByClientIdDescriptor();
            return descriptor.Describe();
        }

        public FlowDescriptor GetUpdateShoppingCartDescriptor()
        {
            var descriptor = new UpdateShoppingCartDescriptor();
            return descriptor.Describe();
        }
    }
}
