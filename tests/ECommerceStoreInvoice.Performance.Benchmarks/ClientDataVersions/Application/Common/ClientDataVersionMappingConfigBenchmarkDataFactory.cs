using ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.ValueObjects;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ClientDataVersions.Application.Common;

internal static class ClientDataVersionMappingConfigBenchmarkDataFactory
{
    public static CreateClientDataVersionRequestDto CreateRequest()
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

    public static ClientDataVersion CreateDomainClientDataVersion(Guid clientId)
    {
        return new ClientDataVersion(
            clientId,
            "Jane Doe",
            new Address("00-001", "Warsaw", "Main", "10A", "2"),
            "123456789",
            "+48",
            "jane@example.com");
    }
}
