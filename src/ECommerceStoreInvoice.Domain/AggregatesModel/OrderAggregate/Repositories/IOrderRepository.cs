using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;

namespace ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories
{
    public interface IOrderRepository
    {
        Task<Order> CreateOrder(Order order);
        Task<IReadOnlyCollection<Order>> GetOrdersByClientId(Guid clientId);
        Task<Order?> GetOrderByOrderId(Guid orderId);
        Task<Order> UpdateOrder(Order order);
        Task<(Order Order, IReadOnlyCollection<ProductVersion> ProductVersions)?> GetOrderWithProductVersionsById(Guid orderId);
        Task<IReadOnlyCollection<(Order Order, IReadOnlyCollection<ProductVersion> ProductVersions)>> GetOrdersWithProductVersionsByClientId(Guid clientId);
    }
}
