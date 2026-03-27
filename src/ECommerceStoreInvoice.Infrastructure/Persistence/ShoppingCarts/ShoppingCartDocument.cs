using MongoDB.Bson.Serialization.Attributes;

namespace ECommerceStoreInvoice.Infrastructure.Persistence.ShoppingCarts
{
    internal sealed record ShoppingCartDocument
    {
        [BsonId]
        public required Guid Id { get; init; }
        public required Guid ClientId { get; init; }
        public required DateTime CreatedAt { get; init; }
        public required DateTime UpdatedAt { get; init; }
        public required IReadOnlyCollection<ShoppingCartLineDocument> Lines { get; init; }
    }
}
