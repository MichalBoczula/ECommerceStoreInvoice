using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.Common;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Rules.Common
{
    public class GuidCollectionValidationRuleTests
    {
        [Fact]
        public async Task IsValid_CollectionIsEmpty_ShouldReturnValidationError()
        {
            var rule = new GuidCollectionIsEmptyValidationRule();
            var validationResult = new ValidationResult();

            await rule.IsValid([], validationResult);

            var error = validationResult.GetValidationErrors().ShouldHaveSingleItem();
            error.Message.ShouldBe("The list of IDs cannot be empty.");
        }

        [Fact]
        public async Task IsValid_CollectionContainsEmptyGuid_ShouldReturnValidationError()
        {
            var rule = new GuidCollectionContainsEmptyGuidValidationRule();
            var validationResult = new ValidationResult();

            await rule.IsValid([Guid.NewGuid(), Guid.Empty], validationResult);

            var error = validationResult.GetValidationErrors().ShouldHaveSingleItem();
            error.Message.ShouldBe("The list of IDs cannot contain an empty GUID.");
        }

        [Fact]
        public void Describe_EmptyCollectionRule_ShouldReturnExpectedDescriptor()
        {
            var descriptor = new GuidCollectionIsEmptyValidationRule().Describe().ShouldHaveSingleItem();

            descriptor.Name.ShouldBe(nameof(GuidCollectionIsEmptyValidationRule));
            descriptor.Entity.ShouldBe("GuidCollection");
            descriptor.Message.ShouldBe("The list of IDs cannot be empty.");
        }

        [Fact]
        public void Describe_EmptyGuidRule_ShouldReturnExpectedDescriptor()
        {
            var descriptor = new GuidCollectionContainsEmptyGuidValidationRule().Describe().ShouldHaveSingleItem();

            descriptor.Name.ShouldBe(nameof(GuidCollectionContainsEmptyGuidValidationRule));
            descriptor.Entity.ShouldBe("GuidCollection");
            descriptor.Message.ShouldBe("The list of IDs cannot contain an empty GUID.");
        }
    }
}
