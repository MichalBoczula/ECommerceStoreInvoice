namespace ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects
{
    public record ShoppingCartLine
    {
        public Guid ProductId { get; init; }
        public int Quantity { get; private set; }

        public ShoppingCartLine(Guid productId, int quantity)
        {
            ProductId = productId;
            Quantity = quantity;
        }

        public void ChangeQuantity(int quantity)
        {
            Quantity = quantity;
        }
    }
}