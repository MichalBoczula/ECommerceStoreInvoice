namespace ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts
{
    /// <summary>Complete stored cart with ordered lines. All response members are emitted, including an empty lines array.</summary>
    public sealed record ShoppingCartResponseDto
    {
        /// <summary>Shopping cart UUID.</summary>
        public required Guid Id { get; init; }
        /// <summary>Client UUID owning the cart.</summary>
        public required Guid ClientId { get; init; }
        /// <summary>UTC cart creation timestamp.</summary>
        public required DateTime CreatedAt { get; init; }
        /// <summary>UTC timestamp of the last cart update, initialized on creation.</summary>
        public required DateTime UpdatedAt { get; init; }
        /// <summary>Complete ordered stored cart lines; empty for a new or cleared cart.</summary>
        public required IReadOnlyCollection<ShoppingCartLineResponseDto> Lines { get; init; }
    }
}