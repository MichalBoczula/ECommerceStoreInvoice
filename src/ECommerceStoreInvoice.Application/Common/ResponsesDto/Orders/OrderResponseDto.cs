namespace ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders
{
    public sealed record OrderResponseDto
    {
        public required Guid Id { get; init; }
        public required Guid ClientId { get; init; }
        public required DateTime CreatedAt { get; init; }
        public required DateTime UpdatedAt { get; init; }
        public required string Status { get; init; }
        public required decimal TotalAmount { get; init; }
        public required string TotalCurrency { get; init; }
        public required IReadOnlyCollection<OrderLineResponseDto> Lines { get; init; }
    }
}
