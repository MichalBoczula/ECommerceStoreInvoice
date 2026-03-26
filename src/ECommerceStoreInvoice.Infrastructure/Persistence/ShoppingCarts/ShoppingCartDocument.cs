using MongoDB.Bson.Serialization.Attributes;

namespace ECommerceStoreInvoice.Infrastructure.Persistence.ShoppingCarts
{
    public sealed class ShoppingCartDocument
    {
        [BsonId]
        public Guid Id { get; set; }

        public Guid ClientId { get; set; }

        public List<ShoppingCartLineDocument> Lines { get; set; } = [];
    }
}
