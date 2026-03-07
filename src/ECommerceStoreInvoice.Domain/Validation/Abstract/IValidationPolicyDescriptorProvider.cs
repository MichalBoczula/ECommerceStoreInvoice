using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Abstract
{
    public interface IValidationPolicyDescriptorProvider
    {
        ValidationPolicyDescriptor Describe();
    }
}
