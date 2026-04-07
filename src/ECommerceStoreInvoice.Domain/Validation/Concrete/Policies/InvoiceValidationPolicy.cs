using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.Orders;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Policies
{
    internal sealed class InvoiceValidationPolicy
        : IValidationPolicy<InvoiceOrderStatusValidationContext>, IValidationPolicyDescriptorProvider
    {
        private readonly List<IValidationRule<InvoiceOrderStatusValidationContext>> _rules = [];

        public InvoiceValidationPolicy()
        {
            _rules.Add(new InvoiceOrderStatusValidationRule());
        }

        public async Task<ValidationResult> Validate(InvoiceOrderStatusValidationContext entity)
        {
            ValidationResult validationResult = new();

            foreach (var rule in _rules)
                await rule.IsValid(entity, validationResult);

            return validationResult;
        }

        public ValidationPolicyDescriptor Describe()
        {
            return new ValidationPolicyDescriptor
            {
                PolicyName = nameof(InvoiceValidationPolicy),
                Rules = _rules.Select(rule => new ValidationRuleDescriptor
                {
                    RuleName = rule.GetType().Name,
                    Rules = rule.Describe()
                }).ToList()
            };
        }
    }
}
