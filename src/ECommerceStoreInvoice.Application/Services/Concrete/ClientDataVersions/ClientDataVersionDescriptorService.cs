using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Descriptors.ClientDataVersions;
using ECommerceStoreInvoice.Application.Services.Abstract.ClientDataVersions;

namespace ECommerceStoreInvoice.Application.Services.Concrete.ClientDataVersions
{
    internal sealed class ClientDataVersionDescriptorService : IClientDataVersionDescriptorService
    {
        public FlowDescriptor GetCreateClientDataVersionDescriptor()
        {
            var descriptor = new CreateClientDataVersionDescriptor();
            return descriptor.Describe();
        }

        public FlowDescriptor GetClientDataVersionByClientIdDescriptor()
        {
            var descriptor = new GetClientDataVersionByClientIdDescriptor();
            return descriptor.Describe();
        }
    }
}
