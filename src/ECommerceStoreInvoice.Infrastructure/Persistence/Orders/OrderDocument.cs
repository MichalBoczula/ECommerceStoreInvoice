using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ECommerceStoreInvoice.Infrastructure.Persistence.Orders
{
    internal sealed record OrderDocument
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public required Guid Id { get; init; }

        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public required Guid ClientId { get; init; }

        public required IReadOnlyCollection<OrderLineDocument> Lines { get; init; }
        public required DateTime CreatedAt { get; init; }
        public required DateTime UpdatedAt { get; init; }
        public required OrderStatus Status { get; init; }
        public required decimal TotalAmount { get; init; }
        public required string TotalCurrency { get; init; }
    }
}
