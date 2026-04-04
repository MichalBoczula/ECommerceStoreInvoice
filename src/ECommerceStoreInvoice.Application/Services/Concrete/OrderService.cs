using ECommerceStoreInvoice.Application.Common.RequestsDto.Orders;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;
using ECommerceStoreInvoice.Application.Descriptors.Orders;
using ECommerceStoreInvoice.Application.Services.Abstract;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;

namespace ECommerceStoreInvoice.Application.Services.Concrete
{
    internal sealed class OrderService(
        IOrderRepository orderRepository,
        IShoppingCartRepository shoppingCartRepository,
        IValidationPolicy<Order> orderValidationPolicy)
        : IOrderService
    {
        public async Task<OrderResponseDto> CreateOrder(Guid clientId)
        {
            var descriptor = new CreateOrderDescriptor();

            var shoppingCart = await descriptor.LoadShoppingCart(clientId, shoppingCartRepository);
            descriptor.ThrowNotFoundExceptionIfShoppingCartMissing(clientId, shoppingCart);

            var order = descriptor.MapToDomain(shoppingCart!);
            var validationResult = await descriptor.ValidateOrder(order, orderValidationPolicy);
            descriptor.ThrowValidationExceptionIfOrderInvalid(validationResult);

            var createdOrder = await descriptor.SaveOrder(order, orderRepository);
            descriptor.ClearShoppingCart(shoppingCart!);
            await descriptor.SaveShoppingCart(shoppingCart!, shoppingCartRepository);

            return descriptor.MapToResponse(createdOrder);
        }

        public async Task<OrderResponseDto> GetOrderByOrderId(Guid orderId)
        {
            var descriptor = new GetOrderByIdDescriptor();

            var order = await descriptor.LoadOrder(orderId, orderRepository);
            descriptor.ThrowNotFoundExceptionIfOrderMissing(orderId, order);

            return descriptor.MapToResponse(order!);
        }

        public async Task<OrderResponseDto> UpdateOrderStatus(Guid orderId, UpdateOrderStatusRequestDto request)
        {
            var descriptor = new UpdateOrderStatusDescriptor();

            var order = await descriptor.LoadOrder(orderId, orderRepository);
            descriptor.ThrowNotFoundExceptionIfOrderMissing(orderId, order);

            var newStatus = descriptor.ParseStatus(request.Status);
            descriptor.EnsureStatusTransitionAllowed(order!, newStatus);
            descriptor.ChangeOrderStatus(order!, newStatus);

            var updatedOrder = await descriptor.SaveOrder(order!, orderRepository);

            return descriptor.MapToResponse(updatedOrder);
        }
    }
}
