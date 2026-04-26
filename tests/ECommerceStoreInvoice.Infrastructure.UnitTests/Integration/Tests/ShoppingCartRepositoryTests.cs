using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
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
            result.Lines.Single().Name.ShouldBe(shoppingCart.Lines.Single().Name);
            result.Lines.Single().Brand.ShouldBe(shoppingCart.Lines.Single().Brand);
            result.Lines.Single().UnitPrice.Amount.ShouldBe(shoppingCart.Lines.Single().UnitPrice.Amount);
            result.Lines.Single().UnitPrice.Currency.ShouldBe(shoppingCart.Lines.Single().UnitPrice.Currency);
            result.Lines.Single().Quantity.ShouldBe(shoppingCart.Lines.Single().Quantity);
            result.Total.Amount.ShouldBe(shoppingCart.Total.Amount);
            result.Total.Currency.ShouldBe(shoppingCart.Total.Currency);
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
            result.Total.Amount.ShouldBe(299.97m);
            result.Total.Currency.ShouldBe("USD");
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

            var originalShoppingCart = CreateShoppingCart(clientId, quantity: 1);
            await repository.CreateShoppingCart(originalShoppingCart);

            var updatedShoppingCart = ShoppingCart.Rehydrate(
                originalShoppingCart.Id,
                originalShoppingCart.ClientId,
                originalShoppingCart.CreatedAt,
                DateTime.UtcNow,
                [
                    new ShoppingCartLine(
                        Guid.NewGuid(),
                        "Laptop",
                        "Contoso",
                        new Money(2500m, "USD"),
                        2)
                ]);

            // act
            await repository.UpdateShoppingCart(updatedShoppingCart);

            // assert
            var result = await repository.GetShoppingCartByClientId(clientId);

            result.ShouldNotBeNull();
            result.Id.ShouldBe(originalShoppingCart.Id);
            result.ClientId.ShouldBe(clientId);
            result.Lines.Count.ShouldBe(1);
            result.Lines.Single().Name.ShouldBe("Laptop");
            result.Lines.Single().Brand.ShouldBe("Contoso");
            result.Lines.Single().UnitPrice.Amount.ShouldBe(2500m);
            result.Lines.Single().Quantity.ShouldBe(2);
            result.Total.Amount.ShouldBe(5000m);
            result.Total.Currency.ShouldBe("USD");
        }

        private static ShoppingCart CreateShoppingCart(Guid clientId, int quantity)
        {
            return ShoppingCart.Rehydrate(
                Guid.NewGuid(),
                clientId,
                DateTime.UtcNow,
                DateTime.UtcNow,
                [
                    new ShoppingCartLine(
                        Guid.NewGuid(),
                        "Monitor",
                        "Fabrikam",
                        new Money(99.99m, "USD"),
                        quantity)
                ]);
        }
    }
}
