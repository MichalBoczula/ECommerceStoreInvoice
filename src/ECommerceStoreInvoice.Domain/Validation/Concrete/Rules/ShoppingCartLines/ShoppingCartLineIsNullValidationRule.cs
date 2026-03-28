using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ShoppingCarts
{
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