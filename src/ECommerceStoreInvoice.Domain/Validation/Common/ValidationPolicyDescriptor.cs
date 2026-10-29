namespace ECommerceStoreInvoice.Domain.Validation.Common
{
    /// <summary>Source-authored policy and its declared rule groups.</summary>
    public sealed record ValidationPolicyDescriptor
    {
        /// <summary>Registered policy name.</summary>
        public required string PolicyName { get; init; }
        /// <summary>Declared validation rule groups in source order.</summary>
        public required List<ValidationRuleDescriptor> Rules { get; init; }
    }
}
