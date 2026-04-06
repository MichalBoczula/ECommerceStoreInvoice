using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Services.Abstract.ClientDataVersions;
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
        IClientDataVersionService clientDataVersionService,
        IInvoicePdfService invoicePdfService)
        : IInvoiceService
    {
        public async Task<InvoiceResponseDto> CreateInvoiceForOrder(Guid clientId, Guid orderId)
        {
            var descriptor = new CreateInvoiceForOrderDescriptor();

            var order = await descriptor.LoadOrder(orderId, orderRepository);
            if (order is not null && order.ClientId != clientId)
            {
                order = null;
            }

            descriptor.ThrowNotFoundExceptionIfOrderMissing(orderId, order);

            var existingInvoice = await descriptor.LoadInvoiceByOrderId(orderId, invoiceRepository);
            descriptor.ThrowAlreadyExistsExceptionIfInvoiceAlreadyExists(orderId, existingInvoice);

            var shoppingCart = await descriptor.LoadShoppingCart(order!.ClientId, shoppingCartRepository);
            var clientDataVersion = await clientDataVersionService.GetByClientId(clientId);
            var storageUrl = await descriptor.GenerateInvoicePdf(order, shoppingCart, clientDataVersion, invoicePdfService);

            var invoice = descriptor.CreateInvoice(orderId, clientDataVersion!.Id, storageUrl);
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
