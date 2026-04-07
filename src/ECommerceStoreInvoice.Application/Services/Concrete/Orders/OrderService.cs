using ECommerceStoreInvoice.Application.Common.RequestsDto.Orders;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;
using ECommerceStoreInvoice.Application.Descriptors.Orders;
using ECommerceStoreInvoice.Application.Services.Abstract.Orders;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;

namespace ECommerceStoreInvoice.Application.Services.Concrete.Orders
{
    internal sealed class OrderService(
        IOrderRepository orderRepository,
        IProductVersionRepository productVersionRepository,
        IShoppingCartRepository shoppingCartRepository,
        IValidationPolicy<Guid> guidValidationPolicy,
        IValidationPolicy<Order> orderValidationPolicy,
        IValidationPolicy<(Order order, OrderStatus newStatus)> updateOrderValidationPolicy)
        : IOrderService
    {
        public async Task<OrderResponseDto> CreateOrder(Guid clientId)
        {
            var descriptor = new CreateOrderDescriptor();

            var validationResult = await descriptor.ValidateClientId(clientId, guidValidationPolicy);
            descriptor.ThrowValidationExceptionIfClientIdInvalid(validationResult);

            var shoppingCart = await descriptor.LoadShoppingCart(clientId, shoppingCartRepository);
            descriptor.ThrowNotFoundExceptionIfShoppingCartMissing(clientId, shoppingCart);

            var productVersions = await descriptor.CreateProductVersions(shoppingCart!, productVersionRepository);

            var order = descriptor.MapToDomain(shoppingCart!, productVersions);
            validationResult = await descriptor.ValidateOrder(order, orderValidationPolicy);
            descriptor.ThrowValidationExceptionIfOrderInvalid(validationResult);

            var createdOrder = await descriptor.SaveOrder(order, orderRepository);
            descriptor.ClearShoppingCart(shoppingCart!);
            await descriptor.SaveShoppingCart(shoppingCart!, shoppingCartRepository);

            return descriptor.MapToResponse(createdOrder);
        }

        public async Task<IReadOnlyCollection<OrderResponseDto>> GetOrdersByClientId(Guid clientId)
        {
            var descriptor = new GetOrdersByClientIdDescriptor();

            var validationResult = await descriptor.ValidateClientId(clientId, guidValidationPolicy);
            descriptor.ThrowValidationExceptionIfClientIdInvalid(validationResult);

            var orders = await descriptor.LoadOrders(clientId, orderRepository);

            return descriptor.MapToResponse(orders);
        }

        public async Task<OrderResponseDto> GetOrderByOrderId(Guid orderId)
        {
            var descriptor = new GetOrderByIdDescriptor();

            var validationResult = await descriptor.ValidateOrderId(orderId, guidValidationPolicy);
            descriptor.ThrowValidationExceptionIfOrderIdInvalid(validationResult);

            var order = await descriptor.LoadOrder(orderId, orderRepository);
            descriptor.ThrowNotFoundExceptionIfOrderMissing(orderId, order);

            return descriptor.MapToResponse(order!);
        }

        public async Task<OrderResponseDto> UpdateOrderStatus(Guid orderId, UpdateOrderStatusRequestDto request)
        {
            var descriptor = new UpdateOrderStatusDescriptor();

            var validationResult = await descriptor.ValidateOrderId(orderId, guidValidationPolicy);
            descriptor.ThrowValidationExceptionIfOrderIdInvalid(validationResult);

            var order = await descriptor.LoadOrder(orderId, orderRepository);
            descriptor.ThrowNotFoundExceptionIfOrderMissing(orderId, order);

            var newStatus = descriptor.ParseStatus(request.Status);
            validationResult = await descriptor.ValidateStatusTransition(order!, newStatus, updateOrderValidationPolicy);
            descriptor.ThrowValidationExceptionIfStatusTransitionInvalid(validationResult);
            descriptor.ChangeOrderStatus(order!, newStatus);

            var updatedOrder = await descriptor.SaveOrder(order!, orderRepository);

            return descriptor.MapToResponse(updatedOrder);
        }
    }
}
