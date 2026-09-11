using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;
using ECommerceStoreInvoice.Infrastructure.UnitTests.Integration.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ECommerceStoreInvoice.Infrastructure.UnitTests.Integration.Tests
{
    public sealed class OrderWithProductVersionsRepositoryTests : IClassFixture<MongoDbTestFixture>
    {
        private readonly MongoDbTestFixture _fixture;

        public OrderWithProductVersionsRepositoryTests(MongoDbTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GetOrderWithProductVersionsById_ShouldReturnOrderWithProductVersions_WhenOrderExists()
        {
            // arrange
            var databaseName = $"invoice-tests-{Guid.NewGuid():N}";

            await using var serviceProvider = TestServiceProviderFactory.Create(
                _fixture.ConnectionString,
                databaseName);

            var orderRepository = serviceProvider.GetRequiredService<IOrderRepository>();
            var productVersionRepository = serviceProvider.GetRequiredService<IProductVersionRepository>();

            var firstProductVersion = new ProductVersion(
                Guid.NewGuid(),
                new Money(129.99m, "USD"),
                "Mechanical Keyboard",
                "KeyMaster");
            var secondProductVersion = new ProductVersion(
                Guid.NewGuid(),
                new Money(79.50m, "USD"),
                "Gaming Mouse",
                "ClickPro");
            var productVersions = new[] { firstProductVersion, secondProductVersion };
            var order = new Order(
                Guid.NewGuid(),
                new[]
                {
                    new OrderLine(firstProductVersion.Id, 1),
                    new OrderLine(secondProductVersion.Id, 2)
                });

            await productVersionRepository.CreateProductVersions(productVersions);
            await orderRepository.CreateOrder(order);

            // act
            var result = await orderRepository.GetOrderWithProductVersionsById(order.Id);

            // assert
            result.ShouldNotBeNull();
            result.Value.Order.Id.ShouldBe(order.Id);
            result.Value.Order.ClientId.ShouldBe(order.ClientId);
            result.Value.Order.Lines.Count.ShouldBe(2);
            result.Value.ProductVersions.Count.ShouldBe(2);
            result.Value.ProductVersions.Select(x => x.Id).ShouldBe(
                productVersions.Select(x => x.Id),
                ignoreOrder: true);
        }

        [Fact]
        public async Task GetOrderWithProductVersionsById_ShouldReturnNull_WhenOrderDoesNotExist()
        {
            // arrange
            var databaseName = $"invoice-tests-{Guid.NewGuid():N}";

            await using var serviceProvider = TestServiceProviderFactory.Create(
                _fixture.ConnectionString,
                databaseName);

            var orderRepository = serviceProvider.GetRequiredService<IOrderRepository>();

            // act
            var result = await orderRepository.GetOrderWithProductVersionsById(Guid.NewGuid());

            // assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task GetOrdersWithProductVersionsByClientId_ShouldReturnOneOrderWithProductVersions_WhenOneOrderExists()
        {
            // arrange
            var databaseName = $"invoice-tests-{Guid.NewGuid():N}";

            await using var serviceProvider = TestServiceProviderFactory.Create(
                _fixture.ConnectionString,
                databaseName);

            var orderRepository = serviceProvider.GetRequiredService<IOrderRepository>();
            var productVersionRepository = serviceProvider.GetRequiredService<IProductVersionRepository>();

            var clientId = Guid.NewGuid();
            var productVersion = new ProductVersion(
                Guid.NewGuid(),
                new Money(129.99m, "USD"),
                "Mechanical Keyboard",
                "KeyMaster");
            var order = new Order(
                clientId,
                new[] { new OrderLine(productVersion.Id, 1) });

            await productVersionRepository.CreateProductVersions(new[] { productVersion });
            await orderRepository.CreateOrder(order);

            // act
            var result = await orderRepository.GetOrdersWithProductVersionsByClientId(clientId);

            // assert
            result.Count.ShouldBe(1);
            var orderWithProductVersions = result.Single();
            orderWithProductVersions.Order.Id.ShouldBe(order.Id);
            orderWithProductVersions.Order.ClientId.ShouldBe(clientId);
            orderWithProductVersions.ProductVersions.Count.ShouldBe(1);
            orderWithProductVersions.ProductVersions.Single().Id.ShouldBe(productVersion.Id);
        }

        [Fact]
        public async Task GetOrdersWithProductVersionsByClientId_ShouldReturnEmptyList_WhenClientHasNoOrders()
        {
            // arrange
            var databaseName = $"invoice-tests-{Guid.NewGuid():N}";

            await using var serviceProvider = TestServiceProviderFactory.Create(
                _fixture.ConnectionString,
                databaseName);

            var orderRepository = serviceProvider.GetRequiredService<IOrderRepository>();

            // act
            var result = await orderRepository.GetOrdersWithProductVersionsByClientId(Guid.NewGuid());

            // assert
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetOrdersWithProductVersionsByClientId_ShouldReturnTwoOrdersWithProductVersions_WhenTwoOrdersExist()
        {
            // arrange
            var databaseName = $"invoice-tests-{Guid.NewGuid():N}";

            await using var serviceProvider = TestServiceProviderFactory.Create(
                _fixture.ConnectionString,
                databaseName);

            var orderRepository = serviceProvider.GetRequiredService<IOrderRepository>();
            var productVersionRepository = serviceProvider.GetRequiredService<IProductVersionRepository>();

            var clientId = Guid.NewGuid();
            var firstProductVersion = new ProductVersion(
                Guid.NewGuid(),
                new Money(129.99m, "USD"),
                "Mechanical Keyboard",
                "KeyMaster");
            var secondProductVersion = new ProductVersion(
                Guid.NewGuid(),
                new Money(79.50m, "USD"),
                "Gaming Mouse",
                "ClickPro");
            var productVersions = new[] { firstProductVersion, secondProductVersion };
            var firstOrder = new Order(
                clientId,
                new[] { new OrderLine(firstProductVersion.Id, 1) });
            var secondOrder = new Order(
                clientId,
                new[] { new OrderLine(secondProductVersion.Id, 2) });

            await productVersionRepository.CreateProductVersions(productVersions);
            await orderRepository.CreateOrder(firstOrder);
            await orderRepository.CreateOrder(secondOrder);

            // act
            var result = await orderRepository.GetOrdersWithProductVersionsByClientId(clientId);

            // assert
            result.Count.ShouldBe(2);
            result.Select(x => x.Order.Id).ShouldBe(
                new[] { firstOrder.Id, secondOrder.Id },
                ignoreOrder: true);
            result.ShouldAllBe(x => x.Order.ClientId == clientId);

            var firstResult = result.Single(x => x.Order.Id == firstOrder.Id);
            firstResult.ProductVersions.Count.ShouldBe(1);
            firstResult.ProductVersions.Single().Id.ShouldBe(firstProductVersion.Id);

            var secondResult = result.Single(x => x.Order.Id == secondOrder.Id);
            secondResult.ProductVersions.Count.ShouldBe(1);
            secondResult.ProductVersions.Single().Id.ShouldBe(secondProductVersion.Id);
        }
    }
}
