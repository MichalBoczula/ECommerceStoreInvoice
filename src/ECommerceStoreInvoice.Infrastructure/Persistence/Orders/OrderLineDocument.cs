using MongoDB.Bson.Serialization.Attributes;

namespace ECommerceStoreInvoice.Infrastructure.Persistence.Orders
{
    internal sealed record OrderLineDocument
    {
        [BsonGuidRepresentation(MongoDB.Bson.GuidRepresentation.Standard)]
        public required Guid ProductVersionId { get; init; }
        public required string Name { get; init; }
        public required string Brand { get; init; }
        public required decimal UnitPriceAmount { get; init; }
        public required string UnitPriceCurrency { get; init; }
        public required int Quantity { get; init; }
        public required decimal TotalAmount { get; init; }
        public required string TotalCurrency { get; init; }
    }
}
