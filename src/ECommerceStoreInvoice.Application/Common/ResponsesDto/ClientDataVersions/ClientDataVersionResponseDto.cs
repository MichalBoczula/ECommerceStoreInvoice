namespace ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions
{
    public sealed record ClientDataVersionResponseDto
    {
        public required Guid Id { get; init; }
        public required Guid ClientId { get; init; }
        public required string PostalCode { get; init; }
        public required string City { get; init; }
        public required string Street { get; init; }
        public required string BuildingNumber { get; init; }
        public required string ApartmentNumber { get; init; }
        public required string PhoneNumber { get; init; }
        public required string PhonePrefix { get; init; }
        public required string AddressEmail { get; init; }
        public required DateTime CreatedAt { get; init; }
    }
}
