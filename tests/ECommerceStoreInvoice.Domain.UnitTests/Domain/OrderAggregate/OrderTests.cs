using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Domain.OrderAggregate
{
    public class OrderTests
    {
        [Fact]
        public void Ctor_ShouldInitializeOrderWithCreatedStatusAndComputedTotal()
        {
            // Arrange
            var clientId = Guid.NewGuid();
            var lines = new[]
            {
                new OrderLine(Guid.NewGuid(), "Mouse", "BrandA", new Money(100, "USD"), 2),
                new OrderLine(Guid.NewGuid(), "Keyboard", "BrandB", new Money(50, "USD"), 3)
            };

            // Act
            var order = new Order(clientId, lines);

            // Assert
            order.ClientId.ShouldBe(clientId);
            order.Lines.Count.ShouldBe(2);
            order.Status.ShouldBe(OrderStatus.Created);
            order.Total.Amount.ShouldBe(350);
            order.Total.Currency.ShouldBe("USD");
            order.UpdatedAt.ShouldBeGreaterThanOrEqualTo(order.CreatedAt);
        }

        [Fact]
        public void Ctor_ShouldDefaultTotalCurrencyToUsd_WhenNoLinesProvided()
        {
            // Arrange
            var clientId = Guid.NewGuid();

            // Act
            var order = new Order(clientId, []);

            // Assert
            order.Total.Amount.ShouldBe(0);
            order.Total.Currency.ShouldBe("USD");
        }

        [Fact]
        public void ChangeOrderStatus_ShouldUpdateStatusAndUpdatedAt()
        {
            // Arrange
            var order = new Order(Guid.NewGuid(),
            [
                new OrderLine(Guid.NewGuid(), "Mouse", "BrandA", new Money(100, "USD"), 1)
            ]);
            var originalUpdatedAt = order.UpdatedAt;

            // Act
            Thread.Sleep(5);
            order.ChangeOrderStatus(OrderStatus.Cancelled);

            // Assert
            order.Status.ShouldBe(OrderStatus.Cancelled);
            order.UpdatedAt.ShouldBeGreaterThan(originalUpdatedAt);
        }

        [Fact]
        public void Rehydrate_ShouldUseProvidedState()
        {
            // Arrange
            var id = Guid.NewGuid();
            var clientId = Guid.NewGuid();
            var lines = new[]
            {
                new OrderLine(Guid.NewGuid(), "Headphones", "BrandC", new Money(75, "eur"), 2)
            };
            var createdAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var updatedAt = new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc);
            var total = new Money(150, "EUR");

            // Act
            var order = Order.Rehydrate(id, clientId, lines, createdAt, updatedAt, OrderStatus.Paid, total);

            // Assert
            order.Id.ShouldBe(id);
            order.ClientId.ShouldBe(clientId);
            order.Lines.Count.ShouldBe(1);
            order.CreatedAt.ShouldBe(createdAt);
            order.UpdatedAt.ShouldBe(updatedAt);
            order.Status.ShouldBe(OrderStatus.Paid);
            order.Total.ShouldBe(total);
        }
    }
}
