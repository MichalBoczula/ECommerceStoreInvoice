using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Descriptors.ProductVersions;
using ECommerceStoreInvoice.Application.Services.Abstract.ProductVersions;

namespace ECommerceStoreInvoice.Application.Services.Concrete.ProductVersions
{
    internal class ProductVersionDescriptorService : IProductVersionDescriptorService
    {
        public FlowDescriptor GetCreateProductVersionDescriptor()
        {
            var descriptor = new CreateProductVersionDescriptor();
            return descriptor.Describe();
        }

        public FlowDescriptor GetProductVersionByIdDescriptor()
        {
            var descriptor = new GetProductVersionByIdDescriptor();
            return descriptor.Describe();
        }
    }
}
