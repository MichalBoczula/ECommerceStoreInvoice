namespace ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories
{
    public interface IOrderRepository
    {
        Task<Order> CreateOrder(Order order);
        Task<Order?> GetOrderByOrderId(Guid orderId);
        Task<Order> UpdateOrder(Guid orderId, Order order);
    }
}
