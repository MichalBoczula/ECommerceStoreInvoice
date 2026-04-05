using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;

namespace ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories
{
    public interface IOrderRepository
    {
        Task<Order> CreateOrder(Order order);
        Task<IReadOnlyCollection<Order>> GetOrdersByClientId(Guid clientId);
        Task<Order?> GetOrderByOrderId(Guid orderId);
        Task<(Order? Order, ClientDataVersion? ClientDataVersion)> GetOrderWithLatestClientDataVersion(Guid clientId, Guid orderId);
        Task<Order> UpdateOrder(Order order);
    }
}
