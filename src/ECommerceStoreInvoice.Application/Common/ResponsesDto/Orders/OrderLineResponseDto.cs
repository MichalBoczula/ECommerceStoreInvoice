namespace ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders
{
    public sealed record OrderLineResponseDto
    {
        public required Guid ProductVersionId { get; init; }
        public required int Quantity { get; init; }
        public required ProductVersionResponseDto ProductVersion { get; init; }
        public required decimal LineTotalAmount { get; init; }
    }
}