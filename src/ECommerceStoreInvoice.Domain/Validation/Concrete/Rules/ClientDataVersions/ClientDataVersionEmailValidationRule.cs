using System.Text.RegularExpressions;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ClientDataVersions
{
    internal sealed class ClientDataVersionEmailValidationRule : IValidationRule<ClientDataVersion>
    {
        private static readonly Regex EmailPattern = new(
            "^[A-Za-z0-9.]+@[A-Za-z0-9]+(?:\\.[A-Za-z0-9]+)+$",
            RegexOptions.Compiled);

        private readonly ValidationError emailCannotBeNullOrWhitespace;
        private readonly ValidationError emailHasInvalidFormat;

        public ClientDataVersionEmailValidationRule()
        {
            emailCannotBeNullOrWhitespace = new ValidationError
            {
                Message = "Address email cannot be null or whitespace.",
                Name = nameof(ClientDataVersionEmailValidationRule),
                Entity = nameof(ClientDataVersion)
            };

            emailHasInvalidFormat = new ValidationError
            {
                Message = "Address email must use only letters, digits and '.', and include '@' with a domain (for example: user@gmail.com).",
                Name = nameof(ClientDataVersionEmailValidationRule),
                Entity = nameof(ClientDataVersion)
            };
        }

        public async Task IsValid(ClientDataVersion entity, ValidationResult validationResults)
        {
            if (entity is null)
                return;

            if (string.IsNullOrWhiteSpace(entity.AddressEmail))
            {
                validationResults.AddValidationError(emailCannotBeNullOrWhitespace);
                return;
            }

            if (!EmailPattern.IsMatch(entity.AddressEmail))
            {
                validationResults.AddValidationError(emailHasInvalidFormat);
            }
        }

        public List<ValidationError> Describe()
        {
            return [emailCannotBeNullOrWhitespace, emailHasInvalidFormat];
        }
    }
}
