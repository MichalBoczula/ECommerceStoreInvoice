using ECommerceStoreInvoice.Infrastructure.Persistence.ProductVersions;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ProductVersions.Infrastructures.Common;

internal static class ProductVersionDocumentBenchmarkDataFactory
{
    private static readonly DateTime BenchmarkCreatedAt = new(2026, 1, 5, 7, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime BenchmarkDeactivatedAt = new(2026, 1, 6, 8, 0, 0, DateTimeKind.Utc);

    public static ProductVersionDocument Create()
    {
        return new ProductVersionDocument
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            IsActive = false,
            CreatedAt = BenchmarkCreatedAt,
            DeactivatedAt = BenchmarkDeactivatedAt,
            ProductId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            PriceAmount = 45.99m,
            PriceCurrency = "USD",
            Name = "Headphones",
            Brand = "Contoso"
        };
    }
}
