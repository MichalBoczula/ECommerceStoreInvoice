using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ShoppingCarts;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Rules.ShoppingCartLines
{
    public class ShoppingCartLineStringsValidationRuleTests
    {
        [Fact]
        public async Task IsValid_NameBrandAndCurrencyAreInvalid_ShouldReturnFourErrors()
        {
            // Arrange
            var rule = new ShoppingCartLineStringsValidationRule();
            var validationResult = new ValidationResult();
            var line = new ShoppingCartLine(" ", "", new Money(10, ""), 1);

            // Act
            await rule.IsValid(line, validationResult);

            // Assert
            validationResult.GetValidationErrors().Count.ShouldBe(4);
            validationResult.GetValidationErrors().ShouldContain(e => e.Message == "Name cannot be null or whitespace.");
            validationResult.GetValidationErrors().ShouldContain(e => e.Message == "Brand cannot be null or whitespace.");
            validationResult.GetValidationErrors().ShouldContain(e => e.Message == "Unit price currency cannot be null or whitespace.");
            validationResult.GetValidationErrors().ShouldContain(e => e.Message == "Unit price currency must be USD.");
        }

        [Fact]
        public async Task IsValid_CurrencyIsUsdCaseInsensitive_ShouldNotReturnErrors()
        {
            // Arrange
            var rule = new ShoppingCartLineStringsValidationRule();
            var validationResult = new ValidationResult();
            var line = new ShoppingCartLine("Keyboard", "Brand", new Money(10, "uSd"), 1);

            // Act
            await rule.IsValid(line, validationResult);

            // Assert
            validationResult.GetValidationErrors().Count.ShouldBe(0);
        }

        [Fact]
        public void Describe_ShouldReturnAllStringValidationDescriptors()
        {
            // Arrange
            var rule = new ShoppingCartLineStringsValidationRule();

            // Act
            var descriptors = rule.Describe();

            // Assert
            descriptors.Count.ShouldBe(4);
            descriptors.ShouldContain(d => d.Message == "Name cannot be null or whitespace.");
            descriptors.ShouldContain(d => d.Message == "Brand cannot be null or whitespace.");
            descriptors.ShouldContain(d => d.Message == "Unit price currency cannot be null or whitespace.");
            descriptors.ShouldContain(d => d.Message == "Unit price currency must be USD.");
            descriptors.All(d => d.Name == "ShoppingCartLineStringsValidationRule").ShouldBeTrue();
            descriptors.All(d => d.Entity == "ShoppingCartLine").ShouldBeTrue();
        }
    }
}
