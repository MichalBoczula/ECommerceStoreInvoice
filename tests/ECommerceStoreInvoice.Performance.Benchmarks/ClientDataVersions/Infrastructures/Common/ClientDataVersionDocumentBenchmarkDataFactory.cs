using ECommerceStoreInvoice.Infrastructure.Persistence.ClientDataVersions;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ClientDataVersions.Infrastructures.Common;

internal static class ClientDataVersionDocumentBenchmarkDataFactory
{
    private static readonly DateTime BenchmarkDate = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public static ClientDataVersionDocument Create(Guid id, Guid clientId, DateTime createdAt)
    {
        return new ClientDataVersionDocument
        {
            Id = id,
            ClientId = clientId,
            ClientName = "John Doe",
            PostalCode = "00-001",
            City = "Warsaw",
            Street = "Marszalkowska",
            BuildingNumber = "10A",
            ApartmentNumber = "12",
            PhoneNumber = "123456789",
            PhonePrefix = "+48",
            AddressEmail = "john.doe@example.com",
            CreatedAt = createdAt
        };
    }

    public static ClientDataVersionDocument Create() => Create(Guid.NewGuid(), Guid.NewGuid(), BenchmarkDate);
}