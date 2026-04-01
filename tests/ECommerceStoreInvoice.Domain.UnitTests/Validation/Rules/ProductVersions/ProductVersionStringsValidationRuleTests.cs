using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ProductVersions;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Rules.ProductVersions
{
    public class ProductVersionStringsValidationRuleTests
    {
        [Fact]
        public async Task IsValid_NameBrandAndCurrencyAreInvalid_ShouldReturnAllMatchingErrors()
        {
            // Arrange
            var rule = new ProductVersionStringsValidationRule();
            var validationResult = new ValidationResult();
            var productVersion = new ProductVersion(Guid.NewGuid(), new Money(20, " "), " ", " ");

            // Act
            await rule.IsValid(productVersion, validationResult);

            // Assert
            validationResult.GetValidationErrors().Count.ShouldBe(4);
            validationResult.GetValidationErrors().ShouldContain(e => e.Message == "Name cannot be null or whitespace.");
            validationResult.GetValidationErrors().ShouldContain(e => e.Message == "Brand cannot be null or whitespace.");
            validationResult.GetValidationErrors().ShouldContain(e => e.Message == "Price currency cannot be null or whitespace.");
            validationResult.GetValidationErrors().ShouldContain(e => e.Message == "Price currency must be USD.");
        }

        [Fact]
        public void Describe_ShouldReturnExpectedDescriptors()
        {
            // Arrange
            var rule = new ProductVersionStringsValidationRule();

            // Act
            var descriptors = rule.Describe();

            // Assert
            descriptors.Count.ShouldBe(4);
            descriptors.ShouldContain(d => d.Message == "Name cannot be null or whitespace.");
            descriptors.ShouldContain(d => d.Message == "Brand cannot be null or whitespace.");
            descriptors.ShouldContain(d => d.Message == "Price currency cannot be null or whitespace.");
            descriptors.ShouldContain(d => d.Message == "Price currency must be USD.");
        }
    }
}
