namespace ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.ValueObjects
{
    public readonly record struct ClientSnapshot
    {
        public Guid ClientId { get; init; }
        public string FullName { get; init; }
        public string BuildingNumber { get; init; }
        public string ApartmentNumber { get; init; }
        public string Street { get; init; }
        public string City { get; init; }
        public string PostalCode { get; init; }

        public ClientSnapshot(
            Guid clientId,
            string fullName,
            string buildingNumber,
            string apartmentNumber,
            string street,
            string city,
            string postalCode)
        {
            ClientId = clientId;
            FullName = fullName;
            BuildingNumber = buildingNumber;
            ApartmentNumber = apartmentNumber;
            Street = street;
            City = city;
            PostalCode = postalCode;
        }
    }
}
