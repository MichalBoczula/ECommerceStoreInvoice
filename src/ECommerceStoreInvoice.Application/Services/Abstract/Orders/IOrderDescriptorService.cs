using ECommerceStoreInvoice.Application.Common.FlowDescriptors;

namespace ECommerceStoreInvoice.Application.Services.Abstract.Orders
{
    public interface IOrderDescriptorService
    {
        FlowDescriptor GetCreateOrderDescriptor();
        FlowDescriptor GetOrderByIdDescriptor();
    }
}
