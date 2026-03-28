using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Application.Services.Abstract;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Services.Concrete
{
    internal sealed class ShoppingCartService(
        IShoppingCartRepository shoppingCartRepository,
        IValidationPolicy<IReadOnlyCollection<ShoppingCartLine>> _shoppingCartLineValidationPolicy,
        IValidationPolicy<Guid> _guidValidationPolicy)
        : IShoppingCartService
    {
        public async Task<ShoppingCartResponseDto?> GetShoppingCartByClientId(Guid clientId)
        {
            var validationResult = await _guidValidationPolicy.Validate(clientId);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }

            var shoppingCart = await shoppingCartRepository.GetShoppingCartByClientId(clientId);

            if (shoppingCart is null)
                throw new ResourceNotFoundException(nameof(GetShoppingCartByClientId), clientId, nameof(ShoppingCart));

            return MappingConfig.MapToResponse(shoppingCart);
        }

        public async Task<ShoppingCartResponseDto> CreateShoppingCart(Guid clientId)
        {
            var validationResult = await _guidValidationPolicy.Validate(clientId);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }

            var existingShoppingCart = await shoppingCartRepository.GetShoppingCartByClientId(clientId);

            if (existingShoppingCart is not null)
                throw new ResourceAlreadyExistsException(nameof(GetShoppingCartByClientId), clientId, nameof(ShoppingCart));

            var shoppingCart = new ShoppingCart(clientId);

            var createdShoppingCart = await shoppingCartRepository.CreateShoppingCart(shoppingCart);

            return MappingConfig.MapToResponse(createdShoppingCart);
        }

        public async Task<ShoppingCartResponseDto> UpdateShoppingCart(Guid clientId, UpdateShoppingCartRequestDto request)
        {
            var validationResult = await _guidValidationPolicy.Validate(clientId);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }

            var shoppingCart = await shoppingCartRepository.GetShoppingCartByClientId(clientId);

            if (shoppingCart is null)
                throw new ResourceNotFoundException(nameof(GetShoppingCartByClientId), clientId, nameof(ShoppingCart));

            var lines = MappingConfig.MapToDomain(request.Lines);

            shoppingCart.ReplaceLines(lines);

            validationResult = await _shoppingCartLineValidationPolicy.Validate(lines);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
            var updatedShoppingCart = await shoppingCartRepository.UpdateShoppingCart(shoppingCart);

            return MappingConfig.MapToResponse(updatedShoppingCart);
        }
    }
}