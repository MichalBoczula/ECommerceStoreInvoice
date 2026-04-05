using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Descriptors.Orders
{
    internal sealed record UpdateOrderStatus;

    internal sealed class UpdateOrderStatusDescriptor : FlowDescriberBase<UpdateOrderStatus>
    {
        [FlowStep(order: 1, bpmnId: "ValidateOrderId")]
        public async Task<ValidationResult> ValidateOrderId(Guid orderId, IValidationPolicy<Guid> guidValidationPolicy)
        {
            return await guidValidationPolicy.Validate(orderId);
        }

        [FlowStep(order: 2, bpmnId: "IsOrderIdValid")]
        public void ThrowValidationExceptionIfOrderIdInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 3, bpmnId: "LoadOrder")]
        public async Task<Order?> LoadOrder(Guid orderId, IOrderRepository orderRepository)
        {
            return await orderRepository.GetOrderByOrderId(orderId);
        }

        [FlowStep(order: 4, bpmnId: "IsOrderExists")]
        public void ThrowNotFoundExceptionIfOrderMissing(Guid orderId, Order? order)
        {
            if (order is null)
            {
                throw new ResourceNotFoundException(nameof(Order), orderId, $"Order with id '{orderId}' was not found.");
            }
        }

        [FlowStep(order: 5, bpmnId: "ParseStatus")]
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

        [FlowStep(order: 6, bpmnId: "ValidateStatusTransition")]
        public async Task<ValidationResult> ValidateStatusTransition(
            Order order,
            OrderStatus newStatus,
            IValidationPolicy<(Order order, OrderStatus newStatus)> updateOrderValidationPolicy)
        {
            return await updateOrderValidationPolicy.Validate((order, newStatus));
        }

        [FlowStep(order: 7, bpmnId: "IsStatusTransitionValid")]
        public void ThrowValidationExceptionIfStatusTransitionInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 8, bpmnId: "ChangeOrderStatus")]
        public void ChangeOrderStatus(Order order, OrderStatus newStatus)
        {
            order.ChangeOrderStatus(newStatus);
        }

        [FlowStep(order: 9, bpmnId: "SaveOrder")]
        public async Task<Order> SaveOrder(Order order, IOrderRepository orderRepository)
        {
            return await orderRepository.UpdateOrder(order);
        }

        [FlowStep(order: 10, bpmnId: "MapOrderResponse")]
        public OrderResponseDto MapToResponse(Order order)
        {
            return MappingConfig.MapToResponse(order);
        }
    }
}
