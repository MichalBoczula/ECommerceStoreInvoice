using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ProductVersions;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Rules.ProductVersions
{
    public class ProductVersionPriceValidationRuleTests
    {
        [Fact]
        public async Task IsValid_PriceAmountIsZero_ShouldReturnValidationError()
        {
            // Arrange
            var rule = new ProductVersionPriceValidationRule();
            var validationResult = new ValidationResult();
            var productVersion = new ProductVersion(Guid.NewGuid(), new Money(0, "USD"), "Keyboard", "Logi");

            // Act
            await rule.IsValid(productVersion, validationResult);

            // Assert
            validationResult.GetValidationErrors().Count.ShouldBe(1);
            validationResult.GetValidationErrors().First().Message.ShouldBe("Price amount must be greater than zero.");
        }

        [Fact]
        public void Describe_ShouldReturnExpectedDescriptor()
        {
            // Arrange
            var rule = new ProductVersionPriceValidationRule();

            // Act
            var descriptors = rule.Describe();

            // Assert
            descriptors.Count.ShouldBe(1);
            descriptors[0].Message.ShouldBe("Price amount must be greater than zero.");
            descriptors[0].Name.ShouldBe("ProductVersionPriceValidationRule");
            descriptors[0].Entity.ShouldBe("ProductVersion");
        }
    }
}
