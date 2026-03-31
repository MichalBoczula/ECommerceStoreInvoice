using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.Common
{
    internal sealed class ClientIdIsEmptyValidationRule : IValidationRule<Guid>
    {
        private readonly ValidationError clientIdIsEmpty;

        public ClientIdIsEmptyValidationRule(string entityName)
        {
            clientIdIsEmpty = new ValidationError
            {
                Message = "ClientId cannot be empty Guid.",
                Name = nameof(ClientIdIsEmptyValidationRule),
                Entity = entityName
            };
        }

        public async Task IsValid(Guid entity, ValidationResult validationResults)
        {
            if (entity == Guid.Empty)
                validationResults.AddValidationError(clientIdIsEmpty);
        }

        public List<ValidationError> Describe()
        {
            return [clientIdIsEmpty];
        }
    }
}
