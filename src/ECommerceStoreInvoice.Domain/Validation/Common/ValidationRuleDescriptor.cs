namespace ProductCatalogECommerceStoreInvoice.Domain.Validation.Common
{
    public sealed class ValidationRuleDescriptor
    {
        public required string RuleName { get; init; }
        public required List<ValidationError> Rules { get; init; }
    }
}
