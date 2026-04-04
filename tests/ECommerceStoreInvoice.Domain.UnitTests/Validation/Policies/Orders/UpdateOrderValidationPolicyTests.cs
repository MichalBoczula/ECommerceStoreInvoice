using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Policies;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Policies.Orders
{
    public class UpdateOrderValidationPolicyTests
    {
        [Theory]
        [InlineData(OrderStatus.Paid)]
        [InlineData(OrderStatus.Cancelled)]
        public async Task Validate_WhenStatusMovesFromCreatedToAllowedTarget_ShouldReturnNoErrors(OrderStatus newStatus)
        {
            // Arrange
            var policy = new UpdateOrderValidationPolicy();
            var order = CreateOrderWithStatus(OrderStatus.Created);

            // Act
            var result = await policy.Validate((order, newStatus));

            // Assert
            result.IsValid.ShouldBeTrue();
            result.GetValidationErrors().Count.ShouldBe(0);
        }

        [Fact]
        public async Task Validate_WhenStatusTransitionIsNotAllowed_ShouldReturnError()
        {
            // Arrange
            var policy = new UpdateOrderValidationPolicy();
            var order = CreateOrderWithStatus(OrderStatus.Paid);

            // Act
            var result = await policy.Validate((order, OrderStatus.Cancelled));

            // Assert
            result.IsValid.ShouldBeFalse();
            result.GetValidationErrors().Count.ShouldBe(1);
            result.GetValidationErrors().First().Name.ShouldBe("UpdateOrderStatusTransitionValidationRule");
        }

        [Fact]
        public void Describe_ShouldIncludeStatusTransitionRule()
        {
            // Arrange
            var policy = new UpdateOrderValidationPolicy();

            // Act
            var descriptor = policy.Describe();

            // Assert
            descriptor.PolicyName.ShouldBe("UpdateOrderValidationPolicy");
            descriptor.Rules.Count.ShouldBe(1);
            descriptor.Rules.ShouldContain(r => r.RuleName == "UpdateOrderStatusTransitionValidationRule");
        }

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
