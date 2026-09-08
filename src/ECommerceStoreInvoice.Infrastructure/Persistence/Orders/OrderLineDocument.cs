using MongoDB.Bson.Serialization.Attributes;

namespace ECommerceStoreInvoice.Infrastructure.Persistence.Orders
{
    internal sealed record OrderLineDocument
    {
        [BsonGuidRepresentation(MongoDB.Bson.GuidRepresentation.Standard)]
        public required Guid ProductVersionId { get; init; }

        public required int Quantity { get; init; }
    }
}
