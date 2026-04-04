using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Descriptors.Orders;
using ECommerceStoreInvoice.Application.Services.Abstract.Orders;

namespace ECommerceStoreInvoice.Application.Services.Concrete.Orders
{
    internal class OrderDescriptorService : IOrderDescriptorService
    {
        public FlowDescriptor GetCreateOrderDescriptor()
        {
            var descriptor = new CreateOrderDescriptor();
            return descriptor.Describe();
        }

        public FlowDescriptor GetOrderByIdDescriptor()
        {
            var descriptor = new GetOrderByIdDescriptor();
            return descriptor.Describe();
        }

        public FlowDescriptor GetOrdersByClientIdDescriptor()
        {
            var descriptor = new GetOrdersByClientIdDescriptor();
            return descriptor.Describe();
        }

        public FlowDescriptor GetUpdateOrderStatusDescriptor()
        {
            var descriptor = new UpdateOrderStatusDescriptor();
            return descriptor.Describe();
        }
    }
}
