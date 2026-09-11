using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Infrastructure.Persistence.Orders;
using ECommerceStoreInvoice.Performance.Benchmarks.ProductVersions.Infrastructures.Common;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Orders.Infrastructures.Common;

internal static class OrderWithProductsDocumentBenchmarkDataFactory
{
    private static readonly DateTime BenchmarkDate = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public static OrderWithProductsDocument CreateWithLinesAndProductVersions(
        int linesCount,
        Guid id,
        Guid clientId)
    {
        var productVersions = Enumerable.Range(1, linesCount)
            .Select(_ => ProductVersionDocumentBenchmarkDataFactory.Create(Guid.NewGuid()))
            .ToList();

        return new OrderWithProductsDocument
        {
            Id = id,
            ClientId = clientId,
            CreatedAt = BenchmarkDate,
            UpdatedAt = BenchmarkDate,
            Status = OrderStatus.Created,
            Lines = productVersions
                .Select((productVersion, index) => new OrderLineDocument
                {
                    ProductVersionId = productVersion.Id,
                    Quantity = index + 1
                })
                .ToList(),
            ProductVersions = productVersions
        };
    }
}
