using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Descriptors.Invoices
{
    internal sealed record CreateInvoiceForOrder;

    internal sealed class CreateInvoiceForOrderDescriptor : FlowDescriberBase<CreateInvoiceForOrder>
    {
        [FlowStep(order: 1, bpmnId: "LoadOrder")]
        public async Task<(Order? Order, ClientDataVersion? ClientDataVersion)> LoadOrderWithLatestClientDataVersion(Guid clientId, Guid orderId, IOrderRepository orderRepository)
        {
            return await orderRepository.GetOrderWithLatestClientDataVersion(clientId, orderId);
        }

        [FlowStep(order: 2, bpmnId: "IsOrderExists")]
        public void ThrowNotFoundExceptionIfOrderMissing(Guid orderId, Order? order)
        {
            if (order is null)
            {
                throw new ResourceNotFoundException(nameof(LoadOrderWithLatestClientDataVersion), orderId, nameof(Order));
            }
        }

        [FlowStep(order: 3, bpmnId: "LoadInvoiceByOrderId")]
        public async Task<Invoice?> LoadInvoiceByOrderId(Guid orderId, IInvoiceRepository invoiceRepository)
        {
            return await invoiceRepository.GetInvoiceByOrderId(orderId);
        }

        [FlowStep(order: 4, bpmnId: "IsInvoiceAlreadyExists")]
        public void ThrowAlreadyExistsExceptionIfInvoiceAlreadyExists(Guid orderId, Invoice? existingInvoice)
        {
            if (existingInvoice is not null)
            {
                throw new ResourceAlreadyExistsException(nameof(LoadInvoiceByOrderId), orderId, nameof(Invoice));
            }
        }

        [FlowStep(order: 5, bpmnId: "LoadShoppingCart")]
        public async Task<ShoppingCart?> LoadShoppingCart(Guid clientId, IShoppingCartRepository shoppingCartRepository)
        {
            return await shoppingCartRepository.GetShoppingCartByClientId(clientId);
        }

        [FlowStep(order: 6, bpmnId: "GenerateInvoicePdf")]
        public async Task<string> GenerateInvoicePdf(Order order, ShoppingCart? shoppingCart, ClientDataVersion? clientDataVersion, IInvoicePdfService invoicePdfService)
        {
            return await invoicePdfService.GenerateInvoicePdf(order, shoppingCart, clientDataVersion);
        }

        [FlowStep(order: 7, bpmnId: "CreateInvoiceDomain")]
        public Invoice CreateInvoice(Guid orderId, string storageUrl)
        {
            return new Invoice(orderId, storageUrl);
        }

        [FlowStep(order: 8, bpmnId: "SaveInvoice")]
        public async Task<Invoice> SaveInvoice(Invoice invoice, IInvoiceRepository invoiceRepository)
        {
            return await invoiceRepository.CreateInvoice(invoice);
        }

        [FlowStep(order: 9, bpmnId: "MapInvoiceResponse")]
        public InvoiceResponseDto MapToResponse(Invoice invoice)
        {
            return MappingConfig.MapToResponse(invoice);
        }

    }
}
