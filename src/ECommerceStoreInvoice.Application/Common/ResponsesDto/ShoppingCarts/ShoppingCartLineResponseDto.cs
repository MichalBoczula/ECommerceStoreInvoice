namespace ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts
{
    /// <summary>One stored cart line without catalog enrichment.</summary>
    public sealed record ShoppingCartLineResponseDto
    {
        /// <summary>Catalog product UUID stored in this cart line.</summary>
        public required Guid ProductId { get; init; }
        /// <summary>Exact stored int32 quantity.</summary>
        public required int Quantity { get; init; }
    }
}