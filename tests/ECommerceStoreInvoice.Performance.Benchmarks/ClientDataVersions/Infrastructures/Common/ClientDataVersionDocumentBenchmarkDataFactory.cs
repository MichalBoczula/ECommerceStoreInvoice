using ECommerceStoreInvoice.Infrastructure.Persistence.ClientDataVersions;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ClientDataVersions.Infrastructures.Common;

internal static class ClientDataVersionDocumentBenchmarkDataFactory
{
    private static readonly DateTime BenchmarkDate = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public static ClientDataVersionDocument Create()
    {
        return new ClientDataVersionDocument
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            ClientId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            ClientName = "John Doe",
            PostalCode = "00-001",
            City = "Warsaw",
            Street = "Marszalkowska",
            BuildingNumber = "10A",
            ApartmentNumber = "12",
            PhoneNumber = "123456789",
            PhonePrefix = "+48",
            AddressEmail = "john.doe@example.com",
            CreatedAt = BenchmarkDate
        };
    }
}
