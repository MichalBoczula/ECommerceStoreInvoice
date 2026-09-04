namespace ECommerceStoreInvoice.Application.Common.RequestsDto.ProductVersions
{
    public sealed record CreateProductVersionRequestDto
    {
        public required Guid ProductId { get; init; }
    }
}
