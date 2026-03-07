namespace ECommerceStoreInvoice.Domain.Validation.Common
{
    public sealed class ValidationPolicyDescriptor
    {
        public required string PolicyName { get; init; }
        public required List<ValidationRuleDescriptor> Rules { get; init; }
    }
}
