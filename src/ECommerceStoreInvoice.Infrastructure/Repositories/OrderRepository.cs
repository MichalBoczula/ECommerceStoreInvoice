using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Infrastructure.Context;
using ECommerceStoreInvoice.Infrastructure.Mapping;
using ECommerceStoreInvoice.Infrastructure.Persistence.Orders;
using ECommerceStoreInvoice.Infrastructure.Persistence.ProductVersions;
using MongoDB.Driver;

namespace ECommerceStoreInvoice.Infrastructure.Repositories
{
    internal sealed class OrderRepository(MongoDbContext context) : IOrderRepository
    {
        private readonly MongoDbContext _context = context;

        public async Task<Order> CreateOrder(Order order)
        {
            var document = OrderMapping.MapToDocument(order);
            await _context.Orders.InsertOneAsync(document);
            return order;
        }

        public async Task<Order> UpdateOrder(Order order)
        {
            var document = OrderMapping.MapToDocument(order);
            await _context.Orders.ReplaceOneAsync(x => x.Id == order.Id, document);
            return order;
        }

        public async Task<Order?> GetOrderByOrderId(Guid orderId)
        {
            var document = await _context.Orders
                .Find(x => x.Id == orderId)
                .FirstOrDefaultAsync();

            return document is null ? null : OrderMapping.MapToDomain(document);
        }

        public async Task<IReadOnlyCollection<Order>> GetOrdersByClientId(Guid clientId)
        {
            var documents = await _context.Orders
                .Find(x => x.ClientId == clientId)
                .ToListAsync();

            return documents.Select(OrderMapping.MapToDomain).ToList();
        }

        public async Task<(Order Order, IReadOnlyCollection<ProductVersion> ProductVersions)?> GetOrderWithProductVersionsById(Guid orderId)
        {
            var pipeline = _context.Orders.Aggregate()
                .Match(o => o.Id == orderId)
                .Lookup<OrderDocument, ProductVersionDocument, OrderWithProductsDocument>(
                    foreignCollection: _context.ProductVersions,
                    localField: o => o.Lines.Select(l => l.ProductVersionId),
                    foreignField: pv => pv.Id,
                    @as: result => result.ProductVersions);

            var document = await pipeline.FirstOrDefaultAsync();

            return document is null ? null : OrderMapping.MapToDomain(document);
        }

        public async Task<IReadOnlyCollection<(Order Order, IReadOnlyCollection<ProductVersion> ProductVersions)>> GetOrdersWithProductVersionsByClientId(Guid clientId)
        {
            var pipeline = _context.Orders.Aggregate()
                .Match(o => o.ClientId == clientId)
                .Lookup<OrderDocument, ProductVersionDocument, OrderWithProductsDocument>(
                    foreignCollection: _context.ProductVersions,
                    localField: o => o.Lines.Select(l => l.ProductVersionId),
                    foreignField: pv => pv.Id,
                    @as: result => result.ProductVersions);

            var documents = await pipeline.ToListAsync();

            return documents.Select(OrderMapping.MapToDomain).ToList();
        }
    }
}