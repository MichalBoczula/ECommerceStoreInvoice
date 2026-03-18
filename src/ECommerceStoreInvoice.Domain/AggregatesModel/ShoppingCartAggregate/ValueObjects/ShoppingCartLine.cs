using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;

namespace ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects
{
    public sealed class ShoppingCartLine
    {
        public Guid ProductVersionId { get; }
        public string Name { get; private set; }
        public string Brand { get; private set; }
        public Money UnitPrice { get; private set; }
        public int Quantity { get; private set; }

        public Money Total => new(UnitPrice.Amount * Quantity, UnitPrice.Currency);

        public ShoppingCartLine(
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
        }

        public void Increase(int quantity)
        {
            Quantity += quantity;
        }

        public void ChangeQuantity(int quantity)
        {
            Quantity = quantity;
        }

        public void UpdatePrice(Money newUnitPrice)
        {
            UnitPrice = newUnitPrice;
        }
    }
}
