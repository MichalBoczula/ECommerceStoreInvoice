using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ECommerceStoreInvoice.Infrastructure.Persistence.Invoices
{
    internal sealed record InvoiceDocument
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public required Guid Id { get; init; }

        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public required Guid OrderId { get; init; }

        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public required Guid ClientDataVersionId { get; init; }

        public required string StorageUrl { get; init; }
        public required DateTime CreatedAt { get; init; }
    }
}
