using ECommerceStoreInvoice.Application.Common.FlowDescriptors;

namespace ECommerceStoreInvoice.Application.Services.Abstract
{
    public interface IProductVersionDescriptorService
    {
        FlowDescriptor GetCreateProductVersionDescriptor();
        FlowDescriptor GetProductVersionByIdDescriptor();
    }
}
