using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.Orders;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Policies
{
    internal sealed class OrderValidationPolicy : IValidationPolicy<Order>, IValidationPolicyDescriptorProvider
    {
        private readonly List<IValidationRule<Order>> _rules = [];
        private readonly IValidationRule<Guid> _clientIdRule;

        public OrderValidationPolicy()
        {
            _rules.Add(new OrderIsNullValidationRule());
            _rules.Add(new OrderLinesValidationRule());
            _clientIdRule = new ClientIdIsEmptyValidationRule(nameof(Order));
        }

        public async Task<ValidationResult> Validate(Order entity)
        {
            ValidationResult validationResult = new();

            foreach (var rule in _rules)
                await rule.IsValid(entity, validationResult);

            if (entity is not null)
                await _clientIdRule.IsValid(entity.ClientId, validationResult);

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

            allErrors.Add(new ValidationRuleDescriptor
            {
                RuleName = _clientIdRule.GetType().Name,
                Rules = _clientIdRule.Describe()
            });

            return new ValidationPolicyDescriptor
            {
                PolicyName = nameof(OrderValidationPolicy),
                Rules = allErrors
            };
        }
    }
}
