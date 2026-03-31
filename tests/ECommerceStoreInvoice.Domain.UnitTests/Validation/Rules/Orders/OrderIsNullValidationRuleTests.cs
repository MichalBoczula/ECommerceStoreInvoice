using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.Orders;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Rules.Orders
{
    public class OrderIsNullValidationRuleTests
    {
        [Fact]
        public async Task IsValid_OrderIsNull_ShouldReturnError()
        {
            // Arrange
            var rule = new OrderIsNullValidationRule();
            var validationResult = new ValidationResult();

            // Act
            await rule.IsValid(null!, validationResult);

            // Assert
            validationResult.GetValidationErrors().Count.ShouldBe(1);
            validationResult.GetValidationErrors().First().Message.ShouldBe("Order cannot be null.");
        }

        [Fact]
        public void Describe_ShouldReturnRuleDescriptor()
        {
            // Arrange
            var rule = new OrderIsNullValidationRule();

            // Act
            var descriptors = rule.Describe();

            // Assert
            descriptors.Count.ShouldBe(1);
            descriptors[0].Name.ShouldBe("OrderIsNullValidationRule");
            descriptors[0].Entity.ShouldBe("Order");
        }
    }
}
