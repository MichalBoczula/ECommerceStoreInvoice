using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Infrastructure.UnitTests.Integration.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ECommerceStoreInvoice.Infrastructure.UnitTests.Integration.Tests
{
    public sealed class OrderRepositoryTests : IClassFixture<MongoDbTestFixture>
    {
        private readonly MongoDbTestFixture _fixture;

        public OrderRepositoryTests(MongoDbTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task CreateOrder_ShouldSaveOrder()
        {
            // arrange
            var databaseName = $"invoice-tests-{Guid.NewGuid():N}";

            await using var serviceProvider = TestServiceProviderFactory.Create(
                _fixture.ConnectionString,
                databaseName);

            var repository = serviceProvider
                .GetRequiredService<IOrderRepository>();

            var clientId = Guid.NewGuid();
            var order = CreateOrder(clientId, quantity: 2);

            // act
            await repository.CreateOrder(order);

            // assert
            var result = await repository.GetOrderByOrderId(order.Id);

            result.ShouldNotBeNull();
            result.Id.ShouldBe(order.Id);
            result.ClientId.ShouldBe(order.ClientId);
            result.Lines.Count.ShouldBe(1);
            result.Lines.Single().ProductVersionId.ShouldBe(order.Lines.Single().ProductVersionId);
            result.Lines.Single().Quantity.ShouldBe(order.Lines.Single().Quantity);
            result.Status.ShouldBe(OrderStatus.Created);
        }

        [Fact]
        public async Task GetOrdersByClientId_ShouldReturnOrders_WhenExists()
        {
            // arrange
            var databaseName = $"invoice-tests-{Guid.NewGuid():N}";

            await using var serviceProvider = TestServiceProviderFactory.Create(
                _fixture.ConnectionString,
                databaseName);

            var repository = serviceProvider
                .GetRequiredService<IOrderRepository>();

            var clientId = Guid.NewGuid();
            var firstOrder = CreateOrder(clientId, quantity: 1);
            var secondOrder = CreateOrder(clientId, quantity: 3);
            var otherClientOrder = CreateOrder(Guid.NewGuid(), quantity: 5);

            await repository.CreateOrder(firstOrder);
            await repository.CreateOrder(secondOrder);
            await repository.CreateOrder(otherClientOrder);

            // act
            var result = await repository.GetOrdersByClientId(clientId);

            // assert
            result.Count.ShouldBe(2);
            result.All(x => x.ClientId == clientId).ShouldBeTrue();
            result.Select(x => x.Id).ShouldContain(firstOrder.Id);
            result.Select(x => x.Id).ShouldContain(secondOrder.Id);
            result.Select(x => x.Id).ShouldNotContain(otherClientOrder.Id);
        }

        [Fact]
        public async Task GetOrderByOrderId_ShouldReturnOrder_WhenExists()
        {
            // arrange
            var databaseName = $"invoice-tests-{Guid.NewGuid():N}";

            await using var serviceProvider = TestServiceProviderFactory.Create(
                _fixture.ConnectionString,
                databaseName);

            var repository = serviceProvider
                .GetRequiredService<IOrderRepository>();

            var clientId = Guid.NewGuid();
            var order = CreateOrder(clientId, quantity: 4);

            await repository.CreateOrder(order);

            // act
            var result = await repository.GetOrderByOrderId(order.Id);

            // assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(order.Id);
            result.ClientId.ShouldBe(clientId);
            result.Lines.Count.ShouldBe(1);
            result.Lines.Single().Quantity.ShouldBe(4);
            result.Lines.Single().ProductVersionId.ShouldBe(order.Lines.Single().ProductVersionId);
            result.Status.ShouldBe(OrderStatus.Created);
        }

        [Fact]
        public async Task UpdateOrder_ShouldPersistStatusAndTimestampWithOriginalLines()
        {
            await using var services = TestServiceProviderFactory.Create(
                _fixture.ConnectionString, $"invoice-tests-{Guid.NewGuid():N}");
            using var writeScope = services.CreateScope();
            var writer = writeScope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var order = CreateOrder(Guid.NewGuid(), quantity: 4);
            await writer.CreateOrder(order);
            var originalVersionId = order.Lines.Single().ProductVersionId;

            var loaded = (await writer.GetOrderByOrderId(order.Id))!;
            loaded.ChangeStatus(OrderStatus.Paid);
            await writer.UpdateOrder(loaded);

            using var readScope = services.CreateScope();
            var stored = (await readScope.ServiceProvider.GetRequiredService<IOrderRepository>()
                .GetOrderByOrderId(order.Id))!;
            stored.Status.ShouldBe(OrderStatus.Paid);
            stored.UpdatedAt.ShouldNotBeNull();
            stored.Lines.Single().ProductVersionId.ShouldBe(originalVersionId);
            stored.Lines.Single().Quantity.ShouldBe(4);
        }

        [Fact]
        public async Task UpdateOrder_WhenTwoReadersSawCreated_SecondWriteCannotOverwriteFirst()
        {
            await using var services = TestServiceProviderFactory.Create(
                _fixture.ConnectionString, $"invoice-tests-{Guid.NewGuid():N}");
            using var firstScope = services.CreateScope();
            using var secondScope = services.CreateScope();
            var first = firstScope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var second = secondScope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var order = CreateOrder(Guid.NewGuid(), quantity: 2);
            await first.CreateOrder(order);

            var firstCopy = (await first.GetOrderByOrderId(order.Id))!;
            var secondCopy = (await second.GetOrderByOrderId(order.Id))!;
            firstCopy.ChangeStatus(OrderStatus.Paid);
            secondCopy.ChangeStatus(OrderStatus.Cancelled);

            await first.UpdateOrder(firstCopy);
            var conflict = await Should.ThrowAsync<OrderWriteConflictException>(
                () => second.UpdateOrder(secondCopy));

            conflict.OrderId.ShouldBe(order.Id);
            using var readScope = services.CreateScope();
            var stored = (await readScope.ServiceProvider.GetRequiredService<IOrderRepository>()
                .GetOrderByOrderId(order.Id))!;
            stored.Status.ShouldBe(OrderStatus.Paid);
            stored.UpdatedAt.ShouldNotBeNull();
            stored.Lines.Single().Quantity.ShouldBe(2);
        }

        private static Order CreateOrder(Guid clientId, int quantity)
        {
            var lines = new List<OrderLine>
            {
                new(Guid.NewGuid(), quantity)
            };

            return Order.Rehydrate(
                Guid.NewGuid(),
                clientId,
                lines,
                DateTime.UtcNow,
                DateTime.UtcNow,
                OrderStatus.Created);
        }
    }
}