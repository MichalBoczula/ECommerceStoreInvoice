using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Invoices.Application.Common
{
    internal static class InvoiceMappingConfigBenchmarkDataFactory
    {
        private static readonly DateTime BenchmarkDate = new(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc);

        public static Invoice CreateDomainInvoice()
        {
            return Invoice.Rehydrate(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                "https://storage.example/invoices/42.pdf",
                BenchmarkDate);
        }

        public static Order CreateSampleOrder(Guid clientId, Guid orderId)
        {
            return Order.Rehydrate(
                orderId,
                clientId,
                new List<OrderLine>(),
                BenchmarkDate,
                BenchmarkDate,
                OrderStatus.Paid,
                new Money(500m, "PLN"));
        }

        public static ClientDataVersionResponseDto CreateClientResponse(Guid clientId)
        {
            return new ClientDataVersionResponseDto
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                ClientName = "Jane Doe",
                PostalCode = "00-001",
                City = "Warsaw",
                Street = "Main Street",
                BuildingNumber = "10A",
                ApartmentNumber = "2",
                PhoneNumber = "123456789",
                PhonePrefix = "+48",
                AddressEmail = "jane@example.com",
                CreatedAt = BenchmarkDate
            };
        }
    }
}