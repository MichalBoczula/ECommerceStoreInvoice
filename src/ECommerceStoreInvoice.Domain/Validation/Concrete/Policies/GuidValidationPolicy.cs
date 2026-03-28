using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ShoppingCarts;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Policies
{
    internal sealed class GuidValidationPolicy : IValidationPolicy<Guid>, IValidationPolicyDescriptorProvider
    {
        private readonly List<IValidationRule<Guid>> _rules = [];

        public GuidValidationPolicy()
        {
            _rules.Add(new GuidIsEmptyValidationRule());
        }

        public async Task<ValidationResult> Validate(Guid entity)
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
                PolicyName = nameof(GuidValidationPolicy),
                Rules = allErrors
            };
        }
    }
}