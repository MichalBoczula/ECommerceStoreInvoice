using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ProductVersions;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Policies
{
    internal sealed class ProductVersionValidationPolicy : IValidationPolicy<ProductVersion>, IValidationPolicyDescriptorProvider
    {
        private readonly List<IValidationRule<ProductVersion>> _rules = [];

        public ProductVersionValidationPolicy()
        {
            _rules.Add(new ProductVersionIsNullValidationRule());
            _rules.Add(new ProductVersionProductIdValidationRule());
            _rules.Add(new ProductVersionStringsValidationRule());
            _rules.Add(new ProductVersionPriceValidationRule());
        }

        public async Task<ValidationResult> Validate(ProductVersion entity)
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
                PolicyName = nameof(ProductVersionValidationPolicy),
                Rules = allErrors
            };
        }
    }
}
