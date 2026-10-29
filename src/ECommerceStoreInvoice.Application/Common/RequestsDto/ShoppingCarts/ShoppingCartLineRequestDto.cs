namespace ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts
{
    /// <summary>One requested cart line. Both members must be present; unknown members are ignored.</summary>
    public sealed record ShoppingCartLineRequestDto
    {
        /// <summary>Catalog product UUID; updating a cart does not query ProductsCatalog.</summary>
        public required Guid ProductId { get; init; }
        /// <summary>Canonical JSON int32 quantity checked by cart rules. ASP.NET web binding also accepts quoted int32 text.</summary>
        public required int Quantity { get; init; }
    }
}
