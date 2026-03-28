using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ShoppingCarts
{
    internal sealed class ShoppingCartLineMoneyValidationRule : IValidationRule<ShoppingCartLine>
    {
        private readonly ValidationError unitPriceAmountCannotBeNegative;

        public ShoppingCartLineMoneyValidationRule()
        {
            unitPriceAmountCannotBeNegative = new ValidationError
            {
                Message = "Unit price amount cannot be negative.",
                Name = nameof(ShoppingCartLineMoneyValidationRule),
                Entity = nameof(ShoppingCartLine)
            };
        }

        public async Task IsValid(ShoppingCartLine entity, ValidationResult validationResults)
        {
            if (entity is null) return;

            if (entity.UnitPrice.Amount < 0)
                validationResults.AddValidationError(unitPriceAmountCannotBeNegative);
        }

        public List<ValidationError> Describe()
        {
            return [unitPriceAmountCannotBeNegative];
        }
    }
}
