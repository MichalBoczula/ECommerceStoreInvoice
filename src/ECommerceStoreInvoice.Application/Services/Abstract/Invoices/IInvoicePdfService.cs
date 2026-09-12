using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;

namespace ECommerceStoreInvoice.Application.Services.Abstract.Invoices
{
    public interface IInvoicePdfService
    {
        Task<string> GenerateInvoicePdf(
            Order order,
            IReadOnlyCollection<ProductVersion> productVersions,
            ClientDataVersionResponseDto? clientDataVersion);
    }
}
