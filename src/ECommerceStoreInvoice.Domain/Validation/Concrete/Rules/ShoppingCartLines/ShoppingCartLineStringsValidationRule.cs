using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ShoppingCarts
{
    internal sealed class ShoppingCartLineStringsValidationRule : IValidationRule<ShoppingCartLine>
    {
        private readonly ValidationError nameCannotBeNullOrWhitespace;
        private readonly ValidationError brandCannotBeNullOrWhitespace;
        private readonly ValidationError currencyCannotBeNullOrWhitespace;
        private readonly ValidationError currencyCurrencyMustBeUSD;

        public ShoppingCartLineStringsValidationRule()
        {
            nameCannotBeNullOrWhitespace = new ValidationError
            {
                Message = "Name cannot be null or whitespace.",
                Name = nameof(ShoppingCartLineStringsValidationRule),
                Entity = nameof(ShoppingCartLine)
            };

            brandCannotBeNullOrWhitespace = new ValidationError
            {
                Message = "Brand cannot be null or whitespace.",
                Name = nameof(ShoppingCartLineStringsValidationRule),
                Entity = nameof(ShoppingCartLine)
            };

            currencyCannotBeNullOrWhitespace = new ValidationError
            {
                Message = "Unit price currency cannot be null or whitespace.",
                Name = nameof(ShoppingCartLineStringsValidationRule),
                Entity = nameof(ShoppingCartLine)
            };

            currencyCurrencyMustBeUSD = new ValidationError
            {
                Message = "Unit price currency must be USD.",
                Name = nameof(ShoppingCartLineStringsValidationRule),
                Entity = nameof(ShoppingCartLine)
            };
        }

        public async Task IsValid(ShoppingCartLine entity, ValidationResult validationResults)
        {
            if (entity is null) return;

            if (string.IsNullOrWhiteSpace(entity.Name))
                validationResults.AddValidationError(nameCannotBeNullOrWhitespace);

            if (string.IsNullOrWhiteSpace(entity.Brand))
                validationResults.AddValidationError(brandCannotBeNullOrWhitespace);

            if (string.IsNullOrWhiteSpace(entity.UnitPrice.Currency))
                validationResults.AddValidationError(currencyCannotBeNullOrWhitespace);

            if (entity.UnitPrice.Currency.ToLower() != "usd")
                validationResults.AddValidationError(currencyCurrencyMustBeUSD);
        }

        public List<ValidationError> Describe()
        {
            return [nameCannotBeNullOrWhitespace, brandCannotBeNullOrWhitespace, currencyCannotBeNullOrWhitespace, currencyCurrencyMustBeUSD];
        }
    }
}