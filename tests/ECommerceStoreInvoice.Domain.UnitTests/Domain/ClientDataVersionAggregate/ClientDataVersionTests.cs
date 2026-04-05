using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.ValueObjects;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Domain.ClientDataVersionAggregate
{
    public class ClientDataVersionTests
    {
        [Fact]
        public void Ctor_ShouldInitializeClientDataVersion()
        {
            // Arrange
            var address = new Address("00-001", "New York", "Main St", "20A", "15");

            // Act
            var clientDataVersion = new ClientDataVersion(address, "123456789", "+1", "client@example.com");

            // Assert
            clientDataVersion.Id.ShouldNotBe(Guid.Empty);
            clientDataVersion.Address.ShouldBe(address);
            clientDataVersion.PhoneNumber.ShouldBe("123456789");
            clientDataVersion.PhonePrefix.ShouldBe("+1");
            clientDataVersion.AddressEmail.ShouldBe("client@example.com");
            clientDataVersion.CreatedAt.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
        }

        [Fact]
        public void Rehydrate_ShouldUseProvidedState()
        {
            // Arrange
            var id = Guid.NewGuid();
            var createdAt = new DateTime(2026, 1, 5, 10, 0, 0, DateTimeKind.Utc);
            var address = new Address("00-001", "New York", "Main St", "20A", "15");

            // Act
            var clientDataVersion = ClientDataVersion.Rehydrate(
                id,
                address,
                "987654321",
                "+1",
                "rehydrated@example.com",
                createdAt);

            // Assert
            clientDataVersion.Id.ShouldBe(id);
            clientDataVersion.Address.ShouldBe(address);
            clientDataVersion.PhoneNumber.ShouldBe("987654321");
            clientDataVersion.PhonePrefix.ShouldBe("+1");
            clientDataVersion.AddressEmail.ShouldBe("rehydrated@example.com");
            clientDataVersion.CreatedAt.ShouldBe(createdAt);
        }
    }
}
