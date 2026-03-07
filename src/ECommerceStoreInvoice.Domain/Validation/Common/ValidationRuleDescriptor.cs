namespace ECommerceStoreInvoice.Domain.Validation.Common
{
    public sealed record ValidationRuleDescriptor
    {
        public required string RuleName { get; init; }
        public required List<ValidationError> Rules { get; init; }
    }
}
