namespace ECommerceStoreInvoice.Application.Common.ResponsesDto
{
    /// <summary>Immutable product snapshot used by an order, including its captured catalog price and currency.</summary>
    public sealed record ProductVersionResponseDto
    {
        /// <summary>Product snapshot UUID.</summary>
        public required Guid Id { get; init; }
        /// <summary>Stored snapshot activity flag as a JSON boolean.</summary>
        public required bool IsActive { get; init; }
        /// <summary>UTC snapshot creation timestamp.</summary>
        public required DateTime CreatedAt { get; init; }
        /// <summary>UTC deactivation timestamp, or null when the snapshot has not been deactivated.</summary>
        public DateTime? DeactivatedAt { get; init; }
        /// <summary>Original ProductsCatalog product UUID.</summary>
        public required Guid ProductId { get; init; }
        /// <summary>Captured decimal unit-price amount serialized as a JSON number.</summary>
        public required decimal PriceAmount { get; init; }
        /// <summary>Captured catalog currency text.</summary>
        public required string PriceCurrency { get; init; }
        /// <summary>Captured catalog product name.</summary>
        public required string Name { get; init; }
        /// <summary>Captured catalog brand.</summary>
        public required string Brand { get; init; }
    }
}
