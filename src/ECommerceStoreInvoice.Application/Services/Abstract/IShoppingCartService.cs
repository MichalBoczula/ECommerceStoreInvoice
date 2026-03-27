using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts;

namespace ECommerceStoreInvoice.Application.Services.Abstract
{
    public interface IShoppingCartService
    {
        Task<ShoppingCartResponseDto?> GetShoppingCartByClientId(Guid clientId);
        Task<ShoppingCartResponseDto> CreateShoppingCart(Guid clientId);
        Task<ShoppingCartResponseDto> UpdateShoppingCart(UpdateShoppingCartRequestDto request);
    }
}
