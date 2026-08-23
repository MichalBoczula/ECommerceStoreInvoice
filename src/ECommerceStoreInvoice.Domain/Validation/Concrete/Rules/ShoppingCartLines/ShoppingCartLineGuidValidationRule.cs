using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ShoppingCarts
{
    internal sealed class ShoppingCartLineGuidValidationRule : IValidationRule<ShoppingCartLine>
    {
        public List<ValidationError> Describe()
        {
            throw new NotImplementedException();
        }

        public Task IsValid(ShoppingCartLine entity, ValidationResult validationResults)
        {
            throw new NotImplementedException();
        }

    }
}