namespace ECommerceStoreInvoice.Application.Common.RequestsDto.ProductVersions
{
    public sealed record CreateProductVersionRequestDto
    {
        public required Guid ProductId { get; init; }
        public required decimal PriceAmount { get; init; }
        public required string PriceCurrency { get; init; }
        public required string Name { get; init; }
        public required string Brand { get; init; }
    }
}
