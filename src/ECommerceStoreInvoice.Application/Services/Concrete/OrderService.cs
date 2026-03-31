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
        public async Task<Order> CreateOrder(Order order)
        {
            return await orderRepository.CreateOrder(order);
        }

        public async Task<Order> GetOrderByOrderId(Guid orderId)
        {
            var order = await orderRepository.GetOrderByOrderId(orderId);

            if (order is null)
                throw new ResourceNotFoundException(nameof(Order), orderId, $"Order with id '{orderId}' was not found.");

            return order;
        }

        public async Task<Order> UpdateOrder(Order order)
        {
            var existingOrder = await orderRepository.GetOrderByOrderId(order.Id);

            if (existingOrder is null)
                throw new ResourceNotFoundException(nameof(Order), order.Id, $"Order with id '{order.Id}' was not found.");

            return await orderRepository.UpdateOrder(order);
        }
    }
}
