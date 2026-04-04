using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Descriptors.Orders
{
    internal sealed record GetOrdersByClientId;

    internal sealed class GetOrdersByClientIdDescriptor : FlowDescriberBase<GetOrdersByClientId>
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

        [FlowStep(order: 3, bpmnId: "LoadOrders")]
        public async Task<IReadOnlyCollection<Order>> LoadOrders(Guid clientId, IOrderRepository orderRepository)
        {
            return await orderRepository.GetOrdersByClientId(clientId);
        }

        [FlowStep(order: 4, bpmnId: "MapOrdersResponse")]
        public IReadOnlyCollection<OrderResponseDto> MapToResponse(IReadOnlyCollection<Order> orders)
        {
            return orders.Select(MappingConfig.MapToResponse).ToList();
        }
    }
}
