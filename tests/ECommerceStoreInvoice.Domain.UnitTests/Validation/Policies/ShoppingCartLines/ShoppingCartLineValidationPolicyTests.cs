using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Policies;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Policies.ShoppingCartLines
{
    public class ShoppingCartLineValidationPolicyTests
    {
        [Fact]
        public async Task Validate_LinesContainInvalidData_ShouldReturnErrorsFromMultipleRules()
        {
            // Arrange
            var policy = new ShoppingCartLineValidationPolicy();
            var lines = new List<ShoppingCartLine>
            {
                new(Guid.NewGuid(), "", "", new Money(-10, "EUR"), 0)
            };

            // Act
            var result = await policy.Validate(lines);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.GetValidationErrors().Count.ShouldBe(5);
            result.GetValidationErrors().ShouldContain(e => e.Name == "ShoppingCartLineStringsValidationRule" && e.Message == "Name cannot be null or whitespace.");
            result.GetValidationErrors().ShouldContain(e => e.Name == "ShoppingCartLineStringsValidationRule" && e.Message == "Brand cannot be null or whitespace.");
            result.GetValidationErrors().ShouldContain(e => e.Name == "ShoppingCartLineStringsValidationRule" && e.Message == "Unit price currency must be USD.");
            result.GetValidationErrors().ShouldContain(e => e.Name == "ShoppingCartLineQuantityValidationRule");
            result.GetValidationErrors().ShouldContain(e => e.Name == "ShoppingCartLineMoneyValidationRule");
        }

        [Fact]
        public async Task Validate_LinesAreValid_ShouldReturnNoErrors()
        {
            // Arrange
            var policy = new ShoppingCartLineValidationPolicy();
            var lines = new List<ShoppingCartLine>
            {
                new(Guid.NewGuid(), "Keyboard", "Logi", new Money(99, "USD"), 1),
                new(Guid.NewGuid(), "Mouse", "Logi", new Money(49, "USD"), 2)
            };

            // Act
            var result = await policy.Validate(lines);

            // Assert
            result.IsValid.ShouldBeTrue();
            result.GetValidationErrors().Count.ShouldBe(0);
        }

        [Fact]
        public void Describe_ShouldIncludeAllUnderlyingRuleDescriptors()
        {
            // Arrange
            var policy = new ShoppingCartLineValidationPolicy();

            // Act
            var descriptor = policy.Describe();

            // Assert
            descriptor.PolicyName.ShouldBe("ShoppingCartLineValidationPolicy");
            descriptor.Rules.Count.ShouldBe(4);
            descriptor.Rules.ShouldContain(r => r.RuleName == "ShoppingCartLineIsNullValidationRule");
            descriptor.Rules.ShouldContain(r => r.RuleName == "ShoppingCartLineStringsValidationRule");
            descriptor.Rules.ShouldContain(r => r.RuleName == "ShoppingCartLineQuantityValidationRule");
            descriptor.Rules.ShouldContain(r => r.RuleName == "ShoppingCartLineMoneyValidationRule");
        }
    }
}
