using MongoDB.Bson.Serialization.Attributes;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;

namespace ECommerceStoreInvoice.Infrastructure.Persistence.Orders
{
    internal sealed record OrderDocument
    {
        [BsonId]
        [BsonGuidRepresentation(MongoDB.Bson.GuidRepresentation.Standard)]
        public required Guid Id { get; init; }

        [BsonGuidRepresentation(MongoDB.Bson.GuidRepresentation.Standard)]
        public required Guid ClientId { get; init; }

        public required IReadOnlyCollection<OrderLineDocument> Lines { get; init; }

        public required DateTime CreatedAt { get; init; }

        public DateTime? UpdatedAt { get; init; }

        public required OrderStatus Status { get; init; }
    }
}