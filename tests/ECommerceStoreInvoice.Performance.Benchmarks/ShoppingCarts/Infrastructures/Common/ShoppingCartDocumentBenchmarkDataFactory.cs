using ECommerceStoreInvoice.Infrastructure.Persistence.ShoppingCarts;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ShoppingCarts.Infrastructures.Common;

internal static class ShoppingCartDocumentBenchmarkDataFactory
{
    private static readonly DateTime BenchmarkDate = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public static ShoppingCartDocument CreateWithLines(int linesCount)
    {
        return CreateWithLines(
            linesCount,
            Guid.NewGuid());
    }

    public static ShoppingCartDocument CreateWithLines(int linesCount, Guid clientId)
    {
        return new ShoppingCartDocument
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            CreatedAt = BenchmarkDate,
            UpdatedAt = BenchmarkDate,
            Lines = Enumerable.Range(1, linesCount)
                .Select(CreateLine)
                .ToList()
        };
    }

    private static ShoppingCartLineDocument CreateLine(int index)
    {
        var quantity = index;

        return new ShoppingCartLineDocument
        {
            ProductId = CreateDeterministicGuid(index),
            Quantity = quantity
        };
    }

    private static Guid CreateDeterministicGuid(int index)
    {
        return Guid.Parse($"00000000-0000-0000-0000-{index:000000000000}");
    }
}