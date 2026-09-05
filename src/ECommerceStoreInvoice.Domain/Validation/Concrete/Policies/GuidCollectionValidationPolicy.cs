using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Policies
{
    internal sealed class GuidCollectionValidationPolicy
        : IValidationPolicy<IEnumerable<Guid>>, IValidationPolicyDescriptorProvider
    {
        private readonly List<IValidationRule<IEnumerable<Guid>>> _rules =
        [
            new GuidCollectionIsEmptyValidationRule(),
            new GuidCollectionContainsEmptyGuidValidationRule()
        ];

        public async Task<ValidationResult> Validate(IEnumerable<Guid> entity)
        {
            ValidationResult validationResult = new();

            foreach (var rule in _rules)
                await rule.IsValid(entity, validationResult);

            return validationResult;
        }

        public ValidationPolicyDescriptor Describe()
        {
            var allErrors = _rules
                .Select(rule => new ValidationRuleDescriptor
                {
                    RuleName = rule.GetType().Name,
                    Rules = rule.Describe()
                })
                .ToList();

            return new ValidationPolicyDescriptor
            {
                PolicyName = nameof(GuidCollectionValidationPolicy),
                Rules = allErrors
            };
        }
    }
}
