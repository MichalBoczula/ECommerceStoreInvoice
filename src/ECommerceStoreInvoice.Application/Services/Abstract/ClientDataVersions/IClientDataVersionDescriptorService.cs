using ECommerceStoreInvoice.Application.Common.FlowDescriptors;

namespace ECommerceStoreInvoice.Application.Services.Abstract.ClientDataVersions
{
    public interface IClientDataVersionDescriptorService
    {
        FlowDescriptor GetCreateClientDataVersionDescriptor();
        FlowDescriptor GetClientDataVersionByClientIdDescriptor();
    }
}
