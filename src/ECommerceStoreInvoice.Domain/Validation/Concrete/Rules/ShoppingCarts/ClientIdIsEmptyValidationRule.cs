using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ShoppingCarts
{
    internal sealed class ClientIdIsEmptyValidationRule : IValidationRule<Guid>
    {
        private readonly ValidationError clientIdCannotBeEmpty;

        public ClientIdIsEmptyValidationRule()
        {
            clientIdCannotBeEmpty = new ValidationError
            {
                Message = "ClientId cannot be empty Guid.",
                Name = nameof(ClientIdIsEmptyValidationRule),
                Entity = nameof(ShoppingCart)
            };
        }

        public async Task IsValid(Guid entity, ValidationResult validationResults)
        {
            if (entity == Guid.Empty)
                validationResults.AddValidationError(clientIdCannotBeEmpty);
        }

        public List<ValidationError> Describe()
        {
            return [clientIdCannotBeEmpty];
        }
    }
}
