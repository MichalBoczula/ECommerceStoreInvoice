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
    }
}
