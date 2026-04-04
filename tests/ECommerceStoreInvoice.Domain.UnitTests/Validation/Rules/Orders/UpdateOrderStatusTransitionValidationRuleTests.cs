using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.Orders;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Rules.Orders
{
    public class UpdateOrderStatusTransitionValidationRuleTests
    {
        [Theory]
        [InlineData(OrderStatus.Paid)]
        [InlineData(OrderStatus.Cancelled)]
        public async Task IsValid_WhenStatusMovesFromCreatedToAllowedTarget_ShouldReturnNoErrors(OrderStatus newStatus)
        {
            // Arrange
            var rule = new UpdateOrderStatusTransitionValidationRule();
            var validationResult = new ValidationResult();
            var order = CreateCreatedOrder();

            // Act
            await rule.IsValid((order, newStatus), validationResult);

            // Assert
            validationResult.IsValid.ShouldBeTrue();
            validationResult.GetValidationErrors().Count.ShouldBe(0);
        }

        [Theory]
        [InlineData(OrderStatus.Created, OrderStatus.Created)]
        [InlineData(OrderStatus.Paid, OrderStatus.Cancelled)]
        [InlineData(OrderStatus.Cancelled, OrderStatus.Paid)]
        public async Task IsValid_WhenStatusTransitionIsNotAllowed_ShouldReturnValidationError(OrderStatus currentStatus, OrderStatus newStatus)
        {
            // Arrange
            var rule = new UpdateOrderStatusTransitionValidationRule();
            var validationResult = new ValidationResult();
            var order = CreateOrderWithStatus(currentStatus);

            // Act
            await rule.IsValid((order, newStatus), validationResult);

            // Assert
            validationResult.IsValid.ShouldBeFalse();
            validationResult.GetValidationErrors().Count.ShouldBe(1);
            validationResult.GetValidationErrors().First().Name.ShouldBe("UpdateOrderStatusTransitionValidationRule");
        }

        [Fact]
        public void Describe_ShouldReturnRuleDescriptor()
        {
            // Arrange
            var rule = new UpdateOrderStatusTransitionValidationRule();

            // Act
            var descriptors = rule.Describe();

            // Assert
            descriptors.Count.ShouldBe(1);
            descriptors[0].Name.ShouldBe("UpdateOrderStatusTransitionValidationRule");
            descriptors[0].Entity.ShouldBe("Order");
        }

        private static Order CreateCreatedOrder() => CreateOrderWithStatus(OrderStatus.Created);

        private static Order CreateOrderWithStatus(OrderStatus status)
        {
            var line = new OrderLine(Guid.NewGuid(), "Keyboard", "Logi", new Money(100, "USD"), 1);

            return Order.Rehydrate(
                Guid.NewGuid(),
                Guid.NewGuid(),
                [line],
                DateTime.UtcNow,
                DateTime.UtcNow,
                status,
                new Money(100, "USD"));
        }
    }
}
