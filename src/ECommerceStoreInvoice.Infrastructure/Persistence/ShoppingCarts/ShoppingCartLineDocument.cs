namespace ECommerceStoreInvoice.Infrastructure.Persistence.ShoppingCarts
{
    internal sealed record ShoppingCartLineDocument
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
