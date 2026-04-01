using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ProductVersions;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Rules.ProductVersions
{
    public class ProductVersionIsNullValidationRuleTests
    {
        [Fact]
        public async Task IsValid_EntityIsNull_ShouldReturnValidationError()
        {
            // Arrange
            var rule = new ProductVersionIsNullValidationRule();
            var validationResult = new ValidationResult();

            // Act
            await rule.IsValid(null!, validationResult);

            // Assert
            validationResult.GetValidationErrors().Count.ShouldBe(1);
            validationResult.GetValidationErrors().First().Message.ShouldBe("Product version cannot be null.");
        }

        [Fact]
        public void Describe_ShouldReturnExpectedDescriptor()
        {
            // Arrange
            var rule = new ProductVersionIsNullValidationRule();

            // Act
            var descriptors = rule.Describe();

            // Assert
            descriptors.Count.ShouldBe(1);
            descriptors[0].Message.ShouldBe("Product version cannot be null.");
            descriptors[0].Name.ShouldBe("ProductVersionIsNullValidationRule");
            descriptors[0].Entity.ShouldBe("ProductVersion");
        }
    }
}
