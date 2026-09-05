using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.Common
{
    internal sealed class GuidCollectionContainsEmptyGuidValidationRule : IValidationRule<IEnumerable<Guid>>
    {
        private readonly ValidationError guidCollectionCannotContainEmptyGuid = new()
        {
            Message = "The list of IDs cannot contain an empty GUID.",
            Name = nameof(GuidCollectionContainsEmptyGuidValidationRule),
            Entity = "GuidCollection"
        };

        public Task IsValid(IEnumerable<Guid> entity, ValidationResult validationResults)
        {
            if (entity?.Any(id => id == Guid.Empty) == true)
                validationResults.AddValidationError(guidCollectionCannotContainEmptyGuid);

            return Task.CompletedTask;
        }

        public List<ValidationError> Describe()
        {
            return [guidCollectionCannotContainEmptyGuid];
        }
    }
}
