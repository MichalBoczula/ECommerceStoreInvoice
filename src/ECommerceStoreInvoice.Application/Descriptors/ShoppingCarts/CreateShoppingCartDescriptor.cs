using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Descriptors.ShoppingCarts
{
    internal sealed record CreateShoppingCart;

    internal sealed class CreateShoppingCartDescriptor : FlowDescriberBase<CreateShoppingCart>
    {
        [FlowStep(order: 1, bpmnId: "ValidateClientId")]
        public async Task<ValidationResult> ValidateClientId(Guid clientId, IValidationPolicy<Guid> guidValidationPolicy)
        {
            return await guidValidationPolicy.Validate(clientId);
        }

        [FlowStep(order: 2, bpmnId: "IsClientIdValid")]
        public void ThrowValidationExceptionIfClientIdInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 3, bpmnId: "LoadExistingShoppingCart")]
        public async Task<ShoppingCart?> LoadShoppingCart(Guid clientId, IShoppingCartRepository shoppingCartRepository)
        {
            return await shoppingCartRepository.GetShoppingCartByClientId(clientId);
        }

        [FlowStep(order: 4, bpmnId: "IsShoppingCartAlreadyExists")]
        public void ThrowAlreadyExistsExceptionIfShoppingCartExists(Guid clientId, ShoppingCart? shoppingCart)
        {
            if (shoppingCart is not null)
            {
                throw new ResourceAlreadyExistsException(nameof(LoadShoppingCart), clientId, nameof(ShoppingCart));
            }
        }

        [FlowStep(order: 5, bpmnId: "CreateShoppingCart")]
        public ShoppingCart Create(Guid clientId)
        {
            return new ShoppingCart(clientId);
        }

        [FlowStep(order: 6, bpmnId: "SaveShoppingCart")]
        public async Task<ShoppingCart> SaveShoppingCart(ShoppingCart shoppingCart, IShoppingCartRepository shoppingCartRepository)
        {
            return await shoppingCartRepository.CreateShoppingCart(shoppingCart);
        }

        [FlowStep(order: 7, bpmnId: "MapShoppingCartResponse")]
        public ShoppingCartResponseDto MapToResponse(ShoppingCart shoppingCart)
        {
            return MappingConfig.MapToResponse(shoppingCart);
        }
    }
}
