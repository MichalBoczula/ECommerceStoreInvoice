using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Domain.OrderAggregate
{
    public class OrderTests
    {
        [Fact]
        public void Ctor_ShouldInitializeOrderWithCreatedStatusAndLines()
        {
            // Arrange
            var clientId = Guid.NewGuid();
            var lines = new[]
            {
                new OrderLine(Guid.NewGuid(), 2),
                new OrderLine(Guid.NewGuid(), 3)
            };

            // Act
            var order = new Order(clientId, lines);

            // Assert
            order.Id.ShouldNotBe(Guid.Empty);
            order.ClientId.ShouldBe(clientId);
            order.Lines.Count.ShouldBe(2);
            order.Status.ShouldBe(OrderStatus.Created);
            order.UpdatedAt.ShouldBeNull();
            order.CreatedAt.ShouldBeInRange(DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
        }

        [Fact]
        public void Ctor_ShouldInitializeOrderWithEmptyLines_WhenNoLinesProvided()
        {
            // Arrange
            var clientId = Guid.NewGuid();

            // Act
            var order = new Order(clientId, []);

            // Assert
            order.ClientId.ShouldBe(clientId);
            order.Lines.ShouldBeEmpty();
            order.Status.ShouldBe(OrderStatus.Created);
            order.UpdatedAt.ShouldBeNull();
        }

        [Fact]
        public void ChangeStatus_ShouldUpdateStatusAndSetUpdatedAt()
        {
            // Arrange
            var order = new Order(
                Guid.NewGuid(),
                [
                    new OrderLine(Guid.NewGuid(), 1)
                ]);

            order.UpdatedAt.ShouldBeNull();

            // Act
            Thread.Sleep(1);
            order.ChangeStatus(OrderStatus.Cancelled);

            // Assert
            order.Status.ShouldBe(OrderStatus.Cancelled);
            order.UpdatedAt.ShouldNotBeNull();
            order.UpdatedAt.Value.ShouldBeGreaterThan(order.CreatedAt);
        }

        [Fact]
        public void Rehydrate_ShouldUseProvidedState()
        {
            // Arrange
            var id = Guid.NewGuid();
            var clientId = Guid.NewGuid();
            var lines = new[]
            {
                new OrderLine(Guid.NewGuid(), 2)
            };
            var createdAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var updatedAt = new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc);

            // Act
            var order = Order.Rehydrate(id, clientId, lines, createdAt, updatedAt, OrderStatus.Paid);

            // Assert
            order.Id.ShouldBe(id);
            order.ClientId.ShouldBe(clientId);
            order.Lines.Count.ShouldBe(1);
            order.CreatedAt.ShouldBe(createdAt);
            order.UpdatedAt.ShouldBe(updatedAt);
            order.Status.ShouldBe(OrderStatus.Paid);
        }
    }
}