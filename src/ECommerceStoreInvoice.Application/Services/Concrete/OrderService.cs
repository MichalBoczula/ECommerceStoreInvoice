using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Application.Services.Abstract;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Services.Concrete
{
    internal sealed class OrderService(
        IOrderRepository orderRepository,
        IShoppingCartRepository shoppingCartRepository)
        : IOrderService
    {
        public async Task<OrderResponseDto> CreateOrder(Guid clientId)
        {
            var shoppingCart = await shoppingCartRepository.GetShoppingCartByClientId(clientId);

            if (shoppingCart is null)
                throw new ResourceNotFoundException(nameof(shoppingCartRepository.GetShoppingCartByClientId), clientId, "Shopping cart was not found.");

            var order = MappingConfig.MapToDomain(shoppingCart);
            var createdOrder = await orderRepository.CreateOrder(order);
            shoppingCart.Clear();
            await shoppingCartRepository.UpdateShoppingCart(shoppingCart);

            return MappingConfig.MapToResponse(createdOrder);
        }

        public async Task<OrderResponseDto> GetOrderByOrderId(Guid orderId)
        {
            var order = await orderRepository.GetOrderByOrderId(orderId);

            if (order is null)
                throw new ResourceNotFoundException(nameof(Order), orderId, $"Order with id '{orderId}' was not found.");

            return MappingConfig.MapToResponse(order);
        }
    }
}
