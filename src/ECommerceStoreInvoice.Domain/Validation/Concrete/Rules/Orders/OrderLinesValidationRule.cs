using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.Orders
{
    internal sealed class OrderLinesValidationRule : IValidationRule<Order>
    {
        private readonly ValidationError linesCannotBeNull;
        private readonly ValidationError linesCannotBeEmpty;

        public OrderLinesValidationRule()
        {
            linesCannotBeNull = new ValidationError
            {
                Message = "Order lines cannot be null.",
                Name = nameof(OrderLinesValidationRule),
                Entity = nameof(Order)
            };

            linesCannotBeEmpty = new ValidationError
            {
                Message = "Order lines cannot be empty.",
                Name = nameof(OrderLinesValidationRule),
                Entity = nameof(Order)
            };
        }

        public async Task IsValid(Order entity, ValidationResult validationResults)
        {
            if (entity is null) return;

            if (entity.Lines is null)
            {
                validationResults.AddValidationError(linesCannotBeNull);
                return;
            }

            if (!entity.Lines.Any())
                validationResults.AddValidationError(linesCannotBeEmpty);
        }

        public List<ValidationError> Describe()
        {
            return [linesCannotBeNull, linesCannotBeEmpty];
        }
    }
}
