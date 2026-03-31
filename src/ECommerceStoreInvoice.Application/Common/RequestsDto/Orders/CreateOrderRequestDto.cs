namespace ECommerceStoreInvoice.Application.Common.RequestsDto.Orders
{
    public sealed record CreateOrderRequestDto
    {
        public required Guid ClientId { get; init; }
        public required IReadOnlyCollection<OrderLineRequestDto> Lines { get; init; }
    }
}
