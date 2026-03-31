using ECommerceStoreInvoice.Application.Common.FlowDescriptors;

namespace ECommerceStoreInvoice.Application.Services.Abstract
{
    public interface IOrderDescriptorService
    {
        FlowDescriptor GetCreateOrderDescriptor();
        FlowDescriptor GetOrderByIdDescriptor();
    }
}
