using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
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

        [FlowStep(order: 2, bpmnId: "IsClientIdValid")]
        public void ThrowValidationExceptionIfClientIdInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 3, bpmnId: "ValidateOrderId")]
        public async Task<ValidationResult> ValidateOrderId(Guid orderId, IValidationPolicy<Guid> guidValidationPolicy)
        {
            return await guidValidationPolicy.Validate(orderId);
        }

        [FlowStep(order: 4, bpmnId: "IsOrderIdValid")]
        public void ThrowValidationExceptionIfOrderIdInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 5, bpmnId: "LoadOrder")]
        public async Task<Order?> LoadOrder(Guid orderId, IOrderRepository orderRepository)
        {
            return await orderRepository.GetOrderByOrderId(orderId);
        }

        [FlowStep(order: 6, bpmnId: "IsOrderExists")]
        public void ThrowNotFoundExceptionIfOrderMissing(Guid orderId, Order? order)
        {
            if (order is null)
            {
                throw new ResourceNotFoundException(nameof(LoadOrder), orderId, nameof(Order));
            }
        }

        [FlowStep(order: 7, bpmnId: "LoadInvoiceByOrderId")]
        public async Task<Invoice?> LoadInvoiceByOrderId(Guid orderId, IInvoiceRepository invoiceRepository)
        {
            return await invoiceRepository.GetInvoiceByOrderId(orderId);
        }

        [FlowStep(order: 8, bpmnId: "IsInvoiceAlreadyExists")]
        public void ThrowAlreadyExistsExceptionIfInvoiceAlreadyExists(Guid orderId, Invoice? existingInvoice)
        {
            if (existingInvoice is not null)
            {
                throw new ResourceAlreadyExistsException(nameof(LoadInvoiceByOrderId), orderId, nameof(Invoice));
            }
        }

        [FlowStep(order: 9, bpmnId: "ValidateOrderStatus")]
        public async Task<ValidationResult> ValidateOrderStatus(
            Order order,
            IValidationPolicy<InvoiceOrderStatusValidationContext> createInvoiceValidationPolicy)
        {
            return await createInvoiceValidationPolicy.Validate(new InvoiceOrderStatusValidationContext(order));
        }

        [FlowStep(order: 10, bpmnId: "IsOrderStatusValid")]
        public void ThrowValidationExceptionIfOrderStatusInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 11, bpmnId: "GenerateInvoicePdf")]
        public async Task<string> GenerateInvoicePdf(Order order, ClientDataVersionResponseDto? clientDataVersion, IInvoicePdfService invoicePdfService)
        {
            return await invoicePdfService.GenerateInvoicePdf(order, clientDataVersion);
        }

        [FlowStep(order: 12, bpmnId: "CreateInvoiceDomain")]
        public Invoice CreateInvoice(Guid orderId, Guid clientDataVersionId, string storageUrl)
        {
            return new Invoice(orderId, clientDataVersionId, storageUrl);
        }

        [FlowStep(order: 13, bpmnId: "SaveInvoice")]
        public async Task<Invoice> SaveInvoice(Invoice invoice, IInvoiceRepository invoiceRepository)
        {
            return await invoiceRepository.CreateInvoice(invoice);
        }

        [FlowStep(order: 14, bpmnId: "MapInvoiceResponse")]
        public InvoiceResponseDto MapToResponse(Invoice invoice)
        {
            return MappingConfig.MapToResponse(invoice);
        }

    }
}
