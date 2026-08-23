namespace ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts
{
    public sealed record ShoppingCartLineResponseDto
    {
        public required Guid ProductId { get; init; }
        public required int Quantity { get; init; }
    }
}