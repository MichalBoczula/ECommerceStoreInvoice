namespace ECommerceStoreInvoice.Infrastructure.Persistence.ProductVersions
{
    internal sealed record ProductVersionDocument
    {
        public required Guid Id { get; init; }
        public required bool IsActive { get; init; }
        public required DateTime CreatedAt { get; init; }
        public DateTime? DeactivatedAt { get; init; }
        public required Guid ProductId { get; init; }
        public required decimal PriceAmount { get; init; }
        public required string PriceCurrency { get; init; }
        public required string Name { get; init; }
        public required string Brand { get; init; }
    }
}
