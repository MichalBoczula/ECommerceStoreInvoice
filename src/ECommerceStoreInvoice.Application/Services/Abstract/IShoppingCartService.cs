using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts;

namespace ECommerceStoreInvoice.Application.Services.Abstract
{
    public interface IShoppingCartService
    {
        Task<ShoppingCartResponseDto?> GetShoppingCartByClientId(Guid clientId);
        Task<ShoppingCartResponseDto> CreateShoppingCart(Guid clientId);
        Task<ShoppingCartResponseDto> UpdateShoppingCart(Guid clientId, UpdateShoppingCartRequestDto request);
        FlowDescriptor GetShoppingCartByClientIdDescriptor();
        FlowDescriptor GetUpdateShoppingCartDescriptor();
    }
}
