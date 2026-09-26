using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Infrastructure.UnitTests.Integration.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Shouldly;

namespace ECommerceStoreInvoice.Infrastructure.UnitTests.Integration.Tests;

public sealed class OrderCheckoutTransactionTests(MongoDbTestFixture fixture) : IClassFixture<MongoDbTestFixture>
{
    [Fact]
    public async Task TransactionLifecycle_RejectsNestedBeginAndCommitWithoutAnActiveSession()
    {
        await using var services = TestServiceProviderFactory.Create(
            fixture.ConnectionString, $"invoice-checkout-{Guid.NewGuid():N}");
        using var scope = services.CreateScope();
        var transaction = scope.ServiceProvider.GetRequiredService<IOrderWriteTransaction>();

        await Should.ThrowAsync<InvalidOperationException>(() => transaction.CommitAsync());
        await transaction.RollbackAsync();
        await transaction.BeginAsync();
        await Should.ThrowAsync<InvalidOperationException>(() => transaction.BeginAsync());
        await transaction.RollbackAsync();
        await transaction.BeginAsync();
        var orders = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var order = new Order(Guid.NewGuid(), [new OrderLine(Guid.NewGuid(), 1)]);
        await orders.CreateOrder(order);
        await transaction.CommitAsync();
        (await orders.GetOrderByOrderId(order.Id)).ShouldNotBeNull();
        await Should.ThrowAsync<InvalidOperationException>(() => transaction.CommitAsync());
    }

    [Fact]
    public async Task CheckoutCommitsProductVersionOrderAndClearedCartTogether()
    {
        await using var services = TestServiceProviderFactory.Create(
            fixture.ConnectionString, $"invoice-checkout-{Guid.NewGuid():N}");
        using var scope = services.CreateScope();
        var carts = scope.ServiceProvider.GetRequiredService<IShoppingCartRepository>();
        var versions = scope.ServiceProvider.GetRequiredService<IProductVersionRepository>();
        var orders = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var transaction = scope.ServiceProvider.GetRequiredService<IOrderWriteTransaction>();
        var cart = CreateCart();
        await carts.CreateShoppingCart(cart);
        var version = CreateVersion(cart.Lines.Single().ProductId);
        var order = new Order(cart.ClientId, [new OrderLine(version.Id, 2)]);

        await transaction.BeginAsync();
        await versions.CreateProductVersions([version]);
        await orders.CreateOrder(order);
        cart.Clear();
        await carts.UpdateShoppingCart(cart);
        await transaction.CommitAsync();

        (await versions.GetProductVersionById(version.Id)).ShouldNotBeNull();
        (await orders.GetOrderByOrderId(order.Id)).ShouldNotBeNull();
        (await carts.GetShoppingCartByClientId(cart.ClientId))!.Lines.ShouldBeEmpty();
    }

    [Fact]
    public async Task CartWriteFailureRollsBackProductVersionsAndOrderAndKeepsOriginalCart()
    {
        await using var services = TestServiceProviderFactory.Create(
            fixture.ConnectionString, $"invoice-checkout-{Guid.NewGuid():N}");
        using var scope = services.CreateScope();
        var carts = scope.ServiceProvider.GetRequiredService<IShoppingCartRepository>();
        var versions = scope.ServiceProvider.GetRequiredService<IProductVersionRepository>();
        var orders = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var transaction = scope.ServiceProvider.GetRequiredService<IOrderWriteTransaction>();
        var cart = CreateCart();
        await carts.CreateShoppingCart(cart);
        var version = CreateVersion(cart.Lines.Single().ProductId);
        var order = new Order(cart.ClientId, [new OrderLine(version.Id, 2)]);

        await transaction.BeginAsync();
        await versions.CreateProductVersions([version]);
        await orders.CreateOrder(order);
        var missingCart = ShoppingCart.Rehydrate(
            Guid.NewGuid(), cart.ClientId, cart.CreatedAt, cart.UpdatedAt, cart.Lines);
        missingCart.Clear();
        await Should.ThrowAsync<InvalidOperationException>(() => carts.UpdateShoppingCart(missingCart));
        await transaction.RollbackAsync();

        (await versions.GetProductVersionById(version.Id)).ShouldBeNull();
        (await orders.GetOrderByOrderId(order.Id)).ShouldBeNull();
        (await carts.GetShoppingCartByClientId(cart.ClientId))!.Lines.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData("Snapshots")]
    [InlineData("Order")]
    [InlineData("Cart")]
    public async Task RollbackAfterCheckoutStep_KeepsCartAndRemovesAllPartialWrites(string completedStep)
    {
        await using var services = TestServiceProviderFactory.Create(
            fixture.ConnectionString, $"invoice-checkout-{Guid.NewGuid():N}");
        using var scope = services.CreateScope();
        var carts = scope.ServiceProvider.GetRequiredService<IShoppingCartRepository>();
        var versions = scope.ServiceProvider.GetRequiredService<IProductVersionRepository>();
        var orders = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var transaction = scope.ServiceProvider.GetRequiredService<IOrderWriteTransaction>();
        var cart = CreateCart();
        await carts.CreateShoppingCart(cart);
        var version = CreateVersion(cart.Lines.Single().ProductId);
        var order = new Order(cart.ClientId, [new OrderLine(version.Id, 2)]);

        await transaction.BeginAsync();
        await versions.CreateProductVersions([version]);
        if (completedStep != "Snapshots")
        {
            await orders.CreateOrder(order);
            if (completedStep == "Cart")
            {
                cart.Clear();
                await carts.UpdateShoppingCart(cart);
            }
        }

        await transaction.RollbackAsync();

        using var readScope = services.CreateScope();
        var persistedVersions = readScope.ServiceProvider.GetRequiredService<IProductVersionRepository>();
        var persistedOrders = readScope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var persistedCarts = readScope.ServiceProvider.GetRequiredService<IShoppingCartRepository>();
        (await persistedVersions.GetProductVersionById(version.Id)).ShouldBeNull();
        (await persistedOrders.GetOrderByOrderId(order.Id)).ShouldBeNull();
        var storedCart = (await persistedCarts.GetShoppingCartByClientId(cart.ClientId))!;
        storedCart.Lines.Single().ProductId.ShouldBe(version.ProductId);
        storedCart.Lines.Single().Quantity.ShouldBe(2);
    }

