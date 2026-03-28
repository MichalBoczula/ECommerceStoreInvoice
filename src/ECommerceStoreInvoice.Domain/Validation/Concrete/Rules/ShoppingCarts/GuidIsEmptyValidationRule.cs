using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ShoppingCarts
{
    internal class GuidIsEmptyValidationRule : IValidationRule<Guid>
    {
        private readonly ValidationError emptyClientId;

        public GuidIsEmptyValidationRule()
        {
            emptyClientId = new ValidationError
            {
                Message = "ClientId cannot be empty Guid.",
                Name = nameof(GuidIsEmptyValidationRule),
                Entity = nameof(ShoppingCart)
            };
        }

        public async Task IsValid(Guid entity, ValidationResult validationResults)
        {
            if (entity == Guid.Empty)
            {
                validationResults.AddValidationError(emptyClientId);
            }
        }

        public List<ValidationError> Describe()
        {
            return [emptyClientId];
        }
    }
}
