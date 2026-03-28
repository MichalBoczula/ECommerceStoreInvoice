using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ShoppingCarts;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Policies
{
    internal sealed class ShoppingCartLineValidationPolicy
        : IValidationPolicy<IReadOnlyCollection<ShoppingCartLine>>, IValidationPolicyDescriptorProvider
    {
        private readonly List<IValidationRule<ShoppingCartLine>> _rules = [];

        public ShoppingCartLineValidationPolicy()
        {
            _rules.Add(new ShoppingCartLineIsNullValidationRule());
            _rules.Add(new ShoppingCartLineStringsValidationRule());
            _rules.Add(new ShoppingCartLineQuantityValidationRule());
            _rules.Add(new ShoppingCartLineMoneyValidationRule());
        }

        public async Task<ValidationResult> Validate(IReadOnlyCollection<ShoppingCartLine> entity)
        {
            ValidationResult validationResult = new();

            foreach (var rule in _rules)
                foreach (var line in entity)
                    await rule.IsValid(line, validationResult);

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
                PolicyName = nameof(ShoppingCartLineValidationPolicy),
                Rules = allErrors
            };
        }
    }
}
