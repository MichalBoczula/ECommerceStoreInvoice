namespace ECommerceStoreInvoice.Application.Common.ResponsesDto
{
    public sealed class InvoiceResponseDto
    {
        public Guid Id { get; init; }
        public Guid OrderId { get; init; }
        public string StorageUrl { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }
}
