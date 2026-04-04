using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Services.Concrete.Invoices
{
    internal sealed class InvoiceService(
        IInvoiceRepository invoiceRepository,
        IOrderRepository orderRepository)
        : IInvoiceService
    {
        public async Task<InvoiceResponseDto> CreateInvoiceForOrder(Guid orderId)
        {
            var order = await orderRepository.GetOrderByOrderId(orderId);

            if (order is null)
            {
                throw new ResourceNotFoundException(
                    nameof(CreateInvoiceForOrder),
                    orderId,
                    nameof(Order));
            }

            var existingInvoice = await invoiceRepository.GetInvoiceByOrderId(orderId);

            if (existingInvoice is not null)
            {
                throw new ResourceAlreadyExistsException(
                    nameof(CreateInvoiceForOrder),
                    orderId,
                    nameof(Invoice));
            }

            var invoice = new Invoice(orderId, BuildStorageUrl(orderId));
            var createdInvoice = await invoiceRepository.CreateInvoice(invoice);

            return MappingConfig.MapToResponse(createdInvoice);
        }

        public async Task<InvoiceResponseDto> GetInvoiceById(Guid invoiceId)
        {
            var invoice = await invoiceRepository.GetInvoiceById(invoiceId);

            if (invoice is null)
            {
                throw new ResourceNotFoundException(
                    nameof(GetInvoiceById),
                    invoiceId,
                    nameof(Invoice));
            }

            return MappingConfig.MapToResponse(invoice);
        }

        private static string BuildStorageUrl(Guid orderId)
        {
            return $"https://invoices.local/orders/{orderId}.pdf";
        }
    }
}