    [Fact]
    public async Task ConcurrentCheckoutsOfSameCart_OnlyCommittedTransactionKeepsItsOrderAndSnapshot()
    {
        await using var services = TestServiceProviderFactory.Create(
            fixture.ConnectionString, $"invoice-checkout-{Guid.NewGuid():N}");
        using var firstScope = services.CreateScope();
        using var secondScope = services.CreateScope();
        var firstCartRepo = firstScope.ServiceProvider.GetRequiredService<IShoppingCartRepository>();
        var secondCartRepo = secondScope.ServiceProvider.GetRequiredService<IShoppingCartRepository>();
        var firstVersions = firstScope.ServiceProvider.GetRequiredService<IProductVersionRepository>();
        var secondVersions = secondScope.ServiceProvider.GetRequiredService<IProductVersionRepository>();
        var firstOrders = firstScope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var secondOrders = secondScope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var firstTransaction = firstScope.ServiceProvider.GetRequiredService<IOrderWriteTransaction>();
        var secondTransaction = secondScope.ServiceProvider.GetRequiredService<IOrderWriteTransaction>();
        var cart = CreateCart();
        await firstCartRepo.CreateShoppingCart(cart);
        var secondCart = (await secondCartRepo.GetShoppingCartByClientId(cart.ClientId))!;
        var firstVersion = CreateVersion(cart.Lines.Single().ProductId);
        var secondVersion = CreateVersion(cart.Lines.Single().ProductId);
        var firstOrder = new Order(cart.ClientId, [new OrderLine(firstVersion.Id, 2)]);
        var secondOrder = new Order(cart.ClientId, [new OrderLine(secondVersion.Id, 2)]);

        await firstTransaction.BeginAsync();
        await secondTransaction.BeginAsync();
        await firstVersions.CreateProductVersions([firstVersion]);
        await secondVersions.CreateProductVersions([secondVersion]);
        await firstOrders.CreateOrder(firstOrder);
        await secondOrders.CreateOrder(secondOrder);

        cart.Clear();
        await firstCartRepo.UpdateShoppingCart(cart);
        await firstTransaction.CommitAsync();
        secondCart.Clear();
        await Should.ThrowAsync<MongoException>(() => secondCartRepo.UpdateShoppingCart(secondCart));
        await secondTransaction.RollbackAsync();

        using var readScope = services.CreateScope();
        var orders = readScope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var versions = readScope.ServiceProvider.GetRequiredService<IProductVersionRepository>();
        var carts = readScope.ServiceProvider.GetRequiredService<IShoppingCartRepository>();
        var persistedOrders = await orders.GetOrdersByClientId(cart.ClientId);
        persistedOrders.Select(x => x.Id).ShouldBe([firstOrder.Id]);
        (await versions.GetProductVersionById(firstVersion.Id)).ShouldNotBeNull();
        (await versions.GetProductVersionById(secondVersion.Id)).ShouldBeNull();
        (await carts.GetShoppingCartByClientId(cart.ClientId))!.Lines.ShouldBeEmpty();
    }

    private static ShoppingCart CreateCart()
    {
        var cart = new ShoppingCart(Guid.NewGuid());
        cart.ReplaceLines([new ShoppingCartLine(Guid.NewGuid(), 2)]);
        return cart;
    }

    private static ProductVersion CreateVersion(Guid productId) =>
        new(productId, new Money(10m, "USD"), "Phone", "Brand");
}
