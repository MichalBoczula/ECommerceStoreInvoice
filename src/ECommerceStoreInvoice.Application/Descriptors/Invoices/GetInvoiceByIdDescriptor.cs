using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Descriptors.Invoices
{
    internal sealed record GetInvoiceById;

    internal sealed class GetInvoiceByIdDescriptor : FlowDescriberBase<GetInvoiceById>
    {
        [FlowStep(order: 1, bpmnId: "LoadInvoiceById")]
        public async Task<Invoice?> LoadInvoiceById(Guid invoiceId, IInvoiceRepository invoiceRepository)
        {
            return await invoiceRepository.GetInvoiceById(invoiceId);
        }

        [FlowStep(order: 2, bpmnId: "IsInvoiceExists")]
        public void ThrowNotFoundExceptionIfInvoiceMissing(Guid invoiceId, Invoice? invoice)
        {
            if (invoice is null)
            {
                throw new ResourceNotFoundException(nameof(LoadInvoiceById), invoiceId, nameof(Invoice));
            }
        }

        [FlowStep(order: 3, bpmnId: "MapInvoiceResponse")]
        public InvoiceResponseDto MapToResponse(Invoice invoice)
        {
            return MappingConfig.MapToResponse(invoice);
        }
    }
}
