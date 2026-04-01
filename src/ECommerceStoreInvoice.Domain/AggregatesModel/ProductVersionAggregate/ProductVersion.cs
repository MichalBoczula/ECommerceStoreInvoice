using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;

namespace ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate
{
    public sealed class ProductVersion
    {
        public Guid Id { get; init; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; init; }
        public DateTime? DeactivatedAt { get; private set; }
        public Guid ProductId { get; init; }
        public Money Price { get; init; }
        public string Name { get; init; }
        public string Brand { get; init; }

        public void Deactivate()
        {
            if (!IsActive)
                return;

            IsActive = false;
            DeactivatedAt = DateTime.UtcNow;
        }

        public ProductVersion(
            Guid productId,
            Money price,
            string name,
            string brand)
        {
            Id = Guid.NewGuid();
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
            DeactivatedAt = null;
            ProductId = productId;
            Price = price;
            Name = name;
            Brand = brand;
        }

        private ProductVersion(
            Guid id,
            bool isActive,
            DateTime createdAt,
            DateTime? deactivatedAt,
            Guid productId,
            Money price,
            string name,
            string brand)
        {
            Id = id;
            IsActive = isActive;
            CreatedAt = createdAt;
            DeactivatedAt = deactivatedAt;
            ProductId = productId;
            Price = price;
            Name = name;
            Brand = brand;
        }

        public static ProductVersion Rehydrate(
            Guid id,
            bool isActive,
            DateTime createdAt,
            DateTime? deactivatedAt,
            Guid productId,
            Money price,
            string name,
            string brand)
        {
            return new ProductVersion(
                id,
                isActive,
                createdAt,
                deactivatedAt,
                productId,
                price,
                name,
                brand);
        }
    }
}
