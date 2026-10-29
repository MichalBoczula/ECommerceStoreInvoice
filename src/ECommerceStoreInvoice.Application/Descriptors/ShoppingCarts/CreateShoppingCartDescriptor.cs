using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Descriptors.ShoppingCarts
{
    internal sealed record CreateShoppingCart;

    [Documentation("inv.flow.create-shopping-cart", "Create one empty cart for a client without querying Users or ProductsCatalog; duplicate ownership is rejected.")]

    internal sealed class CreateShoppingCartDescriptor : FlowDescriberBase<CreateShoppingCart>
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

        [FlowStep(order: 4, bpmnId: "IsShoppingCartAlreadyExists", description: "Reject creation when this client already owns a cart.")]
        public void ThrowAlreadyExistsExceptionIfShoppingCartExists(Guid clientId, ShoppingCart? shoppingCart)
        {
            if (shoppingCart is not null)
            {
                throw new ResourceAlreadyExistsException(nameof(LoadShoppingCart), clientId, nameof(ShoppingCart));
            }
        }

        [FlowStep(order: 5, bpmnId: "CreateShoppingCart", description: "Construct a new empty cart with its own identity and timestamps.")]
        public ShoppingCart Create(Guid clientId)
        {
            return new ShoppingCart(clientId);
        }

        [FlowStep(order: 6, bpmnId: "SaveShoppingCart", description: "Persist the target cart through the repository and active transaction where applicable.")]
        public async Task<ShoppingCart> SaveShoppingCart(ShoppingCart shoppingCart, IShoppingCartRepository shoppingCartRepository)
        {
            return await shoppingCartRepository.CreateShoppingCart(shoppingCart);
        }

        [FlowStep(order: 7, bpmnId: "MapShoppingCartResponse", description: "Return the complete create shopping cart response from the domain values; this description is not execution tracing.")]
        public ShoppingCartResponseDto MapToResponse(ShoppingCart shoppingCart)
        {
            return MappingConfig.MapToResponse(shoppingCart);
        }
    }
}
