using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.ExternalServices;
using ECommerceStoreInvoice.ExternalProviders.IntegrationTests.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ECommerceStoreInvoice.ExternalProviders.IntegrationTests.Tests
{
    public sealed class ExternalProductServiceClientTests : IClassFixture<ExternalProvidersApplicationFactory>
    {
        private readonly ExternalProvidersApplicationFactory _factory;

        public ExternalProductServiceClientTests(ExternalProvidersApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetProductsByIds_WhenListIsEmpty_ShouldReturnEmptyCollectionWithoutCallingApi()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var sut = scope.ServiceProvider.GetRequiredService<IProductServiceClient>();

            // Act
            var result = await sut.GetProductsByIds([]);

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetProductsByIds_WhenProductsDoNotExist_ShouldReturnEmptyCollection()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var sut = scope.ServiceProvider.GetRequiredService<IProductServiceClient>();

            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await sut.GetProductsByIds([nonExistentId]);

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetProductsByIds_WhenProductsExist_ShouldReturnMappedDomainSnapshots()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var sut = scope.ServiceProvider.GetRequiredService<IProductServiceClient>();

            // Using existing mobile phone ID
            var existingProductId = Guid.Parse("0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2");

            // Act
            var result = await sut.GetProductsByIds([existingProductId]);

            // Assert
            result.ShouldNotBeNull();
            result.ShouldNotBeEmpty();

            var snapshot = result.First(p => p.ProductId == existingProductId);
            snapshot.ProductId.ShouldBe(existingProductId);
            snapshot.Name.ShouldNotBeNullOrWhiteSpace();
            snapshot.Brand.ShouldNotBeNullOrWhiteSpace();
            snapshot.Price.Amount.ShouldBeGreaterThan(0);
            snapshot.Price.Currency.ShouldNotBeNullOrWhiteSpace();
        }
    }
}