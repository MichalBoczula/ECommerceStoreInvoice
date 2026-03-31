using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;

namespace ECommerceStoreInvoice.Application.Services.Abstract
{
    public interface IOrderService
    {
        Task<Order> CreateOrder(Order order);
        Task<Order> GetOrderByOrderId(Guid orderId);
        Task<Order> UpdateOrder(Order order);
    }
}
