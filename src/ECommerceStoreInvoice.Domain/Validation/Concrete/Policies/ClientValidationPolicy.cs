using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Policies
{
    [Documentation("inv.policy.client", "Validate a nonempty GUID. The installed error entity is ShoppingCart even when this shared GUID policy is used by another service.")]
    internal sealed class ClientValidationPolicy : IValidationPolicy<Guid>, IValidationPolicyDescriptorProvider
    {
        private readonly List<IValidationRule<Guid>> _rules = [];

        public ClientValidationPolicy()
        {
            _rules.Add(new ClientIdIsEmptyValidationRule(nameof(ShoppingCart)));
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
                PolicyName = nameof(ClientValidationPolicy),
                Rules = allErrors
            };
        }
    }
}
