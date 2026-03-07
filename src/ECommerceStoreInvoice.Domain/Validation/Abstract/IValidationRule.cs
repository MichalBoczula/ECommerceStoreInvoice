using ProductCatalogECommerceStoreInvoice.Domain.Validation.Common;

namespace ProductCatalogECommerceStoreInvoice.Domain.Validation.Abstract
{
    public interface IValidationRule<T>
    {
        Task IsValid(T entity, ValidationResult validationResults);

        List<ValidationError> Describe();
    }
}
