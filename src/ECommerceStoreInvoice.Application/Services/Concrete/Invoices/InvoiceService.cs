using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Descriptors.Invoices;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;

namespace ECommerceStoreInvoice.Application.Services.Concrete.Invoices
{
    internal sealed class InvoiceService(
        IInvoiceRepository invoiceRepository,
        IOrderRepository orderRepository,
        IShoppingCartRepository shoppingCartRepository,
        IInvoicePdfService invoicePdfService)
        : IInvoiceService
    {
        public async Task<InvoiceResponseDto> CreateInvoiceForOrder(Guid orderId)
        {
            var descriptor = new CreateInvoiceForOrderDescriptor();

            var order = await descriptor.LoadOrder(orderId, orderRepository);
            descriptor.ThrowNotFoundExceptionIfOrderMissing(orderId, order);

            var existingInvoice = await descriptor.LoadInvoiceByOrderId(orderId, invoiceRepository);
            descriptor.ThrowAlreadyExistsExceptionIfInvoiceAlreadyExists(orderId, existingInvoice);

            var shoppingCart = await shoppingCartRepository.GetShoppingCartByClientId(order!.ClientId);
            var storageUrl = await invoicePdfService.GenerateInvoicePdf(order!, shoppingCart);

            var invoice = descriptor.CreateInvoice(orderId, storageUrl);
            var createdInvoice = await descriptor.SaveInvoice(invoice, invoiceRepository);

            return descriptor.MapToResponse(createdInvoice);
        }

        public async Task<InvoiceResponseDto> GetInvoiceById(Guid invoiceId)
        {
            var descriptor = new GetInvoiceByIdDescriptor();

            var invoice = await descriptor.LoadInvoiceById(invoiceId, invoiceRepository);
            descriptor.ThrowNotFoundExceptionIfInvoiceMissing(invoiceId, invoice);

            return descriptor.MapToResponse(invoice!);
        }
    }
}
