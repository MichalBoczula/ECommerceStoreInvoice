using System.Text.RegularExpressions;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ClientDataVersions
{
    internal sealed class ClientDataVersionAddressValidationRule : IValidationRule<ClientDataVersion>
    {
        private static readonly Regex PostalCodePattern = new(
            "^\\d{2}-\\d{3}$",
            RegexOptions.Compiled);

        private static readonly Regex AddressComponentPattern = new(
            "^[A-Za-z0-9.,]+$",
            RegexOptions.Compiled);

        private readonly ValidationError postalCodeCannotBeNullOrWhitespace;
        private readonly ValidationError postalCodeHasInvalidFormat;
        private readonly ValidationError cityHasInvalidFormat;
        private readonly ValidationError streetHasInvalidFormat;
        private readonly ValidationError buildingNumberHasInvalidFormat;
        private readonly ValidationError apartmentNumberHasInvalidFormat;

        public ClientDataVersionAddressValidationRule()
        {
            postalCodeCannotBeNullOrWhitespace = new ValidationError
            {
                Message = "Postal code cannot be null or whitespace.",
                Name = nameof(ClientDataVersionAddressValidationRule),
                Entity = nameof(ClientDataVersion)
            };

            postalCodeHasInvalidFormat = new ValidationError
            {
                Message = "Postal code must match pattern nn-nnn (for example: 00-001).",
                Name = nameof(ClientDataVersionAddressValidationRule),
                Entity = nameof(ClientDataVersion)
            };

            cityHasInvalidFormat = new ValidationError
            {
                Message = "Address city may contain only letters, digits, '.' and ','.",
                Name = nameof(ClientDataVersionAddressValidationRule),
                Entity = nameof(ClientDataVersion)
            };

            streetHasInvalidFormat = new ValidationError
            {
                Message = "Address street may contain only letters, digits, '.' and ','.",
                Name = nameof(ClientDataVersionAddressValidationRule),
                Entity = nameof(ClientDataVersion)
            };

            buildingNumberHasInvalidFormat = new ValidationError
            {
                Message = "Address building number may contain only letters, digits, '.' and ','.",
                Name = nameof(ClientDataVersionAddressValidationRule),
                Entity = nameof(ClientDataVersion)
            };

            apartmentNumberHasInvalidFormat = new ValidationError
            {
                Message = "Address apartment number may contain only letters, digits, '.' and ','.",
                Name = nameof(ClientDataVersionAddressValidationRule),
                Entity = nameof(ClientDataVersion)
            };
        }

        public async Task IsValid(ClientDataVersion entity, ValidationResult validationResults)
        {
            if (entity is null)
                return;

            if (string.IsNullOrWhiteSpace(entity.Address.PostalCode))
            {
                validationResults.AddValidationError(postalCodeCannotBeNullOrWhitespace);
                return;
            }

            if (!PostalCodePattern.IsMatch(entity.Address.PostalCode))
                validationResults.AddValidationError(postalCodeHasInvalidFormat);

            ValidateOptionalAddressComponent(entity.Address.City, cityHasInvalidFormat, validationResults);
            ValidateOptionalAddressComponent(entity.Address.Street, streetHasInvalidFormat, validationResults);
            ValidateOptionalAddressComponent(entity.Address.BuildingNumber, buildingNumberHasInvalidFormat, validationResults);
            ValidateOptionalAddressComponent(entity.Address.ApartmentNumber, apartmentNumberHasInvalidFormat, validationResults);
        }

        public List<ValidationError> Describe()
        {
            return
            [
                postalCodeCannotBeNullOrWhitespace,
                postalCodeHasInvalidFormat,
                cityHasInvalidFormat,
                streetHasInvalidFormat,
                buildingNumberHasInvalidFormat,
                apartmentNumberHasInvalidFormat
            ];
        }

        private static void ValidateOptionalAddressComponent(string value, ValidationError validationError, ValidationResult validationResults)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            if (!AddressComponentPattern.IsMatch(value))
                validationResults.AddValidationError(validationError);
        }
    }
}
