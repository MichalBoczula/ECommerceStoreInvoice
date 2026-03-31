using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Descriptors.Orders;
using ECommerceStoreInvoice.Application.Services.Abstract;

namespace ECommerceStoreInvoice.Application.Services.Concrete
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
    }
}
