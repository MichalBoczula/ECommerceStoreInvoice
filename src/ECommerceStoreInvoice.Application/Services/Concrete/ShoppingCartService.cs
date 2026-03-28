using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Application.Services.Abstract;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;

namespace ECommerceStoreInvoice.Application.Services.Concrete
{
    internal sealed class ShoppingCartService(
        IShoppingCartRepository shoppingCartRepository)
        : IShoppingCartService
    {
        public async Task<ShoppingCartResponseDto?> GetShoppingCartByClientId(Guid clientId)
        {
            var shoppingCart = await shoppingCartRepository.GetShoppingCartByClientId(clientId);

            if (shoppingCart is null)
                return null;

            return MappingConfig.MapToResponse(shoppingCart);
        }

        public async Task<ShoppingCartResponseDto> CreateShoppingCart(Guid clientId)
        {
            var shoppingCart = new ShoppingCart(clientId);

            var createdShoppingCart = await shoppingCartRepository.CreateShoppingCart(shoppingCart);

            return MappingConfig.MapToResponse(createdShoppingCart);
        }

        public async Task<ShoppingCartResponseDto> UpdateShoppingCart(Guid clientId, UpdateShoppingCartRequestDto request)
        {
            var shoppingCart = await shoppingCartRepository.GetShoppingCartByClientId(clientId)
                ?? throw new InvalidOperationException($"Shopping cart for client '{clientId}' was not found.");

            var lines = MappingConfig.MapToDomain(request.Lines);

            shoppingCart.ReplaceLines(lines);

            var updatedShoppingCart = await shoppingCartRepository.UpdateShoppingCart(shoppingCart);

            return MappingConfig.MapToResponse(updatedShoppingCart);
        }
    }
}