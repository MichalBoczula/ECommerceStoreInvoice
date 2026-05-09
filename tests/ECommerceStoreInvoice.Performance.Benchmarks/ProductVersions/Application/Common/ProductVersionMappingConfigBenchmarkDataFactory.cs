using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ProductVersions.Application.Common;

internal static class ProductVersionMappingConfigBenchmarkDataFactory
{
    private static readonly DateTime BenchmarkDate = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public static ProductVersion CreateDomainProductVersion()
    {
        return ProductVersion.Rehydrate(
            Guid.NewGuid(),
            true,
            BenchmarkDate,
            null,
            Guid.NewGuid(),
            new Money(199.99m, "USD"),
            "Product Name",
            "Contoso");
    }
}
