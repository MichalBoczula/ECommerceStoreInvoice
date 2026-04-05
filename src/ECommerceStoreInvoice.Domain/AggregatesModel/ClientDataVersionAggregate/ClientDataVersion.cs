using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.ValueObjects;

namespace ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate
{
    public sealed class ClientDataVersion
    {
        public Guid Id { get; init; }
        public Address Address { get; init; }
        public string PhoneNumber { get; init; }
        public string PhonePrefix { get; init; }
        public string AddressEmail { get; init; }
        public DateTime CreatedAt { get; init; }

        public ClientDataVersion(
            Address address,
            string phoneNumber,
            string phonePrefix,
            string addressEmail)
        {
            Id = Guid.NewGuid();
            Address = address;
            PhoneNumber = phoneNumber;
            PhonePrefix = phonePrefix;
            AddressEmail = addressEmail;
            CreatedAt = DateTime.UtcNow;
        }

        private ClientDataVersion(
            Guid id,
            Address address,
            string phoneNumber,
            string phonePrefix,
            string addressEmail,
            DateTime createdAt)
        {
            Id = id;
            Address = address;
            PhoneNumber = phoneNumber;
            PhonePrefix = phonePrefix;
            AddressEmail = addressEmail;
            CreatedAt = createdAt;
        }

        public static ClientDataVersion Rehydrate(
            Guid id,
            Address address,
            string phoneNumber,
            string phonePrefix,
            string addressEmail,
            DateTime createdAt)
        {
            return new ClientDataVersion(
                id,
                address,
                phoneNumber,
                phonePrefix,
                addressEmail,
                createdAt);
        }
    }
}
