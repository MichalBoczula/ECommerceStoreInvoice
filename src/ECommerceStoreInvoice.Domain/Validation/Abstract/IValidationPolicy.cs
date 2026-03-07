using ProductCatalogECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Abstract
{
    public interface IValidationPolicy<T>
    {
        Task<ValidationResult> Validate(T entity);
        ValidationPolicyDescriptor Describe();
    }
}
