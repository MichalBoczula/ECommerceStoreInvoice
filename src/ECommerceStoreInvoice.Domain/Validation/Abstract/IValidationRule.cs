using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Abstract
{
    public interface IValidationRule<T>
    {
        Task IsValid(T entity, ValidationResult validationResults);

        List<ValidationError> Describe();
    }
}
