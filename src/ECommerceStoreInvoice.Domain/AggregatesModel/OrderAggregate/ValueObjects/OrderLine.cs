namespace ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects
{
    public readonly record struct OrderLine
    {
        public Guid ProductVersionId { get; init; }
        public string Name { get; init; }
        public string Brand { get; init; }
        public int Quantity { get; init; }
        public decimal UnitPriceAmount { get; init; }
        public decimal TotalAmount { get; init; }

        public OrderLine(Guid productVersionId, string name, string brand, int quantity, decimal unitPriceAmount, decimal totalAmount)
        {
            ProductVersionId = productVersionId;
            Name = name;
            Brand = brand;
            Quantity = quantity;
            UnitPriceAmount = unitPriceAmount;
            TotalAmount = totalAmount;
        }
    }
}
