namespace ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts
{
    public sealed record ShoppingCartLineResponseDto
    {
        public required string Name { get; init; }
        public required string Brand { get; init; }
        public required decimal UnitPriceAmount { get; init; }
        public required string UnitPriceCurrency { get; init; }
        public required int Quantity { get; init; }
        public required decimal TotalAmount { get; init; }
        public required string TotalCurrency { get; init; }
    }
}
