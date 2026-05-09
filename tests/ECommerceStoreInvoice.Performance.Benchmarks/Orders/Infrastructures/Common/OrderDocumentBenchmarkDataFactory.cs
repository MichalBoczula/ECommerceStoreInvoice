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
            TotalAmount = CalculateTotal(linesCount),
            TotalCurrency = "PLN",
            Lines = Enumerable.Range(1, linesCount)
                .Select(CreateLine)
                .ToList()
        };
    }

    private static decimal CalculateTotal(int linesCount)
    {
        return Enumerable.Range(1, linesCount)
            .Sum(index => (10.99m + index) * index);
    }

    private static OrderLineDocument CreateLine(int index)
    {
        var unitPriceAmount = 10.99m + index;
        var quantity = index;

        return new OrderLineDocument
        {
            ProductVersionId = Guid.NewGuid(),
            Name = $"Product {index}",
            Brand = $"Brand {index % 5}",
            UnitPriceAmount = unitPriceAmount,
            UnitPriceCurrency = "PLN",
            Quantity = quantity,
            TotalAmount = unitPriceAmount * quantity,
            TotalCurrency = "PLN"
        };
    }
}