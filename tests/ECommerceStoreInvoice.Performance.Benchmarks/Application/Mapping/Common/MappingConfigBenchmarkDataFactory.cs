using ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Application.Mapping.Common;

internal static class MappingConfigBenchmarkDataFactory
{
    private static readonly DateTime BenchmarkDate = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public static CreateClientDataVersionRequestDto CreateClientDataVersionRequest()
    {
        return new CreateClientDataVersionRequestDto
        {
            ClientName = "Jane Doe",
            PostalCode = "00-001",
            City = "Warsaw",
            Street = "Main",
            BuildingNumber = "10A",
            ApartmentNumber = "2",
            PhoneNumber = "123456789",
            PhonePrefix = "+48",
            AddressEmail = "jane@example.com"
        };
    }

    public static ShoppingCart CreateShoppingCartWithLines(int linesCount)
    {
        var lines = Enumerable.Range(0, linesCount)
            .Select(i => new ShoppingCartLine(
                Guid.NewGuid(),
                $"Product-{i}",
                "Contoso",
                new Money(9.99m + i, "USD"),
                i + 1))
            .ToList();

        return ShoppingCart.Rehydrate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            BenchmarkDate,
            BenchmarkDate,
            lines);
    }

    public static ProductVersion CreateProductVersion()
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
