using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.Orders
{
    internal sealed class OrderIsNullValidationRule : IValidationRule<Order>
    {
        private readonly ValidationError orderIsNull;

        public OrderIsNullValidationRule()
        {
            orderIsNull = new ValidationError
            {
                Message = "Order cannot be null.",
                Name = nameof(OrderIsNullValidationRule),
                Entity = nameof(Order)
            };
        }

        public async Task IsValid(Order entity, ValidationResult validationResults)
        {
            if (entity is null)
                validationResults.AddValidationError(orderIsNull);
        }

        public List<ValidationError> Describe()
        {
            return [orderIsNull];
        }
    }
}
