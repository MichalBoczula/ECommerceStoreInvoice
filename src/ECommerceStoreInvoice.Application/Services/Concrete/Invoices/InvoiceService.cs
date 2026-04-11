using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Services.Abstract.ClientDataVersions;
using ECommerceStoreInvoice.Application.Descriptors.Invoices;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Services.Concrete.Invoices
{
    internal sealed class InvoiceService(
        IInvoiceRepository invoiceRepository,
        IOrderRepository orderRepository,
        IShoppingCartRepository shoppingCartRepository,
        IClientDataVersionService clientDataVersionService,
        IInvoicePdfService invoicePdfService,
        IValidationPolicy<Guid> guidValidationPolicy,
        IValidationPolicy<InvoiceOrderStatusValidationContext> createInvoiceValidationPolicy)
        : IInvoiceService
    {
        public async Task<InvoiceResponseDto> CreateInvoiceForOrder(Guid clientId, Guid orderId)
        {
            var descriptor = new CreateInvoiceForOrderDescriptor();

            var validationResult = await descriptor.ValidateClientId(clientId, guidValidationPolicy);
            descriptor.ThrowValidationExceptionIfClientIdInvalid(validationResult);

            validationResult = await descriptor.ValidateOrderId(orderId, guidValidationPolicy);
            descriptor.ThrowValidationExceptionIfOrderIdInvalid(validationResult);

            var order = await descriptor.LoadOrder(orderId, orderRepository);
            if (order is not null && order.ClientId != clientId)
            {
                order = null;
            }

            descriptor.ThrowNotFoundExceptionIfOrderMissing(orderId, order);

            var existingInvoice = await descriptor.LoadInvoiceByOrderId(orderId, invoiceRepository);
            descriptor.ThrowAlreadyExistsExceptionIfInvoiceAlreadyExists(orderId, existingInvoice);
            validationResult = await descriptor.ValidateOrderStatus(order!, createInvoiceValidationPolicy);
            descriptor.ThrowValidationExceptionIfOrderStatusInvalid(validationResult);

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

            var validationResult = await descriptor.ValidateInvoiceId(invoiceId, guidValidationPolicy);
            descriptor.ThrowValidationExceptionIfInvoiceIdInvalid(validationResult);

            var invoice = await descriptor.LoadInvoiceById(invoiceId, invoiceRepository);
            descriptor.ThrowNotFoundExceptionIfInvoiceMissing(invoiceId, invoice);

            return descriptor.MapToResponse(invoice!);
        }
    }
}
