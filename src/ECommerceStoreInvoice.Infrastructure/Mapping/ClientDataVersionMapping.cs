using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.ValueObjects;
using ECommerceStoreInvoice.Infrastructure.Persistence.ClientDataVersions;

namespace ECommerceStoreInvoice.Infrastructure.Mapping
{
    internal static class ClientDataVersionMapping
    {
        public static ClientDataVersionDocument MapToDocument(ClientDataVersion clientDataVersion)
        {
            return new ClientDataVersionDocument
            {
                Id = clientDataVersion.Id,
                ClientId = clientDataVersion.ClientId,
                ClientName = clientDataVersion.ClientName,
                PostalCode = clientDataVersion.Address.PostalCode,
                City = clientDataVersion.Address.City,
                Street = clientDataVersion.Address.Street,
                BuildingNumber = clientDataVersion.Address.BuildingNumber,
                ApartmentNumber = clientDataVersion.Address.ApartmentNumber,
                PhoneNumber = clientDataVersion.PhoneNumber,
                PhonePrefix = clientDataVersion.PhonePrefix,
                AddressEmail = clientDataVersion.AddressEmail,
                CreatedAt = clientDataVersion.CreatedAt
            };
        }

        public static ClientDataVersion MapToDomain(ClientDataVersionDocument clientDataVersionDocument)
        {
            return ClientDataVersion.Rehydrate(
                clientDataVersionDocument.Id,
                clientDataVersionDocument.ClientId,
                clientDataVersionDocument.ClientName,
                new Address(
                    clientDataVersionDocument.PostalCode,
                    clientDataVersionDocument.City,
                    clientDataVersionDocument.Street,
                    clientDataVersionDocument.BuildingNumber,
                    clientDataVersionDocument.ApartmentNumber),
                clientDataVersionDocument.PhoneNumber,
                clientDataVersionDocument.PhonePrefix,
                clientDataVersionDocument.AddressEmail,
                clientDataVersionDocument.CreatedAt);
        }
    }
}
