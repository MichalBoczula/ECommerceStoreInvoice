using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Descriptors.Orders
{
    internal sealed record GetOrderById;

    [Documentation("inv.flow.get-order-by-id", "Read a complete order joined to its immutable snapshots without querying the live catalog.")]

    internal sealed class GetOrderByIdDescriptor : FlowDescriberBase<GetOrderById>
    {
        [FlowStep(order: 1, bpmnId: "ValidateOrderId", description: "Validate the order identifier with the registered GUID policy.")]
        public async Task<ValidationResult> ValidateOrderId(Guid orderId, IValidationPolicy<Guid> guidValidationPolicy)
        {
            return await guidValidationPolicy.Validate(orderId);
        }

        [FlowStep(order: 2, bpmnId: "IsOrderIdValid", description: "Reject the flow with the complete registered validation errors when order id invalid.")]
        public void ThrowValidationExceptionIfOrderIdInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 3, bpmnId: "LoadOrderWithProductVersions", description: "Load the order joined to its stored immutable product versions.")]
        public async Task<(Order Order, IReadOnlyCollection<ProductVersion> ProductVersions)?> LoadOrderWithProductVersions(
            Guid orderId,
            IOrderRepository orderRepository)
        {
            return await orderRepository.GetOrderWithProductVersionsById(orderId);
        }

        [FlowStep(order: 4, bpmnId: "IsOrderExists", description: "Reject when the selected order does not exist.")]
        public void ThrowNotFoundExceptionIfOrderMissing(
            Guid orderId,
            (Order Order, IReadOnlyCollection<ProductVersion> ProductVersions)? orderWithProductVersions)
        {
            if (orderWithProductVersions is null)
            {
                throw new ResourceNotFoundException(nameof(Order), orderId, $"Order with id '{orderId}' was not found.");
            }
        }

        [FlowStep(order: 5, bpmnId: "MapOrderResponse", description: "Return the complete get order by id response from the domain values; this description is not execution tracing.")]
        public OrderResponseDto MapToResponse(
            (Order Order, IReadOnlyCollection<ProductVersion> ProductVersions) orderWithProductVersions)
        {
            return MappingConfig.MapToResponse(orderWithProductVersions);
        }
    }
}