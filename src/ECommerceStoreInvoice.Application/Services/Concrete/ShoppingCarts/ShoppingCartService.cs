using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Descriptors.ShoppingCarts;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Application.Services.Abstract.ShoppingCarts;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Services.Concrete.ShoppingCarts
{
    internal sealed class ShoppingCartService(
        IShoppingCartRepository shoppingCartRepository,
        IValidationPolicy<IReadOnlyCollection<ShoppingCartLine>> _shoppingCartLineValidationPolicy,
        IValidationPolicy<Guid> _guidValidationPolicy)
        : IShoppingCartService
    {
        public async Task<ShoppingCartResponseDto?> GetShoppingCartByClientId(Guid clientId)
        {
            var descriptor = new GetShoppingCartByClientIdDescriptor();

            var validationResult = await descriptor.ValidateClientId(clientId, _guidValidationPolicy);
            descriptor.ThrowValidationExceptionIfClientIdInvalid(validationResult);

            var shoppingCart = await descriptor.LoadShoppingCart(clientId, shoppingCartRepository);
            descriptor.ThrowNotFoundExceptionIfShoppingCartMissing(clientId, shoppingCart);

            return descriptor.MapToResponse(shoppingCart!);
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
            var descriptor = new UpdateShoppingCartDescriptor();

            var validationResult = await descriptor.ValidateClientId(clientId, _guidValidationPolicy);
            descriptor.ThrowValidationExceptionIfClientIdInvalid(validationResult);

            var shoppingCart = await descriptor.LoadShoppingCart(clientId, shoppingCartRepository);
            descriptor.ThrowNotFoundExceptionIfShoppingCartMissing(clientId, shoppingCart);

            var lines = descriptor.MapRequestLines(request);
            descriptor.ReplaceShoppingCartLines(shoppingCart!, lines);

            validationResult = await descriptor.ValidateLines(lines, _shoppingCartLineValidationPolicy);
            descriptor.ThrowValidationExceptionIfLinesInvalid(validationResult);

            var updatedShoppingCart = await descriptor.SaveShoppingCart(shoppingCart!, shoppingCartRepository);
            return descriptor.MapToResponse(updatedShoppingCart);
        }
    }
}
