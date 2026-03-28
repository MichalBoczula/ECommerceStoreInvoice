using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;

namespace ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects
{
    public record ShoppingCartLine
    {
        public string Name { get; init; }
        public string Brand { get; init; }
        public Money UnitPrice { get; init; }
        public int Quantity { get; private set; }
        public Money Total { get; private set; }

        public ShoppingCartLine(
            string name,
            string brand,
            Money unitPrice,
            int quantity)
        {
            Name = name;
            Brand = brand;
            UnitPrice = unitPrice;
            Quantity = quantity;
            CalculateTotal();
        }

        private void CalculateTotal()
        {
            Total = new(UnitPrice.Amount * Quantity, UnitPrice.Currency);
        }

        public void ChangeQuantity(int quantity)
        {
            Quantity = quantity;
            CalculateTotal();
        }
    }
}
