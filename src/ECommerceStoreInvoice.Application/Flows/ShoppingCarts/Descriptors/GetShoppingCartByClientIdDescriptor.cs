using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Flows.ShoppingCarts.Descriptors
{
    internal sealed record GetShoppingCartByClientId;

    internal sealed class GetShoppingCartByClientIdDescriptor : FlowDescriberBase<GetShoppingCartByClientId>
    {
        [FlowStep(order: 1, bpmnId: "Task_GetShoppingCart_ValidateClientId")]
        public async Task<ValidationResult> ValidateClientId(Guid clientId, IValidationPolicy<Guid> guidValidationPolicy)
        {
            return await guidValidationPolicy.Validate(clientId);
        }

        [FlowStep(order: 2, bpmnId: "Gateway_GetShoppingCart_ClientIdValid")]
        public void ThrowValidationExceptionIfClientIdInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 3, bpmnId: "Task_GetShoppingCart_LoadByClientId")]
        public async Task<ShoppingCart?> LoadShoppingCart(Guid clientId, IShoppingCartRepository shoppingCartRepository)
        {
            return await shoppingCartRepository.GetShoppingCartByClientId(clientId);
        }

        [FlowStep(order: 4, bpmnId: "Gateway_GetShoppingCart_ShoppingCartExists")]
        public void ThrowNotFoundExceptionIfShoppingCartMissing(Guid clientId, ShoppingCart? shoppingCart)
        {
            if (shoppingCart is null)
            {
                throw new ResourceNotFoundException(nameof(LoadShoppingCart), clientId, nameof(ShoppingCart));
            }
        }

        [FlowStep(order: 5, bpmnId: "Task_GetShoppingCart_MapResponse")]
        public ShoppingCartResponseDto MapToResponse(ShoppingCart shoppingCart)
        {
            return MappingConfig.MapToResponse(shoppingCart);
        }
    }
}
