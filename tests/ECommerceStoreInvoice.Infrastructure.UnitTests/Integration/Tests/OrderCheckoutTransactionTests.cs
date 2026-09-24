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
using Shouldly;

namespace ECommerceStoreInvoice.Infrastructure.UnitTests.Integration.Tests;

public sealed class OrderCheckoutTransactionTests(MongoDbTestFixture fixture) : IClassFixture<MongoDbTestFixture>
{
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

    private static ShoppingCart CreateCart()
    {
        var cart = new ShoppingCart(Guid.NewGuid());
        cart.ReplaceLines([new ShoppingCartLine(Guid.NewGuid(), 2)]);
        return cart;
    }

    private static ProductVersion CreateVersion(Guid productId) =>
        new(productId, new Money(10m, "USD"), "Phone", "Brand");
}
