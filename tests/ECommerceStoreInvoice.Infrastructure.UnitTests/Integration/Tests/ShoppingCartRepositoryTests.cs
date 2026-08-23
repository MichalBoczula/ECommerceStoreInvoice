using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Infrastructure.UnitTests.Integration.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ECommerceStoreInvoice.Infrastructure.UnitTests.Integration.Tests
{
    public sealed class ShoppingCartRepositoryTests : IClassFixture<MongoDbTestFixture>
    {
        private readonly MongoDbTestFixture _fixture;

        public ShoppingCartRepositoryTests(MongoDbTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task CreateShoppingCart_ShouldSaveShoppingCart()
        {
            // arrange
            var databaseName = $"invoice-tests-{Guid.NewGuid():N}";

            await using var serviceProvider = TestServiceProviderFactory.Create(
                _fixture.ConnectionString,
                databaseName);

            var repository = serviceProvider
                .GetRequiredService<IShoppingCartRepository>();

            var clientId = Guid.NewGuid();
            var shoppingCart = CreateShoppingCart(clientId, quantity: 2);

            // act
            await repository.CreateShoppingCart(shoppingCart);

            // assert
            var result = await repository.GetShoppingCartByClientId(clientId);

            result.ShouldNotBeNull();
            result.Id.ShouldBe(shoppingCart.Id);
            result.ClientId.ShouldBe(shoppingCart.ClientId);
            result.Lines.Count.ShouldBe(1);
            result.Lines.Single().ProductId.ShouldBe(shoppingCart.Lines.Single().ProductId);
            result.Lines.Single().Quantity.ShouldBe(shoppingCart.Lines.Single().Quantity);
        }

        [Fact]
        public async Task GetShoppingCartByClientId_ShouldReturnShoppingCart_WhenExists()
        {
            // arrange
            var databaseName = $"invoice-tests-{Guid.NewGuid():N}";

            await using var serviceProvider = TestServiceProviderFactory.Create(
                _fixture.ConnectionString,
                databaseName);

            var repository = serviceProvider
                .GetRequiredService<IShoppingCartRepository>();

            var clientId = Guid.NewGuid();
            var shoppingCart = CreateShoppingCart(clientId, quantity: 3);

            await repository.CreateShoppingCart(shoppingCart);

            // act
            var result = await repository.GetShoppingCartByClientId(clientId);

            // assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(shoppingCart.Id);
            result.ClientId.ShouldBe(clientId);
            result.Lines.Count.ShouldBe(1);
            result.Lines.Single().Quantity.ShouldBe(3);
        }

        [Fact]
        public async Task GetShoppingCartByClientId_ShouldReturnNull_WhenShoppingCartDoesNotExist()
        {
            // arrange
            var databaseName = $"invoice-tests-{Guid.NewGuid():N}";

            await using var serviceProvider = TestServiceProviderFactory.Create(
                _fixture.ConnectionString,
                databaseName);

            var repository = serviceProvider
                .GetRequiredService<IShoppingCartRepository>();

            var clientId = Guid.NewGuid();

            // act
            var result = await repository.GetShoppingCartByClientId(clientId);

            // assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task UpdateShoppingCart_ShouldReplaceExistingShoppingCart()
        {
            // arrange
            var databaseName = $"invoice-tests-{Guid.NewGuid():N}";

            await using var serviceProvider = TestServiceProviderFactory.Create(
                _fixture.ConnectionString,
                databaseName);

            var repository = serviceProvider
                .GetRequiredService<IShoppingCartRepository>();

            var clientId = Guid.NewGuid();
            var newProductId = Guid.NewGuid();

            var originalShoppingCart = CreateShoppingCart(clientId, quantity: 1);
            await repository.CreateShoppingCart(originalShoppingCart);

            var updatedShoppingCart = ShoppingCart.Rehydrate(
                originalShoppingCart.Id,
                originalShoppingCart.ClientId,
                originalShoppingCart.CreatedAt,
                DateTime.UtcNow,
                [
                    new ShoppingCartLine(newProductId, 5)
                ]);

            // act
            await repository.UpdateShoppingCart(updatedShoppingCart);

            // assert
            var result = await repository.GetShoppingCartByClientId(clientId);

            result.ShouldNotBeNull();
            result.Id.ShouldBe(originalShoppingCart.Id);
            result.ClientId.ShouldBe(clientId);
            result.Lines.Count.ShouldBe(1);
            result.Lines.Single().ProductId.ShouldBe(newProductId);
            result.Lines.Single().Quantity.ShouldBe(5);
        }

        private static ShoppingCart CreateShoppingCart(Guid clientId, int quantity)
        {
            return ShoppingCart.Rehydrate(
                Guid.NewGuid(),
                clientId,
                DateTime.UtcNow,
                DateTime.UtcNow,
                [
                    new ShoppingCartLine(Guid.NewGuid(), quantity)
                ]);
        }
    }
}