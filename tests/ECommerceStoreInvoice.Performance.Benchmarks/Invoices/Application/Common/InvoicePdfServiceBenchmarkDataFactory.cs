using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Invoices.Application.Common
{
    internal static class InvoicePdfServiceBenchmarkDataFactory
    {
        public static Order CreateOrder(int linesCount)
        {
            var lines = Enumerable.Range(1, linesCount).Select(i => new OrderLine(
                Guid.NewGuid(),
                $"Product {i}",
                "Brand",
                new Money(100.00m, "PLN"),
                i)).ToList();

            return Order.Rehydrate(
                Guid.NewGuid(),
                Guid.NewGuid(),
                lines,
                DateTime.UtcNow,
                DateTime.UtcNow,
                OrderStatus.Paid,
                new Money(lines.Sum(x => x.Total.Amount), "PLN"));
        }

        public static ClientDataVersionResponseDto CreateClient() => new()
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            ClientName = "Test Client",
            PostalCode = "00-000",
            City = "City",
            Street = "Street",
            BuildingNumber = "1",
            ApartmentNumber = "1",
            PhoneNumber = "123123123",
            PhonePrefix = "+48",
            AddressEmail = "test@example.com",
            CreatedAt = DateTime.UtcNow
        };
    }
}
