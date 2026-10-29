namespace ECommerceStoreInvoice.Domain.Validation.Common
{
    /// <summary>One complete safe business validation error.</summary>
    public sealed record ValidationError
    {
        /// <summary>Safe human-readable validation message.</summary>
        public required string Message { get; init; }
        /// <summary>Source validation-rule name.</summary>
        public required string Name { get; init; }
        /// <summary>Business entity or value being validated.</summary>
        public required string Entity { get; init; }
    }
}
