using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ShoppingCarts;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Rules.ShoppingCartLines
{
    public class ShoppingCartLineIsNullValidationRuleTests
    {
        [Fact]
        public async Task IsValid_LineIsNull_ShouldReturnValidationError()
        {
            // Arrange
            var rule = new ShoppingCartLineIsNullValidationRule();
            var validationResult = new ValidationResult();

            // Act
            await rule.IsValid(null!, validationResult);

            // Assert
            validationResult.GetValidationErrors().Count.ShouldBe(1);
            validationResult.GetValidationErrors().ShouldContain(e => e.Message == "Shopping cart line cannot be null.");
            validationResult.GetValidationErrors().ShouldContain(e => e.Name == "ShoppingCartLineIsNullValidationRule");
            validationResult.GetValidationErrors().ShouldContain(e => e.Entity == "ShoppingCartLine");
        }

        [Fact]
        public void Describe_ShouldReturnExpectedDescriptor()
        {
            // Arrange
            var rule = new ShoppingCartLineIsNullValidationRule();

            // Act
            var descriptors = rule.Describe();

            // Assert
            descriptors.Count.ShouldBe(1);
            descriptors[0].Message.ShouldBe("Shopping cart line cannot be null.");
            descriptors[0].Name.ShouldBe("ShoppingCartLineIsNullValidationRule");
            descriptors[0].Entity.ShouldBe("ShoppingCartLine");
        }
    }
}
