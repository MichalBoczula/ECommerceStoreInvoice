using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Invoices.Application.Common
{
    internal static class InvoicePdfServiceBenchmarkDataFactory
    {
        private static readonly DateTime BenchmarkDate = new(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc);

        public static (Order Order, IReadOnlyCollection<ProductVersion> ProductVersions) CreateOrderWithProducts(int linesCount)
        {
            var productVersions = new List<ProductVersion>(linesCount);
            var orderLines = new List<OrderLine>(linesCount);

            for (var i = 1; i <= linesCount; i++)
            {
                var productVersionId = Guid.NewGuid();
                var productId = Guid.NewGuid();

                var productVersion = ProductVersion.Rehydrate(
                    productVersionId,
                    isActive: true,
                    createdAt: BenchmarkDate.AddDays(-1),
                    deactivatedAt: null,
                    productId: productId,
                    price: new Money(100.00m, "PLN"),
                    name: $"Product {i}",
                    brand: "Brand");

                productVersions.Add(productVersion);
                orderLines.Add(new OrderLine(productVersionId, i));
            }

            var order = Order.Rehydrate(
                Guid.NewGuid(),
                Guid.NewGuid(),
                orderLines,
                BenchmarkDate,
                BenchmarkDate,
                OrderStatus.Paid);

            return (order, productVersions);
        }

        public static Order CreateOrder(int linesCount) => CreateOrderWithProducts(linesCount).Order;

        public static IReadOnlyCollection<ProductVersion> CreateProductVersions(int linesCount) => CreateOrderWithProducts(linesCount).ProductVersions;

        public static ClientDataVersionResponseDto CreateClient() => new()
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            ClientName = "Test Client",
            PostalCode = "00-000",
            City = "Warsaw",
            Street = "Main Street",
            BuildingNumber = "1",
            ApartmentNumber = "1",
            PhoneNumber = "123123123",
            PhonePrefix = "+48",
            AddressEmail = "test@example.com",
            CreatedAt = BenchmarkDate
        };
    }
}