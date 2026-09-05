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

        // [Fact]
        // public async Task GetProductsByIds_WhenProductsExist_ShouldReturnMappedDomainSnapshots()
        // {
        //     // Arrange
        //     using var scope = _factory.Services.CreateScope();
        //     var sut = scope.ServiceProvider.GetRequiredService<IProductServiceClient>();

        //     var existingProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        //     var existingProductId = await SeedProductInExternalCatalogAsync();

        //     // Act
        //     var result = await sut.GetProductsByIds([existingProductId]);

        //     // Assert
        //     result.ShouldNotBeNull();
        //     result.ShouldNotBeEmpty();

        //     var snapshot = result.First(p => p.ProductId == existingProductId);
        //     snapshot.ProductId.ShouldBe(existingProductId);
        //     snapshot.Name.ShouldNotBeNullOrWhiteSpace();
        //     snapshot.Brand.ShouldNotBeNullOrWhiteSpace();
        //     snapshot.Price.Currency.ShouldBe("USD");
        //     snapshot.Price.Amount.ShouldBeGreaterThan(0);
        //     snapshot.Price.Currency.ShouldNotBeNullOrWhiteSpace();
        // }
    }
}