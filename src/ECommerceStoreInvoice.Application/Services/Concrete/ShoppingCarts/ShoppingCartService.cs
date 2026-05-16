using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Descriptors.ShoppingCarts;
using ECommerceStoreInvoice.Application.Services.Abstract.ShoppingCarts;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using Microsoft.Extensions.Logging;

namespace ECommerceStoreInvoice.Application.Services.Concrete.ShoppingCarts
{
    internal sealed class ShoppingCartService(
        IShoppingCartRepository shoppingCartRepository,
        IValidationPolicy<IReadOnlyCollection<ShoppingCartLine>> _shoppingCartLineValidationPolicy,
        IValidationPolicy<Guid> _guidValidationPolicy,
        ILogger<ShoppingCartService> logger)
        : IShoppingCartService
    {
        public async Task<ShoppingCartResponseDto> GetShoppingCartByClientId(Guid clientId)
        {
            logger.LogInformation("Processing shopping cart read request for ClientId: {ClientId}", clientId);

            var descriptor = new GetShoppingCartByClientIdDescriptor();

            var validationResult = await descriptor.ValidateClientId(clientId, _guidValidationPolicy);
            descriptor.ThrowValidationExceptionIfClientIdInvalid(validationResult);

            var shoppingCart = await descriptor.LoadShoppingCart(clientId, shoppingCartRepository);
            descriptor.ThrowNotFoundExceptionIfShoppingCartMissing(clientId, shoppingCart);

            var response = descriptor.MapToResponse(shoppingCart!);
            logger.LogInformation("Successfully retrieved shopping cart for ClientId: {ClientId}, ShoppingCartId: {ShoppingCartId}", clientId, response.Id);

            return response;
        }

        public async Task<ShoppingCartResponseDto> CreateShoppingCart(Guid clientId)
        {
            logger.LogInformation("Initiating shopping cart creation flow for ClientId: {ClientId}", clientId);

            var descriptor = new CreateShoppingCartDescriptor();

            var validationResult = await descriptor.ValidateClientId(clientId, _guidValidationPolicy);
            descriptor.ThrowValidationExceptionIfClientIdInvalid(validationResult);

            var existingShoppingCart = await descriptor.LoadShoppingCart(clientId, shoppingCartRepository);
            descriptor.ThrowAlreadyExistsExceptionIfShoppingCartExists(clientId, existingShoppingCart);

            var shoppingCart = descriptor.Create(clientId);
            var createdShoppingCart = await descriptor.SaveShoppingCart(shoppingCart, shoppingCartRepository);

            var response = descriptor.MapToResponse(createdShoppingCart);
            logger.LogInformation("Successfully created shopping cart. ShoppingCartId: {ShoppingCartId} for ClientId: {ClientId}", response.Id, clientId);

            return response;
        }

        public async Task<ShoppingCartResponseDto> UpdateShoppingCart(Guid clientId, UpdateShoppingCartRequestDto request)
        {
            logger.LogInformation("Initiating shopping cart update flow for ClientId: {ClientId}", clientId);

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
            var response = descriptor.MapToResponse(updatedShoppingCart);

            logger.LogInformation("Successfully updated shopping cart. ShoppingCartId: {ShoppingCartId} for ClientId: {ClientId}", response.Id, clientId);

            return response;
        }
    }
}
