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
        public async Task<InvoiceResponseDto> CreateInvoiceForOrder(Guid clientId, Guid orderId)
        {
            var descriptor = new CreateInvoiceForOrderDescriptor();

            var readModel = await descriptor.LoadOrderWithLatestClientDataVersion(clientId, orderId, orderRepository);
            var order = readModel.Order;
            descriptor.ThrowNotFoundExceptionIfOrderMissing(orderId, order);

            var existingInvoice = await descriptor.LoadInvoiceByOrderId(orderId, invoiceRepository);
            descriptor.ThrowAlreadyExistsExceptionIfInvoiceAlreadyExists(orderId, existingInvoice);

            var shoppingCart = await descriptor.LoadShoppingCart(order!.ClientId, shoppingCartRepository);
            var storageUrl = await descriptor.GenerateInvoicePdf(order, shoppingCart, readModel.ClientDataVersion, invoicePdfService);

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
