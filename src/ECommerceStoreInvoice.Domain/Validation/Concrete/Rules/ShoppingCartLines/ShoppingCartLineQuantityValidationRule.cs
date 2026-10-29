using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ShoppingCarts
{
    [Documentation("inv.rule.shopping-cart-line-quantity", "For a non-null line, reject quantity less than or equal to zero without changing the cart.")]
    internal sealed class ShoppingCartLineQuantityValidationRule : IValidationRule<ShoppingCartLine>
    {
        private readonly ValidationError quantityMustBeGreaterThanZero;

        public ShoppingCartLineQuantityValidationRule()
        {
            quantityMustBeGreaterThanZero = new ValidationError
            {
                Message = "Quantity must be greater than zero.",
                Name = nameof(ShoppingCartLineQuantityValidationRule),
                Entity = nameof(ShoppingCartLine)
            };
        }

        public async Task IsValid(ShoppingCartLine entity, ValidationResult validationResults)
        {
            if (entity is null) return;

            if (entity.Quantity <= 0)
                validationResults.AddValidationError(quantityMustBeGreaterThanZero);
        }

        public List<ValidationError> Describe()
        {
            return [quantityMustBeGreaterThanZero];
        }
    }
}
