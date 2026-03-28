namespace ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts
{
    public sealed record UpdateShoppingCartRequestDto
    {
        public required IReadOnlyCollection<ShoppingCartLineRequestDto> Lines { get; init; }
    }
}
