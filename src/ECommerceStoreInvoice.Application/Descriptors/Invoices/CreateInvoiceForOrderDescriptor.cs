using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using ECommerceStoreInvoice.Application.Services.Abstract.ClientDataVersions;
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

        [FlowStep(order: 6, bpmnId: "IsOrderOwnedByClient")]
        public void ThrowNotFoundExceptionIfOrderOwnedByAnotherClient(Guid clientId, Guid orderId, Order order)
        {
            if (order.ClientId != clientId)
                throw new ResourceNotFoundException(nameof(Order), orderId, $"Order with id '{orderId}' was not found.");
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
                throw new ResourceAlreadyExistsException(nameof(Invoice), orderId, $"Invoice for order '{orderId}' already exists.");
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

        [FlowStep(order: 11, bpmnId: "LoadClientDataVersion")]
        public Task<ClientDataVersionResponseDto?> LoadClientDataVersion(Guid clientId, IClientDataVersionService service) =>
            service.GetByClientId(clientId);

        [FlowStep(order: 12, bpmnId: "CreateInvoiceReservation")]
        public Invoice CreateInvoiceReservation(Guid orderId, Guid clientDataVersionId) =>
            new(orderId, clientDataVersionId, string.Empty);

        [FlowStep(order: 13, bpmnId: "CreateGenerationAttemptId")]
        public Guid CreateGenerationAttemptId() => Guid.NewGuid();

        [FlowStep(order: 14, bpmnId: "TryClaimInvoiceGeneration")]
        public Task<InvoiceGenerationClaim?> TryClaimInvoiceGeneration(Invoice invoice, Guid attemptId, IInvoiceGenerationRepository repository) =>
            repository.TryClaimAsync(invoice, attemptId);

        [FlowStep(order: 15, bpmnId: "IsGenerationClaimAvailable")]
        public void ThrowAlreadyExistsExceptionIfClaimMissing(Guid orderId, InvoiceGenerationClaim? claim)
        {
            if (claim is null)
                throw new ResourceAlreadyExistsException(nameof(Invoice), orderId, $"Invoice generation for order '{orderId}' is already in progress or completed.");
        }

        [FlowStep(order: 16, bpmnId: "GenerateInvoicePdf")]
        public async Task<string> GenerateInvoicePdf(
            InvoiceGenerationClaim claim,
            Order order,
            IReadOnlyCollection<ProductVersion> productVersions,
            ClientDataVersionResponseDto? clientDataVersion,
            IInvoicePdfService invoicePdfService)
        {
            return await invoicePdfService.GenerateInvoicePdf(claim.InvoiceId, claim.AttemptId, order, productVersions, clientDataVersion);
        }

        [FlowStep(order: 17, bpmnId: "CompleteInvoiceGeneration")]
        public Task<Invoice> CompleteInvoiceGeneration(InvoiceGenerationClaim claim, string storageUrl, IInvoiceGenerationRepository repository) =>
            repository.CompleteAsync(claim, storageUrl);

        [FlowStep(order: 18, bpmnId: "ReleaseFailedGeneration")]
        public Task ReleaseFailedGeneration(InvoiceGenerationClaim claim, IInvoiceGenerationRepository repository) =>
            repository.ReleaseAsync(claim);

        [FlowStep(order: 19, bpmnId: "DeleteFailedGenerationPdf")]
        public Task DeleteFailedGenerationPdf(InvoiceGenerationClaim claim, IInvoicePdfService pdfService) =>
            pdfService.DeleteGeneratedPdf(claim.InvoiceId, claim.AttemptId);

        [FlowStep(order: 20, bpmnId: "MapInvoiceResponse")]
        public InvoiceResponseDto MapToResponse(Invoice invoice)
        {
            return MappingConfig.MapToResponse(invoice);
        }
    }
}
