using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Infrastructure.Persistence.ProductVersions;
using MongoDB.Bson.Serialization.Attributes;

namespace ECommerceStoreInvoice.Infrastructure.Persistence.Orders
{
    internal sealed class OrderWithProductsDocument
    {
        [BsonId]
        public Guid Id { get; set; }

        public Guid ClientId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public OrderStatus Status { get; set; }

        public List<OrderLineDocument> Lines { get; set; } = [];

        public List<ProductVersionDocument> ProductVersions { get; set; } = [];
    }
}