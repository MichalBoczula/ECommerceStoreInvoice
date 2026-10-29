namespace ECommerceStoreInvoice.Domain.Validation.Common
{
    /// <summary>Source-authored validation rule and its possible errors.</summary>
    public sealed record ValidationRuleDescriptor
    {
        /// <summary>Source validation-rule name.</summary>
        public required string RuleName { get; init; }
        /// <summary>Possible structured errors declared by this rule, rather than an execution trace.</summary>
        public required List<ValidationError> Rules { get; init; }
    }
}
