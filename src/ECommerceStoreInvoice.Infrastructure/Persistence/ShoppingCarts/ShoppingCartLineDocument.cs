using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ECommerceStoreInvoice.Infrastructure.Persistence.ShoppingCarts
{
    internal sealed record ShoppingCartLineDocument
    {
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public required Guid ProductId { get; init; }
        public required int Quantity { get; init; }
    }
}
