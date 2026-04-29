using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Infrastructure.Persistence.Orders;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Orders.Infrastructures.Common;

internal static class OrderDocumentBenchmarkDataFactory
{
    private static readonly DateTime BenchmarkDate = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public static object CreateWithLines(int linesCount)
    {
        return new OrderDocument
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            ClientId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
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
        var totalAmount = unitPriceAmount * quantity;

        return new OrderLineDocument
        {
            ProductVersionId = CreateDeterministicGuid(index),
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
