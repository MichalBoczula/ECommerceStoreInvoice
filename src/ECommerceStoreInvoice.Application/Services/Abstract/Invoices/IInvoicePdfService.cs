using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;

namespace ECommerceStoreInvoice.Application.Services.Abstract.Invoices
{
    public interface IInvoicePdfService
    {
        Task<string> GenerateInvoicePdf(
            Guid invoiceId,
            Guid attemptId,
            Order order,
            IReadOnlyCollection<ProductVersion> productVersions,
            ClientDataVersionResponseDto? clientDataVersion);
        Task DeleteGeneratedPdf(Guid invoiceId, Guid attemptId);
    }
}
