using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;

namespace ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate
{
    public sealed class ShoppingCart
    {
        public Guid Id { get; init; }
        public Guid ClientId { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; private set; }
        public IReadOnlyCollection<ShoppingCartLine> Lines => _lines.AsReadOnly();

        private readonly List<ShoppingCartLine> _lines = [];

        public ShoppingCart(Guid clientId)
        {
            Id = Guid.NewGuid();
            ClientId = clientId;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        private ShoppingCart(Guid id, Guid clientId, DateTime createdAt, DateTime updatedAt)
        {
            Id = id;
            ClientId = clientId;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        public void Clear()
        {
            _lines.Clear();
            UpdatedAt = DateTime.UtcNow;
        }

        public void ReplaceLines(IEnumerable<ShoppingCartLine> lines)
        {
            _lines.Clear();
            _lines.AddRange(lines);
            UpdatedAt = DateTime.UtcNow;
        }

        public static ShoppingCart Rehydrate(
            Guid id,
            Guid clientId,
            DateTime createdAt,
            DateTime updatedAt,
            IEnumerable<ShoppingCartLine> lines)
        {
            var shoppingCart = new ShoppingCart(id, clientId, createdAt, updatedAt);
            shoppingCart._lines.AddRange(lines);
            return shoppingCart;
        }
    }
}