using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ProductVersions
{
    internal sealed class ProductVersionStringsValidationRule : IValidationRule<ProductVersion>
    {
        private readonly ValidationError nameCannotBeNullOrWhitespace;
        private readonly ValidationError brandCannotBeNullOrWhitespace;
        private readonly ValidationError currencyCannotBeNullOrWhitespace;
        private readonly ValidationError currencyMustBeSupported;

        public ProductVersionStringsValidationRule()
        {
            nameCannotBeNullOrWhitespace = new ValidationError
            {
                Message = "Name cannot be null or whitespace.",
                Name = nameof(ProductVersionStringsValidationRule),
                Entity = nameof(ProductVersion)
            };

            brandCannotBeNullOrWhitespace = new ValidationError
            {
                Message = "Brand cannot be null or whitespace.",
                Name = nameof(ProductVersionStringsValidationRule),
                Entity = nameof(ProductVersion)
            };

            currencyCannotBeNullOrWhitespace = new ValidationError
            {
                Message = "Price currency cannot be null or whitespace.",
                Name = nameof(ProductVersionStringsValidationRule),
                Entity = nameof(ProductVersion)
            };

            currencyMustBeSupported = new ValidationError
            {
                Message = "Price currency must be USD or PLN.",
                Name = nameof(ProductVersionStringsValidationRule),
                Entity = nameof(ProductVersion)
            };
        }

        public async Task IsValid(ProductVersion entity, ValidationResult validationResults)
        {
            if (entity is null)
                return;

            if (string.IsNullOrWhiteSpace(entity.Name))
                validationResults.AddValidationError(nameCannotBeNullOrWhitespace);

            if (string.IsNullOrWhiteSpace(entity.Brand))
                validationResults.AddValidationError(brandCannotBeNullOrWhitespace);

            if (string.IsNullOrWhiteSpace(entity.Price.Currency))
                validationResults.AddValidationError(currencyCannotBeNullOrWhitespace);

            if (!string.Equals(entity.Price.Currency, "USD", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(entity.Price.Currency, "PLN", StringComparison.OrdinalIgnoreCase))
                validationResults.AddValidationError(currencyMustBeSupported);
        }

        public List<ValidationError> Describe()
        {
            return [nameCannotBeNullOrWhitespace, brandCannotBeNullOrWhitespace, currencyCannotBeNullOrWhitespace, currencyMustBeSupported];
        }
    }
}
