namespace ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts
{
    public sealed record ShoppingCartResponseDto
    {
        public required Guid Id { get; init; }
        public required Guid ClientId { get; init; }
        public required DateTime CreatedAt { get; init; }
        public required DateTime UpdatedAt { get; init; }
        public required IReadOnlyCollection<ShoppingCartLineResponseDto> Lines { get; init; }
    }
}