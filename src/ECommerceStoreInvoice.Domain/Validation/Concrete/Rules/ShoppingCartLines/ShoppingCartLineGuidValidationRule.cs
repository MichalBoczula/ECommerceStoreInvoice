using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ShoppingCarts
{
    internal sealed class ShoppingCartLineGuidValidationRule : IValidationRule<ShoppingCartLine>
    {
        private readonly ValidationError shoppingCartLineIdIsEmpty;

        public ShoppingCartLineGuidValidationRule()
        {
            shoppingCartLineIdIsEmpty = new ValidationError
            {
                Message = "ShoppingCartLine Id cannot be empty Guid.",
                Name = nameof(ShoppingCartLineGuidValidationRule),
                Entity = nameof(ShoppingCartLine)
            };
        }

        public List<ValidationError> Describe()
        {
            return [shoppingCartLineIdIsEmpty];
        }

        public async Task IsValid(ShoppingCartLine entity, ValidationResult validationResults)
        {
            if (entity.ProductId == Guid.Empty)
            {
                validationResults.AddValidationError(shoppingCartLineIdIsEmpty);
            }
        }
    }
}