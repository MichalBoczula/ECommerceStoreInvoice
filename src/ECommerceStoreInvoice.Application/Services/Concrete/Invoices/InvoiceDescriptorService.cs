using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Descriptors.Invoices;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;

namespace ECommerceStoreInvoice.Application.Services.Concrete.Invoices
{
    internal class InvoiceDescriptorService : IInvoiceDescriptorService
    {
        public FlowDescriptor GetCreateInvoiceForOrderDescriptor()
        {
            var descriptor = new CreateInvoiceForOrderDescriptor();
            return descriptor.Describe();
        }

        public FlowDescriptor GetInvoiceByIdDescriptor()
        {
            var descriptor = new GetInvoiceByIdDescriptor();
            return descriptor.Describe();
        }
    }
}
