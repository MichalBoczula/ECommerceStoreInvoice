namespace ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts
{
    /// <summary>Replaces all cart lines. The lines member must be present and non-null; an empty array clears the cart. Unknown members are ignored.</summary>
    public sealed record UpdateShoppingCartRequestDto
    {
        /// <summary>Complete replacement array of non-null cart lines; an empty array is accepted.</summary>
        public required IReadOnlyCollection<ShoppingCartLineRequestDto> Lines { get; init; }
    }
}
