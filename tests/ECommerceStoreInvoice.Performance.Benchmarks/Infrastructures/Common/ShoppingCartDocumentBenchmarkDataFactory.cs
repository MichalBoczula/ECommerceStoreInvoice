using ECommerceStoreInvoice.Infrastructure.Persistence.ShoppingCarts;

namespace ECommerceStoreInvoice.Performance.Benchmarks.TestData;

internal static class ShoppingCartDocumentBenchmarkDataFactory
{
    private static readonly DateTime BenchmarkDate = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public static ShoppingCartDocument CreateWithLines(int linesCount)
    {
        return new ShoppingCartDocument
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            ClientId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            CreatedAt = BenchmarkDate,
            UpdatedAt = BenchmarkDate,
            Lines = Enumerable.Range(1, linesCount)
                .Select(CreateLine)
                .ToList()
        };
    }

    private static ShoppingCartLineDocument CreateLine(int index)
    {
        var unitPriceAmount = 10.99m + index;
        var quantity = index;
        var totalAmount = unitPriceAmount * quantity;

        return new ShoppingCartLineDocument
        {
            ProductId = CreateDeterministicGuid(index),
            Name = $"Product {index}",
            Brand = $"Brand {index % 5}",
            UnitPriceAmount = unitPriceAmount,
            UnitPriceCurrency = "PLN",
            Quantity = quantity,
            TotalAmount = totalAmount,
            TotalCurrency = "PLN"
        };
    }

    private static Guid CreateDeterministicGuid(int index)
    {
        return Guid.Parse($"00000000-0000-0000-0000-{index:000000000000}");
    }
}