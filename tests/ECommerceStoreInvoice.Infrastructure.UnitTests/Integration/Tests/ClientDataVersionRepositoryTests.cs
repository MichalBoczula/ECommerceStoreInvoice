using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.ValueObjects;
using ECommerceStoreInvoice.Infrastructure.UnitTests.Integration.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ECommerceStoreInvoice.Infrastructure.UnitTests.Integration.Tests
{
    public sealed class ClientDataVersionRepositoryTests : IClassFixture<MongoDbTestFixture>
    {
        private readonly MongoDbTestFixture _fixture;

        public ClientDataVersionRepositoryTests(MongoDbTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Create_ShouldSaveClientDataVersion()
        {
            // arrange
            var databaseName = $"invoice-tests-{Guid.NewGuid():N}";

            await using var serviceProvider = TestServiceProviderFactory.Create(
                _fixture.ConnectionString,
                databaseName);

            var repository = serviceProvider
                .GetRequiredService<IClientDataVersionRepository>();

            var clientId = Guid.NewGuid();

            var clientDataVersion = ClientDataVersion.Rehydrate(
                Guid.NewGuid(),
                clientId,
                "Jan Kowalski",
                new Address(
                    "00-001",
                    "Warszawa",
                    "Testowa",
                    "10",
                    "5"),
                "123456789",
                "+48",
                "jan.kowalski@test.pl",
                DateTime.UtcNow);

            // act
            await repository.Create(clientDataVersion);

            // assert
            var result = await repository.GetByClientId(clientId);

            result.ShouldNotBeNull();
            result.Id.ShouldBe(clientDataVersion.Id);
            result.ClientId.ShouldBe(clientDataVersion.ClientId);
            result.ClientName.ShouldBe(clientDataVersion.ClientName);
            result.Address.PostalCode.ShouldBe(clientDataVersion.Address.PostalCode);
            result.Address.City.ShouldBe(clientDataVersion.Address.City);
            result.Address.Street.ShouldBe(clientDataVersion.Address.Street);
            result.Address.BuildingNumber.ShouldBe(clientDataVersion.Address.BuildingNumber);
            result.Address.ApartmentNumber.ShouldBe(clientDataVersion.Address.ApartmentNumber);
            result.PhoneNumber.ShouldBe(clientDataVersion.PhoneNumber);
            result.PhonePrefix.ShouldBe(clientDataVersion.PhonePrefix);
            result.AddressEmail.ShouldBe(clientDataVersion.AddressEmail);
        }

        [Fact]
        public async Task GetByClientId_ShouldReturnClientDataVersion_WhenExists()
        {
            // arrange
            var databaseName = $"invoice-tests-{Guid.NewGuid():N}";

            await using var serviceProvider = TestServiceProviderFactory.Create(
                _fixture.ConnectionString,
                databaseName);

            var repository = serviceProvider
                .GetRequiredService<IClientDataVersionRepository>();

            var clientId = Guid.NewGuid();

            var clientDataVersion = ClientDataVersion.Rehydrate(
                Guid.NewGuid(),
                clientId,
                "Adam Nowak",
                new Address(
                    "30-001",
                    "Kraków",
                    "Długa",
                    "20",
                    null),
                "987654321",
                "+48",
                "adam.nowak@test.pl",
                DateTime.UtcNow);

            await repository.Create(clientDataVersion);

            // act
            var result = await repository.GetByClientId(clientId);

            // assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(clientDataVersion.Id);
            result.ClientId.ShouldBe(clientId);
            result.ClientName.ShouldBe("Adam Nowak");
            result.Address.PostalCode.ShouldBe("30-001");
            result.Address.City.ShouldBe("Kraków");
            result.Address.Street.ShouldBe("Długa");
            result.Address.BuildingNumber.ShouldBe("20");
            result.Address.ApartmentNumber.ShouldBeNull();
            result.PhoneNumber.ShouldBe("987654321");
            result.PhonePrefix.ShouldBe("+48");
            result.AddressEmail.ShouldBe("adam.nowak@test.pl");
        }

        [Fact]
        public async Task GetByClientId_ShouldReturnNull_WhenClientDataVersionDoesNotExist()
        {
            // arrange
            var databaseName = $"invoice-tests-{Guid.NewGuid():N}";

            await using var serviceProvider = TestServiceProviderFactory.Create(
                _fixture.ConnectionString,
                databaseName);

            var repository = serviceProvider
                .GetRequiredService<IClientDataVersionRepository>();

            var clientId = Guid.NewGuid();

            // act
            var result = await repository.GetByClientId(clientId);

            // assert
            result.ShouldBeNull();
        }
    }
}