using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;

namespace ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate
{
    public sealed class ShoppingCart
    {
        public Guid Id { get; }
        public Guid ClientId { get; }
        public IReadOnlyCollection<ShoppingCartLine> Lines => _lines.AsReadOnly();
        public DateTime CreatedAt { get; }
        public DateTime UpdatedAt { get; private set; }
        public bool CheckedOut { get; private set; }

        private readonly List<ShoppingCartLine> _lines = [];

        public ShoppingCart(Guid clientId)
        {
            Id = Guid.NewGuid();
            ClientId = clientId;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void AddItem(ProductVersion productVersion, int quantity)
        {
            var existing = _lines.FirstOrDefault(x => x.ProductVersionId == productVersion.Id);

            if (existing is null)
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
                existing.Increase(quantity);
            }

            UpdatedAt = DateTime.UtcNow;
        }

        public void Checkout()
        {
            CheckedOut = true;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
