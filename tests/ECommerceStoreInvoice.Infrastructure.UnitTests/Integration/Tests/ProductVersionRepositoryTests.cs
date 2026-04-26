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
    }
}
