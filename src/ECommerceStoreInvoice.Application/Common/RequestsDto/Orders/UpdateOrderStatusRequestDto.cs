namespace ECommerceStoreInvoice.Application.Common.RequestsDto.Orders
{
    public sealed record UpdateOrderStatusRequestDto
    {
        public required string Status { get; init; }
    }
}
