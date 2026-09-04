namespace ECommerceStoreInvoice.Application.Common.RequestsDto.ProductVersions
{
    public sealed record CreateMultipleProductVersionsRequestDto
    {
        public required IEnumerable<Guid> ProductIds { get; init; }
    }
}