using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ClientDataVersions;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Policies
{
    internal sealed class ClientDataVersionValidationPolicy : IValidationPolicy<ClientDataVersion>, IValidationPolicyDescriptorProvider
    {
        private readonly List<IValidationRule<ClientDataVersion>> _rules = [];

        public ClientDataVersionValidationPolicy()
        {
            _rules.Add(new ClientDataVersionPhoneValidationRule());
            _rules.Add(new ClientDataVersionEmailValidationRule());
            _rules.Add(new ClientDataVersionAddressValidationRule());
        }

        public async Task<ValidationResult> Validate(ClientDataVersion entity)
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
                PolicyName = nameof(ClientDataVersionValidationPolicy),
                Rules = allErrors
            };
        }
    }
}
