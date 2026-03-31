using ECommerceStoreInvoice.Application.Common.RequestsDto.Orders;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Application.Services.Abstract;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Services.Concrete
{
    internal sealed class OrderService(
        IOrderRepository orderRepository)
        : IOrderService
    {
        public async Task<OrderResponseDto> CreateOrder(CreateOrderRequestDto request)
        {
            var order = MappingConfig.MapToDomain(request);
            var createdOrder = await orderRepository.CreateOrder(order);

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
