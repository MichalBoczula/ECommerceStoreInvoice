using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;

namespace ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.ValueObjects
{
    public readonly record struct OrderLineSnapshot
    {
        public Guid ProductVersionId { get; init; }
        public int Quantity { get; init; }
        public string Name { get; init; }
        public string Brand { get; init; }
        public Money Total { get; init; }
        public Money UnitPrice { get; init; }

        public OrderLineSnapshot(
            Guid productVersionId,
            int quantity,
            string name,
            string brand,
            Money unitPrice)
        {
            ProductVersionId = productVersionId;
            Quantity = quantity;
            Name = name;
            Brand = brand;
            UnitPrice = unitPrice;
            Total = CalculateTotal(unitPrice, quantity);
        }

        internal Money CalculateTotal(Money unitPrice, int quantity)
        {
            return new Money(unitPrice.Amount * quantity, unitPrice.Currency);
        }
    }
}
