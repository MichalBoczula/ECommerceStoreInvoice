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

    [Documentation("inv.flow.create-invoice-for-order", "Verify paid-order ownership, reserve one invoice per order, fence generation attempts, complete only after PDF creation and preserve ambiguous completion artifacts.")]

    internal sealed class CreateInvoiceForOrderDescriptor : FlowDescriberBase<CreateInvoiceForOrder>
    {
        [FlowStep(order: 1, bpmnId: "ValidateClientId", description: "Validate the client identifier with the registered GUID policy.")]
        public async Task<ValidationResult> ValidateClientId(Guid clientId, IValidationPolicy<Guid> guidValidationPolicy)
        {
            return await guidValidationPolicy.Validate(clientId);
        }

        [FlowStep(order: 2, bpmnId: "ValidateOrderId", description: "Validate the order identifier with the registered GUID policy.")]
        public async Task<ValidationResult> ValidateOrderId(Guid orderId, IValidationPolicy<Guid> guidValidationPolicy)
        {
            return await guidValidationPolicy.Validate(orderId);
        }

        [FlowStep(order: 3, bpmnId: "ThrowValidationExceptionIfIdInvalid", description: "Reject invalid client or order identifiers before reservation or PDF generation.")]
        public void ThrowValidationExceptionIfIdInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 4, bpmnId: "LoadOrderWithProductVersions", description: "Load the order joined to its stored immutable product versions.")]
        public async Task<(Order Order, IReadOnlyCollection<ProductVersion> ProductVersions)?> LoadOrderWithProductVersions(
            Guid orderId,
            IOrderRepository orderRepository)
        {
            return await orderRepository.GetOrderWithProductVersionsById(orderId);
        }

        [FlowStep(order: 5, bpmnId: "IsOrderExists", description: "Reject when the selected order does not exist.")]
        public void ThrowNotFoundExceptionIfOrderMissing(
            Guid orderId,
            (Order Order, IReadOnlyCollection<ProductVersion> ProductVersions)? orderWithProductVersions)
        {
            if (orderWithProductVersions is null)
            {
                throw new ResourceNotFoundException(nameof(Order), orderId, $"Order with id '{orderId}' was not found.");
            }
        }

        [FlowStep(order: 6, bpmnId: "IsOrderOwnedByClient", description: "Reject ownership mismatch as not found without exposing another client order.")]
        public void ThrowNotFoundExceptionIfOrderOwnedByAnotherClient(Guid clientId, Guid orderId, Order order)
        {
            if (order.ClientId != clientId)
                throw new ResourceNotFoundException(nameof(Order), orderId, $"Order with id '{orderId}' was not found.");
        }

        [FlowStep(order: 7, bpmnId: "LoadInvoiceByOrderId", description: "Read completed invoice metadata by its order identity.")]
        public async Task<Invoice?> LoadInvoiceByOrderId(Guid orderId, IInvoiceRepository invoiceRepository)
        {
            return await invoiceRepository.GetInvoiceByOrderId(orderId);
        }

        [FlowStep(order: 8, bpmnId: "IsInvoiceAlreadyExists", description: "Reject a published duplicate invoice for the same order.")]
        public void ThrowAlreadyExistsExceptionIfInvoiceAlreadyExists(Guid orderId, Invoice? existingInvoice)
        {
            if (existingInvoice is not null)
            {
                throw new ResourceAlreadyExistsException(nameof(Invoice), orderId, $"Invoice for order '{orderId}' already exists.");
            }
        }

        [FlowStep(order: 9, bpmnId: "ValidateOrderStatus", description: "Require a paid order before claiming invoice generation.")]
        public async Task<ValidationResult> ValidateOrderStatus(
            Order order,
            IValidationPolicy<InvoiceOrderStatusValidationContext> createInvoiceValidationPolicy)
        {
            return await createInvoiceValidationPolicy.Validate(new InvoiceOrderStatusValidationContext(order));
        }

        [FlowStep(order: 10, bpmnId: "IsOrderStatusValid", description: "Reject the flow with the complete registered validation errors when order status invalid.")]
        public void ThrowValidationExceptionIfOrderStatusInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 11, bpmnId: "LoadClientDataVersion", description: "Load the latest client version for this invoice while preserving existing invoice references.")]
        public Task<ClientDataVersionResponseDto?> LoadClientDataVersion(Guid clientId, IClientDataVersionService service) =>
            service.GetByClientId(clientId);

        [FlowStep(order: 12, bpmnId: "CreateInvoiceReservation", description: "Construct the invoice reservation with the paid order and selected historical client version.")]
        public Invoice CreateInvoiceReservation(Guid orderId, Guid clientDataVersionId) =>
            new(orderId, clientDataVersionId, string.Empty);

        [FlowStep(order: 13, bpmnId: "CreateGenerationAttemptId", description: "Allocate a fresh generation attempt identity for reservation fencing and artifact ownership.")]
        public Guid CreateGenerationAttemptId() => Guid.NewGuid();

        [FlowStep(order: 14, bpmnId: "TryClaimInvoiceGeneration", description: "Atomically claim a new, failed or expired reservation with the current attempt and lease.")]
        public Task<InvoiceGenerationClaim?> TryClaimInvoiceGeneration(Invoice invoice, Guid attemptId, IInvoiceGenerationRepository repository) =>
            repository.TryClaimAsync(invoice, attemptId);

        [FlowStep(order: 15, bpmnId: "IsGenerationClaimAvailable", description: "Reject generation when another active attempt or completed invoice owns the reservation.")]
        public void ThrowAlreadyExistsExceptionIfClaimMissing(Guid orderId, InvoiceGenerationClaim? claim)
        {
            if (claim is null)
                throw new ResourceAlreadyExistsException(nameof(Invoice), orderId, $"Invoice generation for order '{orderId}' is already in progress or completed.");
        }

        [FlowStep(order: 16, bpmnId: "GenerateInvoicePdf", description: "Generate the real PDF under this invoice and attempt identity using order snapshots and the selected client version.")]
        public async Task<string> GenerateInvoicePdf(
            InvoiceGenerationClaim claim,
            Order order,
            IReadOnlyCollection<ProductVersion> productVersions,
            ClientDataVersionResponseDto? clientDataVersion,
            IInvoicePdfService invoicePdfService)
        {
            return await invoicePdfService.GenerateInvoicePdf(claim.InvoiceId, claim.AttemptId, order, productVersions, clientDataVersion);
        }

        [FlowStep(order: 17, bpmnId: "CompleteInvoiceGeneration", description: "Conditionally publish the PDF location only for the current invoice generation attempt.")]
        public Task<Invoice> CompleteInvoiceGeneration(InvoiceGenerationClaim claim, string storageUrl, IInvoiceGenerationRepository repository) =>
            repository.CompleteAsync(claim, storageUrl);

        [FlowStep(order: 18, bpmnId: "ReleaseFailedGeneration", description: "Release only the failing attempt claim; an old attempt cannot release a replacement claim.")]
        public Task ReleaseFailedGeneration(InvoiceGenerationClaim claim, IInvoiceGenerationRepository repository) =>
            repository.ReleaseAsync(claim);

        [FlowStep(order: 19, bpmnId: "DeleteFailedGenerationPdf", description: "Delete only the failed attempt artifact when completion was not attempted; preserve ambiguous completion files.")]
        public Task DeleteFailedGenerationPdf(InvoiceGenerationClaim claim, IInvoicePdfService pdfService) =>
            pdfService.DeleteGeneratedPdf(claim.InvoiceId, claim.AttemptId);

        [FlowStep(order: 20, bpmnId: "MapInvoiceResponse", description: "Return the complete create invoice for order response from the domain values; this description is not execution tracing.")]
        public InvoiceResponseDto MapToResponse(Invoice invoice)
        {
            return MappingConfig.MapToResponse(invoice);
        }
    }
}
