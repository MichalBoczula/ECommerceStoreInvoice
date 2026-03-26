namespace ECommerceStoreInvoice.Infrastructure.Persistence.ShoppingCarts
{
    public sealed class ShoppingCartLineDocument
    {
        public Guid ProductVersionId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Brand { get; set; } = string.Empty;

        public decimal UnitPriceAmount { get; set; }

        public string UnitPriceCurrency { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal TotalAmount { get; set; }

        public string TotalCurrency { get; set; } = string.Empty;
    }
}
