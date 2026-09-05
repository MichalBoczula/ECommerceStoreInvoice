using ECommerceStoreInvoice.Domain.Validation.Concrete.Policies;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Policies
{
    public class GuidCollectionValidationPolicyTests
    {
        [Fact]
        public async Task Validate_CollectionIsEmpty_ShouldReturnError()
        {
            var result = await new GuidCollectionValidationPolicy().Validate([]);

            result.IsValid.ShouldBeFalse();
            result.GetValidationErrors().ShouldContain(error =>
                error.Name == "GuidCollectionIsEmptyValidationRule");
        }

        [Fact]
        public async Task Validate_CollectionContainsEmptyGuid_ShouldReturnError()
        {
            var result = await new GuidCollectionValidationPolicy().Validate([Guid.NewGuid(), Guid.Empty]);

            result.IsValid.ShouldBeFalse();
            result.GetValidationErrors().ShouldContain(error =>
                error.Name == "GuidCollectionContainsEmptyGuidValidationRule");
        }

        [Fact]
        public async Task Validate_CollectionIsValid_ShouldReturnNoErrors()
        {
            var result = await new GuidCollectionValidationPolicy().Validate([Guid.NewGuid(), Guid.NewGuid()]);

            result.IsValid.ShouldBeTrue();
            result.GetValidationErrors().ShouldBeEmpty();
        }

        [Fact]
        public void Describe_ShouldIncludeAllUnderlyingRuleDescriptors()
        {
            var descriptor = new GuidCollectionValidationPolicy().Describe();

            descriptor.PolicyName.ShouldBe(nameof(GuidCollectionValidationPolicy));
            descriptor.Rules.Count.ShouldBe(2);
            descriptor.Rules.ShouldContain(rule => rule.RuleName == "GuidCollectionIsEmptyValidationRule");
            descriptor.Rules.ShouldContain(rule => rule.RuleName == "GuidCollectionContainsEmptyGuidValidationRule");
        }
    }
}
