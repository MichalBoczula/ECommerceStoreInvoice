using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;

public sealed class ShoppingCart
{
    public Guid Id { get; init; }
    public Guid ClientId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; private set; }
    public IReadOnlyCollection<ShoppingCartLine> Lines => _lines.AsReadOnly();
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

    private ShoppingCart(Guid id, Guid clientId, DateTime createdAt, DateTime updatedAt)
    {
        Id = id;
        ClientId = clientId;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    private void CalculateTotal()
    {
        Total = new(_lines.Sum(x => x.Total.Amount), _lines.FirstOrDefault()?.Total.Currency ?? "USD");
    }

    public void Clear()
    {
        _lines.Clear();
        UpdatedAt = DateTime.UtcNow;
        CalculateTotal();
    }

    public void ReplaceLines(IEnumerable<ShoppingCartLine> lines)
    {
        _lines.Clear();
        _lines.AddRange(lines);
        UpdatedAt = DateTime.UtcNow;
        CalculateTotal();
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
        shoppingCart.CalculateTotal();

        return shoppingCart;
    }
}