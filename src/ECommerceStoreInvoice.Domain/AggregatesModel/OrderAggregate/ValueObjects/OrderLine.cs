namespace ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects
{
    public sealed class OrderLine
    {
        public Guid ProductVersionId { get; }
        public string Name { get; }
        public string Brand { get; }
        public int Quantity { get; }
        public decimal UnitPriceAmount { get; }
        public decimal TotalAmount => UnitPriceAmount * Quantity;
    }
}
