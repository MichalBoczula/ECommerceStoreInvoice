namespace ECommerceStoreInvoice.Domain.Validation.Common
{
    public sealed record ValidationError
    {
        public required string Message { get; init; }
        public required string RuleName { get; init; }
        public required string Entity { get; init; }
    }
}
