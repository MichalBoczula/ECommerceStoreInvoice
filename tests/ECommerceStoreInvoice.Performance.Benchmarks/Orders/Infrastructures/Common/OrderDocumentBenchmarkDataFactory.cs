using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Infrastructure.Persistence.Orders;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Orders.Infrastructures.Common;

internal static class OrderDocumentBenchmarkDataFactory
{
    private static readonly DateTime BenchmarkDate = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public static OrderDocument CreateWithLines(int linesCount, Guid id, Guid clientId)
    {
        return new OrderDocument
        {
            Id = id,
            ClientId = clientId,
            CreatedAt = BenchmarkDate,
            UpdatedAt = BenchmarkDate,
            Status = OrderStatus.Created,
            Lines = Enumerable.Range(1, linesCount)
                .Select(CreateLine)
                .ToList()
        };
    }

    private static OrderLineDocument CreateLine(int index)
    {
        return new OrderLineDocument
        {
            ProductVersionId = Guid.NewGuid(),
            Quantity = index
        };
    }
}