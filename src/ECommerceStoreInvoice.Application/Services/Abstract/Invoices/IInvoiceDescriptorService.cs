using ECommerceStoreInvoice.Application.Common.FlowDescriptors;

namespace ECommerceStoreInvoice.Application.Services.Abstract.Invoices
{
    public interface IInvoiceDescriptorService
    {
        FlowDescriptor GetCreateInvoiceForOrderDescriptor();
        FlowDescriptor GetInvoiceByIdDescriptor();
    }
}
