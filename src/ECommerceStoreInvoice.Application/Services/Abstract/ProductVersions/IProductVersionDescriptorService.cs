using ECommerceStoreInvoice.Application.Common.FlowDescriptors;

namespace ECommerceStoreInvoice.Application.Services.Abstract.ProductVersions
{
    public interface IProductVersionDescriptorService
    {
        FlowDescriptor GetCreateProductVersionDescriptor();
        FlowDescriptor GetProductVersionByIdDescriptor();
    }
}
