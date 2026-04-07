using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ShoppingCarts;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Rules.ShoppingCartLines
{
    public class ShoppingCartLineQuantityValidationRuleTests
    {
        [Fact]
        public async Task IsValid_QuantityIsZero_ShouldReturnValidationError()
        {
            // Arrange
            var rule = new ShoppingCartLineQuantityValidationRule();
            var validationResult = new ValidationResult();
            var line = new ShoppingCartLine(Guid.NewGuid(), "Mouse", "Brand", new Money(20, "USD"), 0);

            // Act
            await rule.IsValid(line, validationResult);

            // Assert
            validationResult.GetValidationErrors().Count.ShouldBe(1);
            validationResult.GetValidationErrors().ShouldContain(e => e.Message == "Quantity must be greater than zero.");
        }

        [Fact]
        public async Task IsValid_QuantityIsPositive_ShouldNotReturnValidationError()
        {
            // Arrange
            var rule = new ShoppingCartLineQuantityValidationRule();
            var validationResult = new ValidationResult();
            var line = new ShoppingCartLine(Guid.NewGuid(), "Mouse", "Brand", new Money(20, "USD"), 1);

            // Act
            await rule.IsValid(line, validationResult);

            // Assert
            validationResult.GetValidationErrors().Count.ShouldBe(0);
        }

        [Fact]
        public void Describe_ShouldReturnExpectedDescriptor()
        {
            // Arrange
            var rule = new ShoppingCartLineQuantityValidationRule();

            // Act
            var descriptors = rule.Describe();

            // Assert
            descriptors.Count.ShouldBe(1);
            descriptors[0].Message.ShouldBe("Quantity must be greater than zero.");
            descriptors[0].Name.ShouldBe("ShoppingCartLineQuantityValidationRule");
            descriptors[0].Entity.ShouldBe("ShoppingCartLine");
        }
    }
}
