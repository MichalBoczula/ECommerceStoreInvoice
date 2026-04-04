using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.Orders;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Policies
{
    internal sealed class UpdateOrderValidationPolicy
        : IValidationPolicy<(Order order, OrderStatus newStatus)>, IValidationPolicyDescriptorProvider
    {
        private readonly List<IValidationRule<(Order order, OrderStatus newStatus)>> _rules = [];

        public UpdateOrderValidationPolicy()
        {
            _rules.Add(new UpdateOrderStatusTransitionValidationRule());
        }

        public async Task<ValidationResult> Validate((Order order, OrderStatus newStatus) entity)
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
                PolicyName = nameof(UpdateOrderValidationPolicy),
                Rules = _rules.Select(rule => new ValidationRuleDescriptor
                {
                    RuleName = rule.GetType().Name,
                    Rules = rule.Describe()
                }).ToList()
            };
        }
    }
}
