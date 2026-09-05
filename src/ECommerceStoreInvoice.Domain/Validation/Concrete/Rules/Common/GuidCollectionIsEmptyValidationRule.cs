using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.Common
{
    internal sealed class GuidCollectionIsEmptyValidationRule : IValidationRule<IEnumerable<Guid>>
    {
        private readonly ValidationError guidCollectionCannotBeEmpty = new()
        {
            Message = "The list of IDs cannot be empty.",
            Name = nameof(GuidCollectionIsEmptyValidationRule),
            Entity = "GuidCollection"
        };

        public Task IsValid(IEnumerable<Guid> entity, ValidationResult validationResults)
        {
            if (entity is null || !entity.Any())
                validationResults.AddValidationError(guidCollectionCannotBeEmpty);

            return Task.CompletedTask;
        }

        public List<ValidationError> Describe()
        {
            return [guidCollectionCannotBeEmpty];
        }
    }
}
