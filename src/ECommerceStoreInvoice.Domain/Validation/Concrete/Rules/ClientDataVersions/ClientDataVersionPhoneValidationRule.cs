using System.Text.RegularExpressions;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ClientDataVersions
{
    internal sealed class ClientDataVersionPhoneValidationRule : IValidationRule<ClientDataVersion>
    {
        private static readonly Regex DigitsOnlyPattern = new("^[0-9]+$", RegexOptions.Compiled);

        private readonly ValidationError phoneNumberCannotBeNullOrWhitespace;
        private readonly ValidationError phoneNumberHasInvalidFormat;
        private readonly ValidationError phoneNumberCannotBeZeroOrNegative;
        private readonly ValidationError phonePrefixCannotBeNullOrWhitespace;
        private readonly ValidationError phonePrefixHasInvalidFormat;
        private readonly ValidationError phonePrefixCannotBeZeroOrNegative;

        public ClientDataVersionPhoneValidationRule()
        {
            phoneNumberCannotBeNullOrWhitespace = new ValidationError
            {
                Message = "Phone number cannot be null or whitespace.",
                Name = nameof(ClientDataVersionPhoneValidationRule),
                Entity = nameof(ClientDataVersion)
            };

            phoneNumberHasInvalidFormat = new ValidationError
            {
                Message = "Phone number must contain digits only.",
                Name = nameof(ClientDataVersionPhoneValidationRule),
                Entity = nameof(ClientDataVersion)
            };

            phoneNumberCannotBeZeroOrNegative = new ValidationError
            {
                Message = "Phone number must be greater than 0.",
                Name = nameof(ClientDataVersionPhoneValidationRule),
                Entity = nameof(ClientDataVersion)
            };

            phonePrefixCannotBeNullOrWhitespace = new ValidationError
            {
                Message = "Phone prefix cannot be null or whitespace.",
                Name = nameof(ClientDataVersionPhoneValidationRule),
                Entity = nameof(ClientDataVersion)
            };

            phonePrefixHasInvalidFormat = new ValidationError
            {
                Message = "Phone prefix must contain digits only.",
                Name = nameof(ClientDataVersionPhoneValidationRule),
                Entity = nameof(ClientDataVersion)
            };

            phonePrefixCannotBeZeroOrNegative = new ValidationError
            {
                Message = "Phone prefix must be greater than 0.",
                Name = nameof(ClientDataVersionPhoneValidationRule),
                Entity = nameof(ClientDataVersion)
            };
        }

        public async Task IsValid(ClientDataVersion entity, ValidationResult validationResults)
        {
            if (entity is null)
                return;

            ValidateNumber(entity.PhoneNumber, phoneNumberCannotBeNullOrWhitespace, phoneNumberHasInvalidFormat, phoneNumberCannotBeZeroOrNegative, validationResults);
            ValidateNumber(entity.PhonePrefix, phonePrefixCannotBeNullOrWhitespace, phonePrefixHasInvalidFormat, phonePrefixCannotBeZeroOrNegative, validationResults);
        }

        public List<ValidationError> Describe()
        {
            return
            [
                phoneNumberCannotBeNullOrWhitespace,
                phoneNumberHasInvalidFormat,
                phoneNumberCannotBeZeroOrNegative,
                phonePrefixCannotBeNullOrWhitespace,
                phonePrefixHasInvalidFormat,
                phonePrefixCannotBeZeroOrNegative
            ];
        }

        private static void ValidateNumber(
            string value,
            ValidationError cannotBeNullOrWhitespaceError,
            ValidationError hasInvalidFormatError,
            ValidationError cannotBeZeroOrNegativeError,
            ValidationResult validationResults)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                validationResults.AddValidationError(cannotBeNullOrWhitespaceError);
                return;
            }

            if (!DigitsOnlyPattern.IsMatch(value))
            {
                validationResults.AddValidationError(hasInvalidFormatError);
                return;
            }

            if (!long.TryParse(value, out var parsedValue) || parsedValue <= 0)
            {
                validationResults.AddValidationError(cannotBeZeroOrNegativeError);
            }
        }
    }
}
