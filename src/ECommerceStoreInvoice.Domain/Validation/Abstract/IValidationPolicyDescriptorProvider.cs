using ProductCatalogECommerceStoreInvoice.Domain.Validation.Common;

namespace ProductCatalogECommerceStoreInvoice.Domain.Validation.Abstract
{
    public interface IValidationPolicyDescriptorProvider
    {
        ValidationPolicyDescriptor Describe();
    }
}
