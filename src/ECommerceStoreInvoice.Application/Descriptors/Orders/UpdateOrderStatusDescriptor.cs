using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Descriptors.Orders
{
    internal sealed record UpdateOrderStatus;

    [Documentation("inv.flow.update-order-status", "Allow only Created to Paid or Cancelled; reject terminal/repeat transitions and real stale conditional writes without changing the winner.")]

    internal sealed class UpdateOrderStatusDescriptor : FlowDescriberBase<UpdateOrderStatus>
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

        [FlowStep(order: 5, bpmnId: "ParseStatus", description: "Parse status text with existing case-insensitive enum parsing; a parsed value still requires transition validation.")]
        [InlineRule("inv.rule.order-status-parse", "status", "Enum.TryParse cannot parse the supplied status text.", "Return the safe status validation error before changing the order.")]
        public OrderStatus ParseStatus(string status)
        {
            if (Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
            {
                return parsedStatus;
            }

            var validationResult = new ValidationResult();
            validationResult.AddValidationError(new ValidationError
            {
                Name = nameof(status),
                Entity = nameof(Order),
                Message = $"Order status '{status}' is invalid. Allowed values: {string.Join(", ", Enum.GetNames<OrderStatus>())}."
            });

            throw new ValidationException(validationResult);
        }

        [FlowStep(order: 6, bpmnId: "ValidateStatusTransition", description: "Apply the existing Created-only transition policy to the loaded order and requested target status.")]
        public async Task<ValidationResult> ValidateStatusTransition(
            Order order,
            OrderStatus newStatus,
            IValidationPolicy<(Order order, OrderStatus newStatus)> updateOrderValidationPolicy)
        {
            return await updateOrderValidationPolicy.Validate((order, newStatus));
        }

        [FlowStep(order: 7, bpmnId: "IsStatusTransitionValid", description: "Reject the flow with the complete registered validation errors when status transition invalid.")]
        public void ThrowValidationExceptionIfStatusTransitionInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 8, bpmnId: "ChangeOrderStatus", description: "Apply the accepted status and update timestamp to the loaded order.")]
        public void ChangeOrderStatus(Order order, OrderStatus newStatus)
        {
            order.ChangeStatus(newStatus);
        }

        [FlowStep(order: 9, bpmnId: "SaveOrder", description: "Persist the order using the repository contract; a stale status replacement throws a conflict.")]
        public async Task<Order> SaveOrder(Order order, IOrderRepository orderRepository)
        {
            return await orderRepository.UpdateOrder(order);
        }

        [FlowStep(order: 10, bpmnId: "MapOrderResponse", description: "Return the complete update order status response from the domain values; this description is not execution tracing.")]
        public OrderResponseDto MapToResponse(
            Order order,
            IReadOnlyCollection<ProductVersion> productVersions)
        {
            return MappingConfig.MapToResponse(order, productVersions);
        }
    }
}