using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ShoppingCarts.Application.Common;

internal static class ShoppingCartMappingConfigBenchmarkDataFactory
{
    private static readonly DateTime BenchmarkDate = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public static ShoppingCartLineRequestDto CreateRequestLine(int index)
    {
        return new ShoppingCartLineRequestDto
        {
            ProductId = CreateDeterministicGuid(index),
            Name = $"Product {index}",
            Brand = $"Brand {index % 5}",
            UnitPriceAmount = 10.99m + index,
            UnitPriceCurrency = "PLN",
            Quantity = index
        };
    }

    public static IReadOnlyCollection<ShoppingCartLineRequestDto> CreateRequestLines(int linesCount)
    {
        return Enumerable.Range(1, linesCount)
            .Select(CreateRequestLine)
            .ToList();
    }

    public static ShoppingCartLine CreateDomainLine(int index)
    {
        return new ShoppingCartLine(
            CreateDeterministicGuid(index),
            $"Product {index}",
            $"Brand {index % 5}",
            new Money(10.99m + index, "PLN"),
            index);
    }

    public static ShoppingCart CreateDomainCart(int linesCount)
    {
        var lines = Enumerable.Range(1, linesCount)
            .Select(CreateDomainLine)
            .ToList();

        return ShoppingCart.Rehydrate(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            BenchmarkDate,
            BenchmarkDate,
            lines);
    }

    private static Guid CreateDeterministicGuid(int index)
    {
        return Guid.Parse($"00000000-0000-0000-0000-{index:000000000000}");
    }
}
