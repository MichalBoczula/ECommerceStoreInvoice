using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;

namespace ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects
{
    public record class OrderLine
    {
        public Guid ProductVersionId { get; init; }
        public string Name { get; init; }
        public string Brand { get; init; }
        public Money UnitPrice { get; init; }
        public int Quantity { get; init; }
        public Money Total { get; init; }

        public OrderLine(
            Guid productVersionId,
            string name,
            string brand,
            Money unitPrice,
            int quantity)
        {
            ProductVersionId = productVersionId;
            Name = name;
            Brand = brand;
            UnitPrice = unitPrice;
            Quantity = quantity;
            Total = new(UnitPrice.Amount * Quantity, UnitPrice.Currency);
        }
    }
}
