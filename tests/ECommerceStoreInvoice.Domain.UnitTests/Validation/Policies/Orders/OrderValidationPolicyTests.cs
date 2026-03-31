using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Policies;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Policies.Orders
{
    public class OrderValidationPolicyTests
    {
        [Fact]
        public async Task Validate_OrderWithInvalidData_ShouldReturnErrors()
        {
            // Arrange
            var policy = new OrderValidationPolicy();
            var order = new Order(Guid.Empty, []);

            // Act
            var result = await policy.Validate(order);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.GetValidationErrors().Count.ShouldBe(2);
            result.GetValidationErrors().ShouldContain(e => e.Name == "ClientIdIsEmptyValidationRule");
            result.GetValidationErrors().ShouldContain(e => e.Name == "OrderLinesValidationRule");
        }

        [Fact]
        public async Task Validate_OrderIsValid_ShouldReturnNoErrors()
        {
            // Arrange
            var policy = new OrderValidationPolicy();
            var line = new Domain.AggregatesModel.OrderAggregate.ValueObjects.OrderLine(
                Guid.NewGuid(),
                "Keyboard",
                "Logi",
                new Domain.AggregatesModel.Common.ValueObjects.Money(99, "USD"),
                1);
            var order = new Order(Guid.NewGuid(), [line]);

            // Act
            var result = await policy.Validate(order);

            // Assert
            result.IsValid.ShouldBeTrue();
            result.GetValidationErrors().Count.ShouldBe(0);
        }

        [Fact]
        public void Describe_ShouldIncludeAllUnderlyingRuleDescriptors()
        {
            // Arrange
            var policy = new OrderValidationPolicy();

            // Act
            var descriptor = policy.Describe();

            // Assert
            descriptor.PolicyName.ShouldBe("OrderValidationPolicy");
            descriptor.Rules.Count.ShouldBe(3);
            descriptor.Rules.ShouldContain(r => r.RuleName == "OrderIsNullValidationRule");
            descriptor.Rules.ShouldContain(r => r.RuleName == "ClientIdIsEmptyValidationRule");
            descriptor.Rules.ShouldContain(r => r.RuleName == "OrderLinesValidationRule");
        }
    }
}
