using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ProductVersions
{
    internal sealed class ProductVersionIsNullValidationRule : IValidationRule<ProductVersion>
    {
        private readonly ValidationError productVersionIsNull;

        public ProductVersionIsNullValidationRule()
        {
            productVersionIsNull = new ValidationError
            {
                Message = "Product version cannot be null.",
                Name = nameof(ProductVersionIsNullValidationRule),
                Entity = nameof(ProductVersion)
            };
        }

        public async Task IsValid(ProductVersion entity, ValidationResult validationResults)
        {
            if (entity is null)
                validationResults.AddValidationError(productVersionIsNull);
        }

        public List<ValidationError> Describe()
        {
            return [productVersionIsNull];
        }
    }
}
