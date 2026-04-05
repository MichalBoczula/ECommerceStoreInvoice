using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Infrastructure.Context;
using ECommerceStoreInvoice.Infrastructure.Mapping;
using ECommerceStoreInvoice.Infrastructure.Persistence.ClientDataVersions;
using ECommerceStoreInvoice.Infrastructure.Persistence.Orders;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

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

        public async Task<(Order? Order, ClientDataVersion? ClientDataVersion)> GetOrderWithLatestClientDataVersion(Guid clientId, Guid orderId)
        {
            var clientDataVersionsCollectionName = _context.ClientDataVersions.CollectionNamespace.CollectionName;

            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument
                {
                    { "_id", orderId },
                    { "clientId", clientId }
                }),
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", clientDataVersionsCollectionName },
                    { "let", new BsonDocument("clientId", "$clientId") },
                    { "pipeline", new BsonArray
                        {
                            new BsonDocument("$match", new BsonDocument("$expr", new BsonDocument("$eq", new BsonArray { "$clientId", "$$clientId" }))),
                            new BsonDocument("$sort", new BsonDocument("createdAt", -1)),
                            new BsonDocument("$limit", 1)
                        }
                    },
                    { "as", "clientDataVersions" }
                })
            };

            var resultDocument = await _context.Orders
                .Aggregate<BsonDocument>(pipeline)
                .FirstOrDefaultAsync();

            if (resultDocument is null)
            {
                return (null, null);
            }

            var orderDataDocument = resultDocument.DeepClone().AsBsonDocument;
            orderDataDocument.Remove("clientDataVersions");

            var orderDocument = BsonSerializer.Deserialize<OrderDocument>(orderDataDocument);
            var order = OrderMapping.MapToDomain(orderDocument);

            var clientDataVersion = resultDocument.TryGetValue("clientDataVersions", out var clientDataVersionsValue) &&
                                    clientDataVersionsValue.AsBsonArray.Any()
                ? ClientDataVersionMapping.MapToDomain(BsonSerializer.Deserialize<ClientDataVersionDocument>(clientDataVersionsValue.AsBsonArray[0].AsBsonDocument))
                : null;

            return (order, clientDataVersion);
        }

        public async Task<IReadOnlyCollection<Order>> GetOrdersByClientId(Guid clientId)
        {
            var orderDocuments = await _context.Orders
                .Find(x => x.ClientId == clientId)
                .ToListAsync();

            return orderDocuments
                .Select(OrderMapping.MapToDomain)
                .ToList();
        }

        public async Task<Order> UpdateOrder(Order order)
        {
            var orderDocument = OrderMapping.MapToDocument(order);

            var result = await _context.Orders.ReplaceOneAsync(
                x => x.Id == order.Id,
                orderDocument);

            if (result.MatchedCount == 0)
                throw new InvalidOperationException($"Order with id '{order.Id}' was not found.");

            return order;
        }
    }
}
