namespace ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts
{
    public sealed record ShoppingCartLineRequestDto
    {
        public required Guid ProductId { get; init; }
        public required int Quantity { get; init; }
    }
}
