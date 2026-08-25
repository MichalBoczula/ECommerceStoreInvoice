using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Policies;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ShoppingCarts;
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
                new(Guid.Empty, 0)
            };

            // Act
            var result = await policy.Validate(lines);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.GetValidationErrors().Count.ShouldBe(2);
            result.GetValidationErrors().ShouldContain(e => e.Name == nameof(ShoppingCartLineGuidValidationRule) || e.Message.Contains("Guid") || e.Message.Contains("ProductId"));
            result.GetValidationErrors().ShouldContain(e => e.Name == nameof(ShoppingCartLineQuantityValidationRule) || e.Message == "Quantity must be greater than zero.");
        }

        [Fact]
        public async Task Validate_LinesAreValid_ShouldReturnNoErrors()
        {
            // Arrange
            var policy = new ShoppingCartLineValidationPolicy();
            var lines = new List<ShoppingCartLine>
            {
                new(Guid.NewGuid(), 1),
                new(Guid.NewGuid(), 2)
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
            descriptor.PolicyName.ShouldBe(nameof(ShoppingCartLineValidationPolicy));
            descriptor.Rules.Count.ShouldBe(3);
            descriptor.Rules.ShouldContain(r => r.RuleName == nameof(ShoppingCartLineIsNullValidationRule));
            descriptor.Rules.ShouldContain(r => r.RuleName == nameof(ShoppingCartLineGuidValidationRule));
            descriptor.Rules.ShouldContain(r => r.RuleName == nameof(ShoppingCartLineQuantityValidationRule));
        }
    }
}