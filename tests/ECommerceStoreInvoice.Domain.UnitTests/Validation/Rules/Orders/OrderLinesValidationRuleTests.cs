using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.Orders;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Rules.Orders
{
    public class OrderLinesValidationRuleTests
    {
        [Fact]
        public async Task IsValid_LinesAreEmpty_ShouldReturnError()
        {
            // Arrange
            var rule = new OrderLinesValidationRule();
            var validationResult = new ValidationResult();
            var order = new Order(Guid.NewGuid(), []);

            // Act
            await rule.IsValid(order, validationResult);

            // Assert
            validationResult.GetValidationErrors().Count.ShouldBe(1);
            validationResult.GetValidationErrors().First().Message.ShouldBe("Order lines cannot be empty.");
        }

        [Fact]
        public void Describe_ShouldReturnRuleDescriptors()
        {
            // Arrange
            var rule = new OrderLinesValidationRule();

            // Act
            var descriptors = rule.Describe();

            // Assert
            descriptors.Count.ShouldBe(2);
            descriptors.ShouldContain(d => d.Message == "Order lines cannot be null.");
            descriptors.ShouldContain(d => d.Message == "Order lines cannot be empty.");
        }
    }
}
