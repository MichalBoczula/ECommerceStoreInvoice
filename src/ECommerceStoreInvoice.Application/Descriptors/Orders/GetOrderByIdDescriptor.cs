using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Descriptors.Orders
{
    internal sealed record GetOrderById;

    internal sealed class GetOrderByIdDescriptor : FlowDescriberBase<GetOrderById>
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

        [FlowStep(order: 5, bpmnId: "LoadProductVersions")]
        public async Task<IReadOnlyCollection<ProductVersion>> LoadProductVersions(
            Order order,
            IProductVersionRepository productVersionRepository)
        {
            var productVersionIds = order.Lines
                .Select(l => l.ProductVersionId)
                .Distinct()
                .ToList();

            if (productVersionIds.Count == 0)
            {
                return [];
            }

            return await productVersionRepository.GetProductVersionsByIds(productVersionIds);
        }

        [FlowStep(order: 6, bpmnId: "MapOrderResponse")]
        public OrderResponseDto MapToResponse(Order order, IReadOnlyCollection<ProductVersion> productVersions)
        {
            var productVersionDtos = productVersions
                .Select(MappingConfig.MapToResponse)
                .ToList();

            return MappingConfig.MapToResponse(order, productVersionDtos);
        }
    }
}