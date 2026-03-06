using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;

namespace ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate
{
    public sealed class ProductVersion
    {
        public Guid Id { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime ChangedAt { get; private set; }
        public Guid ProductId { get; private set; }
        public Money Price { get; private set; }
        public string Name { get; private set; }
        public string Brand { get; private set; }

        public void SetChangeDate() => ChangedAt = DateTime.UtcNow;

        public void Deactivate()
        {
            if (!IsActive)
                return;

            IsActive = false;
            SetChangeDate();
        }

        public ProductVersion(
            Guid productId,
            Money price,
            string name,
            string brand)
        {
            Id = Guid.NewGuid();
            IsActive = true;
            ChangedAt = DateTime.UtcNow;
            ProductId = productId;
            Price = price;
            Name = name;
            Brand = brand;
        }
    }
}
