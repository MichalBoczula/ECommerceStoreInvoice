using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ProductVersions
{
    internal sealed class ProductVersionPriceValidationRule : IValidationRule<ProductVersion>
    {
        private readonly ValidationError priceMustBeGreaterThanZero;

        public ProductVersionPriceValidationRule()
        {
            priceMustBeGreaterThanZero = new ValidationError
            {
                Message = "Price amount must be greater than zero.",
                Name = nameof(ProductVersionPriceValidationRule),
                Entity = nameof(ProductVersion)
            };
        }

        public async Task IsValid(ProductVersion entity, ValidationResult validationResults)
        {
            if (entity is null)
                return;

            if (entity.Price.Amount <= 0)
                validationResults.AddValidationError(priceMustBeGreaterThanZero);
        }

        public List<ValidationError> Describe()
        {
            return [priceMustBeGreaterThanZero];
        }
    }
}
