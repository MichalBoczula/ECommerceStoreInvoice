using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;

namespace ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate
{
    public sealed class ShoppingCart
    {
        public Guid Id { get; init; }
        public Guid ClientId { get; init; }
        public IReadOnlyCollection<ShoppingCartLine> Lines => _lines.AsReadOnly();
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; private set; }
        public Money Total { get; private set; }

        private readonly List<ShoppingCartLine> _lines = [];

        public ShoppingCart(Guid clientId)
        {
            Id = Guid.NewGuid();
            ClientId = clientId;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            CalculateTotal();
        }

        public void AddItem(ProductVersion productVersion, int quantity)
        {
            var isProductExist = _lines.Any(x => x.ProductVersionId == productVersion.Id);

            if (!isProductExist)
            {
                _lines.Add(new ShoppingCartLine(
                    productVersion.Id,
                    productVersion.Name,
                    productVersion.Brand,
                    productVersion.Price,
                    quantity));
            }
            else
            {
                var existing = _lines.First(x => x.ProductVersionId == productVersion.Id);
                existing.ChangeQuantity(quantity);
            }

            UpdatedAt = DateTime.UtcNow;
        }

        public void RemoveItem(Guid productVersionId, bool all)
        {
            var existing = _lines.FirstOrDefault(x => x.ProductVersionId == productVersionId);
            
            if (existing is null) return;

            if (all)
            {
                _lines.Remove(existing);
                UpdatedAt = DateTime.UtcNow;
            }
            else if (existing.Quantity > 1)
            {
                existing.ChangeQuantity(-1);
                UpdatedAt = DateTime.UtcNow;
            }

            CalculateTotal();
        }

        public void CalculateTotal()
        {
            Total = new(_lines.Sum(x => x.Total.Amount), _lines.FirstOrDefault()?.Total.Currency ?? "USD");
        }
    }
}
