namespace ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts
{
    public sealed record UpdateShoppingCartRequestDto
    {
        public required Guid Id { get; init; }
        public required Guid ClientId { get; init; }
        public required DateTime CreatedAt { get; init; }
        public required DateTime UpdatedAt { get; init; }
        public required IReadOnlyCollection<ShoppingCartLineRequestDto> Lines { get; init; }
    }
}
