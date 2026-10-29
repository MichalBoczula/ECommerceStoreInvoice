using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Descriptors.ShoppingCarts
{
    internal sealed record UpdateShoppingCart;

    [Documentation("inv.flow.update-shopping-cart", "Replace all stored cart lines after validation; an empty replacement clears the cart and unrelated clients stay isolated.")]

    internal sealed class UpdateShoppingCartDescriptor : FlowDescriberBase<UpdateShoppingCart>
    {
        [FlowStep(order: 1, bpmnId: "ValidateClientId", description: "Validate the client identifier with the registered GUID policy.")]
        public async Task<ValidationResult> ValidateClientId(Guid clientId, IValidationPolicy<Guid> guidValidationPolicy)
        {
            return await guidValidationPolicy.Validate(clientId);
        }

        [FlowStep(order: 2, bpmnId: "IsClientIdValid", description: "Reject the flow with the complete registered validation errors when client id invalid.")]
        public void ThrowValidationExceptionIfClientIdInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 3, bpmnId: "LoadExistingShoppingCart", description: "Load the cart belonging to the selected client.")]
        public async Task<ShoppingCart?> LoadShoppingCart(Guid clientId, IShoppingCartRepository shoppingCartRepository)
        {
            return await shoppingCartRepository.GetShoppingCartByClientId(clientId);
        }

        [FlowStep(order: 4, bpmnId: "IsShoppingCartExists", description: "Reject when the client has no cart.")]
        public void ThrowNotFoundExceptionIfShoppingCartMissing(Guid clientId, ShoppingCart? shoppingCart)
        {
            if (shoppingCart is null)
            {
                throw new ResourceNotFoundException(nameof(LoadShoppingCart), clientId, nameof(ShoppingCart));
            }
        }

        [FlowStep(order: 5, bpmnId: "MapShoppingCartLinesRequest", description: "Map every requested line to a domain line in the supplied order.")]
        public IReadOnlyCollection<ShoppingCartLine> MapRequestLines(UpdateShoppingCartRequestDto request)
        {
            return MappingConfig.MapToDomain(request.Lines);
        }

        [FlowStep(order: 6, bpmnId: "AssignShoppingCartLines", description: "Replace the target cart lines and update its timestamp without altering other clients.")]
        public void ReplaceShoppingCartLines(ShoppingCart shoppingCart, IReadOnlyCollection<ShoppingCartLine> lines)
        {
            shoppingCart.ReplaceLines(lines);
        }

        [FlowStep(order: 7, bpmnId: "ValidateShoppingCartLines", description: "Validate every replacement cart line with the registered line policy.")]
        public async Task<ValidationResult> ValidateLines(
            IReadOnlyCollection<ShoppingCartLine> lines,
            IValidationPolicy<IReadOnlyCollection<ShoppingCartLine>> shoppingCartLineValidationPolicy)
        {
            return await shoppingCartLineValidationPolicy.Validate(lines);
        }

        [FlowStep(order: 8, bpmnId: "IsShoppingCartLinesValid", description: "Reject the flow with the complete registered validation errors when lines invalid.")]
        public void ThrowValidationExceptionIfLinesInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 9, bpmnId: "SaveShoppingCart", description: "Persist the target cart through the repository and active transaction where applicable.")]
        public async Task<ShoppingCart> SaveShoppingCart(ShoppingCart shoppingCart, IShoppingCartRepository shoppingCartRepository)
        {
            return await shoppingCartRepository.UpdateShoppingCart(shoppingCart);
        }

        [FlowStep(order: 10, bpmnId: "MapShoppingCartResponse", description: "Return the complete update shopping cart response from the domain values; this description is not execution tracing.")]
        public ShoppingCartResponseDto MapToResponse(ShoppingCart shoppingCart)
        {
            return MappingConfig.MapToResponse(shoppingCart);
        }
    }
}
