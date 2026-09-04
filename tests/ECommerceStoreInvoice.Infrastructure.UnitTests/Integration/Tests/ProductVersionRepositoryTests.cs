using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;
using ECommerceStoreInvoice.Infrastructure.UnitTests.Integration.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ECommerceStoreInvoice.Infrastructure.UnitTests.Integration.Tests
{
    public sealed class ProductVersionRepositoryTests : IClassFixture<MongoDbTestFixture>
    {
        private readonly MongoDbTestFixture _fixture;

        public ProductVersionRepositoryTests(MongoDbTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task CreateProductVersion_ShouldSaveProductVersion()
        {
            // arrange
            var databaseName = $"invoice-tests-{Guid.NewGuid():N}";

            await using var serviceProvider = TestServiceProviderFactory.Create(
                _fixture.ConnectionString,
                databaseName);

            var repository = serviceProvider
                .GetRequiredService<IProductVersionRepository>();

            var productVersion = ProductVersion.Rehydrate(
                Guid.NewGuid(),
                isActive: true,
                createdAt: DateTime.UtcNow,
                deactivatedAt: null,
                productId: Guid.NewGuid(),
                price: new Money(99.99m, "PLN"),
                name: "Gaming Mouse",
                brand: "TestBrand");

            // act
            await repository.CreateProductVersion(productVersion);

            // assert
            var result = await repository.GetProductVersionById(productVersion.Id);

            result.ShouldNotBeNull();
            result.Id.ShouldBe(productVersion.Id);
            result.IsActive.ShouldBe(productVersion.IsActive);
            result.ProductId.ShouldBe(productVersion.ProductId);
            result.Price.Amount.ShouldBe(productVersion.Price.Amount);
            result.Price.Currency.ShouldBe(productVersion.Price.Currency);
            result.Name.ShouldBe(productVersion.Name);
            result.Brand.ShouldBe(productVersion.Brand);
        }

        [Fact]
        public async Task GetProductVersionById_ShouldReturnProductVersion_WhenExists()
        {
            // arrange
            var databaseName = $"invoice-tests-{Guid.NewGuid():N}";

            await using var serviceProvider = TestServiceProviderFactory.Create(
                _fixture.ConnectionString,
                databaseName);

            var repository = serviceProvider
                .GetRequiredService<IProductVersionRepository>();

            var productVersion = ProductVersion.Rehydrate(
                Guid.NewGuid(),
                isActive: false,
                createdAt: DateTime.UtcNow.AddDays(-1),
                deactivatedAt: DateTime.UtcNow,
                productId: Guid.NewGuid(),
                price: new Money(249.50m, "EUR"),
                name: "Mechanical Keyboard",
                brand: "KeyMaster");

            await repository.CreateProductVersion(productVersion);

            // act
            var result = await repository.GetProductVersionById(productVersion.Id);

            // assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(productVersion.Id);
            result.IsActive.ShouldBeFalse();
            result.DeactivatedAt.ShouldNotBeNull();
            result.ProductId.ShouldBe(productVersion.ProductId);
            result.Price.Amount.ShouldBe(249.50m);
            result.Price.Currency.ShouldBe("EUR");
            result.Name.ShouldBe("Mechanical Keyboard");
            result.Brand.ShouldBe("KeyMaster");
        }

        [Fact]
        public async Task GetProductVersionById_ShouldReturnNull_WhenProductVersionDoesNotExist()
        {
            // arrange
            var databaseName = $"invoice-tests-{Guid.NewGuid():N}";

            await using var serviceProvider = TestServiceProviderFactory.Create(
                _fixture.ConnectionString,
                databaseName);

            var repository = serviceProvider
                .GetRequiredService<IProductVersionRepository>();

            // act
            var result = await repository.GetProductVersionById(Guid.NewGuid());

            // assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task CreateProductVersions_ShouldSaveMultipleProductVersionsInBulk()
        {
            // arrange
            var databaseName = $"invoice-tests-{Guid.NewGuid():N}";

            await using var serviceProvider = TestServiceProviderFactory.Create(
                _fixture.ConnectionString,
                databaseName);

            var repository = serviceProvider
                .GetRequiredService<IProductVersionRepository>();

            var productVersion1 = ProductVersion.Rehydrate(
                Guid.NewGuid(), true, DateTime.UtcNow, null, Guid.NewGuid(),
                new Money(150m, "USD"), "Monitor 24", "Dell");

            var productVersion2 = ProductVersion.Rehydrate(
                Guid.NewGuid(), true, DateTime.UtcNow, null, Guid.NewGuid(),
                new Money(300m, "USD"), "Monitor 27", "LG");

            var productVersion3 = ProductVersion.Rehydrate(
                Guid.NewGuid(), true, DateTime.UtcNow, null, Guid.NewGuid(),
                new Money(900m, "USD"), "MacBook Air", "Apple");

            var batch = new[] { productVersion1, productVersion2, productVersion3 };

            // act
            await repository.CreateProductVersions(batch);

            // assert
            var result1 = await repository.GetProductVersionById(productVersion1.Id);
            var result2 = await repository.GetProductVersionById(productVersion2.Id);
            var result3 = await repository.GetProductVersionById(productVersion3.Id);

            result1.ShouldNotBeNull();
            result1.Id.ShouldBe(productVersion1.Id);
            result1.Name.ShouldBe("Monitor 24");

            result2.ShouldNotBeNull();
            result2.Id.ShouldBe(productVersion2.Id);
            result2.Name.ShouldBe("Monitor 27");

            result3.ShouldNotBeNull();
            result3.Id.ShouldBe(productVersion3.Id);
            result3.Name.ShouldBe("MacBook Air");
        }
    }
}
