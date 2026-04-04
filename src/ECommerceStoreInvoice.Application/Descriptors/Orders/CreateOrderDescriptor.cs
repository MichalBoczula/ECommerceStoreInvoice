using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Descriptors.Orders
{
    internal sealed record CreateOrder;

    internal sealed class CreateOrderDescriptor : FlowDescriberBase<CreateOrder>
    {
        [FlowStep(order: 1, bpmnId: "LoadShoppingCart")]
        public async Task<ShoppingCart?> LoadShoppingCart(Guid clientId, IShoppingCartRepository shoppingCartRepository)
        {
            return await shoppingCartRepository.GetShoppingCartByClientId(clientId);
        }

        [FlowStep(order: 2, bpmnId: "IsShoppingCartExists")]
        public void ThrowNotFoundExceptionIfShoppingCartMissing(Guid clientId, ShoppingCart? shoppingCart)
        {
            if (shoppingCart is null)
            {
                throw new ResourceNotFoundException(nameof(LoadShoppingCart), clientId, nameof(ShoppingCart));
            }
        }

        [FlowStep(order: 3, bpmnId: "MapOrderDomain")]
        public Order MapToDomain(ShoppingCart shoppingCart)
        {
            return MappingConfig.MapToDomain(shoppingCart);
        }

        [FlowStep(order: 4, bpmnId: "ValidateOrder")]
        public async Task<ValidationResult> ValidateOrder(Order order, IValidationPolicy<Order> orderValidationPolicy)
        {
            return await orderValidationPolicy.Validate(order);
        }

        [FlowStep(order: 5, bpmnId: "IsOrderValid")]
        public void ThrowValidationExceptionIfOrderInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 6, bpmnId: "SaveOrder")]
        public async Task<Order> SaveOrder(Order order, IOrderRepository orderRepository)
        {
            return await orderRepository.CreateOrder(order);
        }

        [FlowStep(order: 7, bpmnId: "ClearShoppingCart")]
        public void ClearShoppingCart(ShoppingCart shoppingCart)
        {
            shoppingCart.Clear();
        }

        [FlowStep(order: 8, bpmnId: "SaveShoppingCart")]
        public async Task SaveShoppingCart(ShoppingCart shoppingCart, IShoppingCartRepository shoppingCartRepository)
        {
            await shoppingCartRepository.UpdateShoppingCart(shoppingCart);
        }

        [FlowStep(order: 9, bpmnId: "MapOrderResponse")]
        public OrderResponseDto MapToResponse(Order order)
        {
            return MappingConfig.MapToResponse(order);
        }
    }
}
