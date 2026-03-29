using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ShoppingCarts;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Rules.ShoppingCartLines
{
    public class ShoppingCartLineMoneyValidationRuleTests
    {
        [Fact]
        public async Task IsValid_UnitPriceAmountIsNegative_ShouldReturnValidationError()
        {
            // Arrange
            var rule = new ShoppingCartLineMoneyValidationRule();
            var validationResult = new ValidationResult();
            var line = new ShoppingCartLine("Mouse", "Brand", new Money(-1, "USD"), 1);

            // Act
            await rule.IsValid(line, validationResult);

            // Assert
            validationResult.GetValidationErrors().Count.ShouldBe(1);
            validationResult.GetValidationErrors().ShouldContain(e => e.Message == "Unit price amount cannot be negative.");
        }

        [Fact]
        public async Task IsValid_UnitPriceAmountIsZero_ShouldNotReturnValidationError()
        {
            // Arrange
            var rule = new ShoppingCartLineMoneyValidationRule();
            var validationResult = new ValidationResult();
            var line = new ShoppingCartLine("Mouse", "Brand", new Money(0, "USD"), 1);

            // Act
            await rule.IsValid(line, validationResult);

            // Assert
            validationResult.GetValidationErrors().Count.ShouldBe(0);
        }

        [Fact]
        public void Describe_ShouldReturnExpectedDescriptor()
        {
            // Arrange
            var rule = new ShoppingCartLineMoneyValidationRule();

            // Act
            var descriptors = rule.Describe();

            // Assert
            descriptors.Count.ShouldBe(1);
            descriptors[0].Message.ShouldBe("Unit price amount cannot be negative.");
            descriptors[0].Name.ShouldBe("ShoppingCartLineMoneyValidationRule");
            descriptors[0].Entity.ShouldBe("ShoppingCartLine");
        }
    }
}
