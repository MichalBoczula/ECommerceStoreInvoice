using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.ValueObjects;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ClientDataVersions.Domain
{
    internal static class ClientDataVersionValidationDataFactory
    {
        public static ClientDataVersion CreateValid()
        {
            var address = new Address("00-001", "Warsaw", "Street", "1", "1");

            return new ClientDataVersion(
                Guid.NewGuid(),
                "John Doe",
                address,
                "123456789",
                "48",
                "john.doe@example.com");
        }

        public static ClientDataVersion CreateInvalidEmail()
        {
            var address = new Address("00-001", "Warsaw", "Street", "1", "1");

            return new ClientDataVersion(
                Guid.NewGuid(),
                "John Doe",
                address,
                "123456789",
                "48",
                "invalid-email"); 
        }

        public static ClientDataVersion CreateAllInvalid()
        {
            var address = new Address("ABC", "City", "Street", "B", "A");

            return new ClientDataVersion(
                Guid.NewGuid(),
                "John Doe",
                address,
                "-100",  
                "prefix", 
                "bad@@email.com");
        }
    }
}
