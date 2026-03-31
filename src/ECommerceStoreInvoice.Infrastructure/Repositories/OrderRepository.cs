using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Infrastructure.Context;
using ECommerceStoreInvoice.Infrastructure.Mapping;
using MongoDB.Driver;

namespace ECommerceStoreInvoice.Infrastructure.Repositories
{
    internal class OrderRepository(MongoDbContext context) : IOrderRepository
    {
        private readonly MongoDbContext _context = context;

        public async Task<Order> CreateOrder(Order order)
        {
            var orderDocument = OrderMapping.MapToDocument(order);

            await _context.Orders.InsertOneAsync(orderDocument);

            return order;
        }

        public async Task<Order?> GetOrderByOrderId(Guid orderId)
        {
            var orderDocument = await _context.Orders
                .Find(x => x.Id == orderId)
                .FirstOrDefaultAsync();

            if (orderDocument is null)
                return null;

            return OrderMapping.MapToDomain(orderDocument);
        }

        public async Task<Order> UpdateOrder(Guid orderId, Order order)
        {
            var orderDocument = OrderMapping.MapToDocument(order);

            var result = await _context.Orders.ReplaceOneAsync(
                x => x.Id == orderId,
                orderDocument);

            if (result.MatchedCount == 0)
                throw new InvalidOperationException($"Order with id '{orderId}' was not found.");

            return order;
        }
    }
}
