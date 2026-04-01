using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ProductVersions;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Rules.ProductVersions
{
    public class ProductVersionProductIdValidationRuleTests
    {
        [Fact]
        public async Task IsValid_ProductIdIsEmpty_ShouldReturnValidationError()
        {
            // Arrange
            var rule = new ProductVersionProductIdValidationRule();
            var validationResult = new ValidationResult();
            var productVersion = new ProductVersion(Guid.Empty, new Money(20, "USD"), "Keyboard", "Logi");

            // Act
            await rule.IsValid(productVersion, validationResult);

            // Assert
            validationResult.GetValidationErrors().Count.ShouldBe(1);
            validationResult.GetValidationErrors().First().Message.ShouldBe("ProductId cannot be empty.");
        }

        [Fact]
        public void Describe_ShouldReturnExpectedDescriptor()
        {
            // Arrange
            var rule = new ProductVersionProductIdValidationRule();

            // Act
            var descriptors = rule.Describe();

            // Assert
            descriptors.Count.ShouldBe(1);
            descriptors[0].Message.ShouldBe("ProductId cannot be empty.");
            descriptors[0].Name.ShouldBe("ProductVersionProductIdValidationRule");
            descriptors[0].Entity.ShouldBe("ProductVersion");
        }
    }
}
