using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.ValueObjects;

namespace ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate
{
    public sealed class ClientDataVersion
    {
        public Guid Id { get; init; }
        public Guid ClientId { get; init; }
        public string ClientName { get; init; }
        public Address Address { get; init; }
        public string PhoneNumber { get; init; }
        public string PhonePrefix { get; init; }
        public string AddressEmail { get; init; }
        public DateTime CreatedAt { get; init; }

        public ClientDataVersion(
            Guid clientId,
            string clientName,
            Address address,
            string phoneNumber,
            string phonePrefix,
            string addressEmail)
        {
            Id = Guid.NewGuid();
            ClientId = clientId;
            ClientName = clientName;
            Address = address;
            PhoneNumber = phoneNumber;
            PhonePrefix = phonePrefix;
            AddressEmail = addressEmail;
            CreatedAt = DateTime.UtcNow;
        }

        private ClientDataVersion(
            Guid id,
            Guid clientId,
            string clientName,
            Address address,
            string phoneNumber,
            string phonePrefix,
            string addressEmail,
            DateTime createdAt)
        {
            Id = id;
            ClientId = clientId;
            ClientName = clientName;
            Address = address;
            PhoneNumber = phoneNumber;
            PhonePrefix = phonePrefix;
            AddressEmail = addressEmail;
            CreatedAt = createdAt;
        }

        public static ClientDataVersion Rehydrate(
            Guid id,
            Guid clientId,
            string clientName,
            Address address,
            string phoneNumber,
            string phonePrefix,
            string addressEmail,
            DateTime createdAt)
        {
            return new ClientDataVersion(
                id,
                clientId,
                clientName,
                address,
                phoneNumber,
                phonePrefix,
                addressEmail,
                createdAt);
        }
    }
}
