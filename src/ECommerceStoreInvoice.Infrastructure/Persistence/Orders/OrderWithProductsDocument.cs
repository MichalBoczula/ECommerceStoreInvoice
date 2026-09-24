using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Infrastructure.Persistence.ProductVersions;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace ECommerceStoreInvoice.Infrastructure.Persistence.Orders
{
    internal sealed class OrderWithProductsDocument
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; }

        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid ClientId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public OrderStatus Status { get; set; }

        public List<OrderLineDocument> Lines { get; set; } = [];

        public List<ProductVersionDocument> ProductVersions { get; set; } = [];
    }
}
