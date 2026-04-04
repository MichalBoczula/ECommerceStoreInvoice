namespace ECommerceStoreInvoice.Application.Common.ResponsesDto.Invoices
{
    public sealed record InvoiceLineDto
    {
        public required string ProductVersionId { get; init; }
        public required string Name { get; init; }
        public required string Brand { get; init; }
        public required int Quantity { get; init; }
        public required decimal UnitAmount { get; init; }
        public required decimal TotalAmount { get; init; }
        public required string Currency { get; init; }
    }
}
