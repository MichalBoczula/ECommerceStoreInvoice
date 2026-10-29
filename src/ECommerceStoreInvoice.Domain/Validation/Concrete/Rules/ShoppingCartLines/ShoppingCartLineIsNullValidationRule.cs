using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ShoppingCarts
{
    [Documentation("inv.rule.shopping-cart-line-is-null", "Reject a null domain cart line; HTTP binding rejects null line objects earlier, so that HTTP failure is not proof this rule executed.")]
    internal sealed class ShoppingCartLineIsNullValidationRule : IValidationRule<ShoppingCartLine>
    {
        private readonly ValidationError shoppingCartLineIsNull;

        public ShoppingCartLineIsNullValidationRule()
        {
            shoppingCartLineIsNull = new ValidationError
            {
                Message = "Shopping cart line cannot be null.",
                Name = nameof(ShoppingCartLineIsNullValidationRule),
                Entity = nameof(ShoppingCartLine)
            };
        }

        public async Task IsValid(ShoppingCartLine entity, ValidationResult validationResults)
        {
            if (entity is null)
                validationResults.AddValidationError(shoppingCartLineIsNull);

            return;
        }

        public List<ValidationError> Describe()
        {
            return [shoppingCartLineIsNull];
        }
    }
}