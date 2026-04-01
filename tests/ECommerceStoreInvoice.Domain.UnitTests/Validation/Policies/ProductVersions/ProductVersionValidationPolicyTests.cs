using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Policies;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Policies.ProductVersions
{
    public class ProductVersionValidationPolicyTests
    {
        [Fact]
        public async Task Validate_ProductVersionWithInvalidData_ShouldReturnErrors()
        {
            // Arrange
            var policy = new ProductVersionValidationPolicy();
            var productVersion = new ProductVersion(Guid.Empty, new Money(0, "EUR"), " ", " ");

            // Act
            var result = await policy.Validate(productVersion);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.GetValidationErrors().Count.ShouldBe(5);
            result.GetValidationErrors().ShouldContain(e => e.Name == "ProductVersionProductIdValidationRule");
            result.GetValidationErrors().ShouldContain(e => e.Name == "ProductVersionPriceValidationRule");
            result.GetValidationErrors().ShouldContain(e => e.Name == "ProductVersionStringsValidationRule" && e.Message == "Name cannot be null or whitespace.");
            result.GetValidationErrors().ShouldContain(e => e.Name == "ProductVersionStringsValidationRule" && e.Message == "Brand cannot be null or whitespace.");
            result.GetValidationErrors().ShouldContain(e => e.Name == "ProductVersionStringsValidationRule" && e.Message == "Price currency must be USD.");
        }

        [Fact]
        public async Task Validate_ProductVersionIsValid_ShouldReturnNoErrors()
        {
            // Arrange
            var policy = new ProductVersionValidationPolicy();
            var productVersion = new ProductVersion(Guid.NewGuid(), new Money(15.99m, "USD"), "Keyboard", "Logi");

            // Act
            var result = await policy.Validate(productVersion);

            // Assert
            result.IsValid.ShouldBeTrue();
            result.GetValidationErrors().Count.ShouldBe(0);
        }

        [Fact]
        public void Describe_ShouldIncludeAllUnderlyingRuleDescriptors()
        {
            // Arrange
            var policy = new ProductVersionValidationPolicy();

            // Act
            var descriptor = policy.Describe();

            // Assert
            descriptor.PolicyName.ShouldBe("ProductVersionValidationPolicy");
            descriptor.Rules.Count.ShouldBe(4);
            descriptor.Rules.ShouldContain(r => r.RuleName == "ProductVersionIsNullValidationRule");
            descriptor.Rules.ShouldContain(r => r.RuleName == "ProductVersionProductIdValidationRule");
            descriptor.Rules.ShouldContain(r => r.RuleName == "ProductVersionStringsValidationRule");
            descriptor.Rules.ShouldContain(r => r.RuleName == "ProductVersionPriceValidationRule");
        }
    }
}
