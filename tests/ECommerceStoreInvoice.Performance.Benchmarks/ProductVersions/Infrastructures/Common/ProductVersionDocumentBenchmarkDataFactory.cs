using ECommerceStoreInvoice.Infrastructure.Persistence.ProductVersions;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ProductVersions.Infrastructures.Common;

internal static class ProductVersionDocumentBenchmarkDataFactory
{
    private static readonly DateTime BenchmarkCreatedAt = new(2026, 1, 5, 7, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime BenchmarkDeactivatedAt = new(2026, 1, 6, 8, 0, 0, DateTimeKind.Utc);

    public static ProductVersionDocument Create(Guid id)
    {
        return new ProductVersionDocument
        {
            Id = id,
            IsActive = false,
            CreatedAt = BenchmarkCreatedAt,
            DeactivatedAt = BenchmarkDeactivatedAt,
            ProductId = Guid.NewGuid(),
            PriceAmount = 45.99m,
            PriceCurrency = "USD",
            Name = "Headphones",
            Brand = "Contoso"
        };
    }

    public static ProductVersionDocument Create()
    {
        return Create(Guid.NewGuid());
    }
}