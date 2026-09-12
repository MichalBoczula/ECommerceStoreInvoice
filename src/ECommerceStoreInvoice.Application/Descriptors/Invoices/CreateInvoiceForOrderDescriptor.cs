using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Descriptors.Invoices
{
    internal sealed record CreateInvoiceForOrder;

    internal sealed class CreateInvoiceForOrderDescriptor : FlowDescriberBase<CreateInvoiceForOrder>
    {
        [FlowStep(order: 1, bpmnId: "ValidateClientId")]
        public async Task<ValidationResult> ValidateClientId(Guid clientId, IValidationPolicy<Guid> guidValidationPolicy)
        {
            return await guidValidationPolicy.Validate(clientId);
        }

        [FlowStep(order: 2, bpmnId: "ValidateOrderId")]
        public async Task<ValidationResult> ValidateOrderId(Guid orderId, IValidationPolicy<Guid> guidValidationPolicy)
        {
            return await guidValidationPolicy.Validate(orderId);
        }

        [FlowStep(order: 3, bpmnId: "ThrowValidationExceptionIfIdInvalid")]
        public void ThrowValidationExceptionIfIdInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 4, bpmnId: "LoadOrderWithProductVersions")]
        public async Task<(Order Order, IReadOnlyCollection<ProductVersion> ProductVersions)?> LoadOrderWithProductVersions(
            Guid orderId,
            IOrderRepository orderRepository)
        {
            return await orderRepository.GetOrderWithProductVersionsById(orderId);
        }

        [FlowStep(order: 5, bpmnId: "IsOrderExists")]
        public void ThrowNotFoundExceptionIfOrderMissing(
            Guid orderId,
            (Order Order, IReadOnlyCollection<ProductVersion> ProductVersions)? orderWithProductVersions)
        {
            if (orderWithProductVersions is null)
            {
                throw new ResourceNotFoundException(nameof(Order), orderId, $"Order with id '{orderId}' was not found.");
            }
        }

        [FlowStep(order: 6, bpmnId: "LoadInvoiceByOrderId")]
        public async Task<Invoice?> LoadInvoiceByOrderId(Guid orderId, IInvoiceRepository invoiceRepository)
        {
            return await invoiceRepository.GetInvoiceByOrderId(orderId);
        }

        [FlowStep(order: 7, bpmnId: "IsInvoiceAlreadyExists")]
        public void ThrowAlreadyExistsExceptionIfInvoiceAlreadyExists(Guid orderId, Invoice? existingInvoice)
        {
            if (existingInvoice is not null)
            {
                throw new ResourceAlreadyExistsException(nameof(Invoice), orderId, $"Invoice for order '{orderId}' already exists.");
            }
        }

        [FlowStep(order: 8, bpmnId: "ValidateOrderStatus")]
        public async Task<ValidationResult> ValidateOrderStatus(
            Order order,
            IValidationPolicy<InvoiceOrderStatusValidationContext> createInvoiceValidationPolicy)
        {
            return await createInvoiceValidationPolicy.Validate(new InvoiceOrderStatusValidationContext(order));
        }

        [FlowStep(order: 9, bpmnId: "IsOrderStatusValid")]
        public void ThrowValidationExceptionIfOrderStatusInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 10, bpmnId: "GenerateInvoicePdf")]
        public async Task<string> GenerateInvoicePdf(
            Order order,
            IReadOnlyCollection<ProductVersion> productVersions,
            ClientDataVersionResponseDto? clientDataVersion,
            IInvoicePdfService invoicePdfService)
        {
            return await invoicePdfService.GenerateInvoicePdf(order, productVersions, clientDataVersion);
        }

        [FlowStep(order: 11, bpmnId: "CreateInvoiceDomain")]
        public Invoice CreateInvoice(Guid orderId, Guid clientDataVersionId, string storageUrl)
        {
            return new Invoice(orderId, clientDataVersionId, storageUrl);
        }

        [FlowStep(order: 12, bpmnId: "SaveInvoice")]
        public async Task<Invoice> SaveInvoice(Invoice invoice, IInvoiceRepository invoiceRepository)
        {
            return await invoiceRepository.CreateInvoice(invoice);
        }

        [FlowStep(order: 13, bpmnId: "MapInvoiceResponse")]
        public InvoiceResponseDto MapToResponse(Invoice invoice)
        {
            return MappingConfig.MapToResponse(invoice);
        }
    }
}