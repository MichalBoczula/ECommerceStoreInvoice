using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ProductVersions
{
    internal sealed class ProductVersionProductIdValidationRule : IValidationRule<ProductVersion>
    {
        private readonly ValidationError productIdCannotBeEmpty;

        public ProductVersionProductIdValidationRule()
        {
            productIdCannotBeEmpty = new ValidationError
            {
                Message = "ProductId cannot be empty.",
                Name = nameof(ProductVersionProductIdValidationRule),
                Entity = nameof(ProductVersion)
            };
        }

        public async Task IsValid(ProductVersion entity, ValidationResult validationResults)
        {
            if (entity is null)
                return;

            if (entity.ProductId == Guid.Empty)
                validationResults.AddValidationError(productIdCannotBeEmpty);
        }

        public List<ValidationError> Describe()
        {
            return [productIdCannotBeEmpty];
        }
    }
}
