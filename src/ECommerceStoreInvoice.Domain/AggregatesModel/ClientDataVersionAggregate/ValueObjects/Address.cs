namespace ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.ValueObjects
{
    public readonly record struct Address
    {
        public string PostalCode { get; init; }
        public string City { get; init; }
        public string Street { get; init; }
        public string BuildingNumber { get; init; }
        public string ApartmentNumber { get; init; }

        public Address(
            string postalCode,
            string city,
            string street,
            string buildingNumber,
            string apartmentNumber)
        {
            PostalCode = postalCode;
            City = city;
            Street = street;
            BuildingNumber = buildingNumber;
            ApartmentNumber = apartmentNumber;
        }
    }
}
