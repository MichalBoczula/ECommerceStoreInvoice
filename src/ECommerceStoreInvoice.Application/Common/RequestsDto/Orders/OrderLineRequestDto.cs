namespace ECommerceStoreInvoice.Application.Common.RequestsDto.Orders
{
    public sealed record OrderLineRequestDto
    {
        public required Guid ProductVersionId { get; init; }
        public required string Name { get; init; }
        public required string Brand { get; init; }
        public required decimal UnitPriceAmount { get; init; }
        public required string UnitPriceCurrency { get; init; }
        public required int Quantity { get; init; }
    }
}
