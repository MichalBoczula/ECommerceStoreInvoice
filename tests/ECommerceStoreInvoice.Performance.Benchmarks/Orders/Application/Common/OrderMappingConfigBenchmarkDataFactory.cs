using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Orders.Application.Common;

internal static class OrderMappingConfigBenchmarkDataFactory
{
    private static readonly DateTime BenchmarkDate = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public static IReadOnlyCollection<ProductVersion> CreateProductVersions(int linesCount)
    {
        return Enumerable.Range(1, linesCount)
            .Select(CreateProductVersion)
            .ToList();
    }

    public static ShoppingCartLine CreateShoppingCartLine(int index)
    {
        return new ShoppingCartLine(
            CreateDeterministicGuid(index),
            $"Product {index}",
            $"Brand {index % 5}",
            new Money(10.99m + index, "PLN"),
            index);
    }

    public static ProductVersion CreateProductVersion(int index)
    {
        return ProductVersion.Rehydrate(
            CreateDeterministicGuid(index + 1000),
            true,
            BenchmarkDate,
            null,
            CreateDeterministicGuid(index),
            new Money(10.99m + index, "PLN"),
            $"Product {index}",
            $"Brand {index % 5}");
    }

    public static ShoppingCart CreateDomainCart(int linesCount)
    {
        var lines = Enumerable.Range(1, linesCount)
            .Select(CreateShoppingCartLine)
            .ToList();

        return ShoppingCart.Rehydrate(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            BenchmarkDate,
            BenchmarkDate,
            lines);
    }

    public static Order CreateDomainOrder(int linesCount)
    {
        var lines = Enumerable.Range(1, linesCount)
            .Select(index => new OrderLine(
                CreateDeterministicGuid(index + 1000),
                $"Product {index}",
                $"Brand {index % 5}",
                new Money(10.99m + index, "PLN"),
                index))
            .ToList();

        return Order.Rehydrate(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            lines,
            BenchmarkDate,
            BenchmarkDate,
            OrderStatus.Created,
            new Money(lines.Sum(x => x.Total.Amount), "PLN"));
    }

    private static Guid CreateDeterministicGuid(int index)
    {
        return Guid.Parse($"00000000-0000-0000-0000-{index:000000000000}");
    }
}
