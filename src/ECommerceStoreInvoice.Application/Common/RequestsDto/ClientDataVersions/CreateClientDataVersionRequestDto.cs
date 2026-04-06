namespace ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions
{
    public sealed record CreateClientDataVersionRequestDto
    {
        public required string ClientName { get; init; }
        public required string PostalCode { get; init; }
        public required string City { get; init; }
        public required string Street { get; init; }
        public required string BuildingNumber { get; init; }
        public required string ApartmentNumber { get; init; }
        public required string PhoneNumber { get; init; }
        public required string PhonePrefix { get; init; }
        public required string AddressEmail { get; init; }
    }
}
