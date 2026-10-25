using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Descriptors.Invoices;

internal sealed record DownloadInvoicePdf;

internal sealed class DownloadInvoicePdfDescriptor : FlowDescriberBase<DownloadInvoicePdf>
{
    [FlowStep(order: 1, bpmnId: "ValidateInvoiceId")]
    public async Task ValidateInvoiceId(Guid invoiceId, IValidationPolicy<Guid> policy)
    {
        var result = await policy.Validate(invoiceId);
        if (!result.IsValid) throw new ValidationException(result);
    }

    [FlowStep(order: 2, bpmnId: "LoadCompletedInvoice")]
    public async Task<Invoice> LoadInvoice(Guid invoiceId, IInvoiceRepository repository) =>
        await repository.GetInvoiceById(invoiceId)
        ?? throw new ResourceNotFoundException(nameof(LoadInvoice), invoiceId, nameof(Invoice));

    [FlowStep(order: 3, bpmnId: "OpenInvoicePdf")]
    public async Task<Stream> OpenPdf(Invoice invoice, IInvoicePdfService pdfService, CancellationToken cancellationToken) =>
        await pdfService.OpenReadAsync(invoice.StorageUrl, cancellationToken)
        ?? throw new ResourceNotFoundException(nameof(OpenPdf), invoice.Id, "InvoicePdf");
}
