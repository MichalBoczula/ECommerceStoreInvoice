using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Flows.ShoppingCarts.Descriptors
{
    internal sealed record UpdateShoppingCart;

    internal sealed class UpdateShoppingCartDescriptor : FlowDescriberBase<UpdateShoppingCart>
    {
        [FlowStep(order: 1, bpmnId: "Task_UpdateShoppingCart_ValidateClientId")]
        public async Task<ValidationResult> ValidateClientId(Guid clientId, IValidationPolicy<Guid> guidValidationPolicy)
        {
            return await guidValidationPolicy.Validate(clientId);
        }

        [FlowStep(order: 2, bpmnId: "Gateway_UpdateShoppingCart_ClientIdValid")]
        public void ThrowValidationExceptionIfClientIdInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 3, bpmnId: "Task_UpdateShoppingCart_LoadExistingShoppingCart")]
        public async Task<ShoppingCart?> LoadShoppingCart(Guid clientId, IShoppingCartRepository shoppingCartRepository)
        {
            return await shoppingCartRepository.GetShoppingCartByClientId(clientId);
        }

        [FlowStep(order: 4, bpmnId: "Gateway_UpdateShoppingCart_ShoppingCartExists")]
        public void ThrowNotFoundExceptionIfShoppingCartMissing(Guid clientId, ShoppingCart? shoppingCart)
        {
            if (shoppingCart is null)
            {
                throw new ResourceNotFoundException(nameof(LoadShoppingCart), clientId, nameof(ShoppingCart));
            }
        }

        [FlowStep(order: 5, bpmnId: "Task_UpdateShoppingCart_MapRequestLines")]
        public IReadOnlyCollection<ShoppingCartLine> MapRequestLines(UpdateShoppingCartRequestDto request)
        {
            return MappingConfig.MapToDomain(request.Lines);
        }

        [FlowStep(order: 6, bpmnId: "Task_UpdateShoppingCart_AssignLines")]
        public void ReplaceShoppingCartLines(ShoppingCart shoppingCart, IReadOnlyCollection<ShoppingCartLine> lines)
        {
            shoppingCart.ReplaceLines(lines);
        }

        [FlowStep(order: 7, bpmnId: "Task_UpdateShoppingCart_ValidateLines")]
        public async Task<ValidationResult> ValidateLines(
            IReadOnlyCollection<ShoppingCartLine> lines,
            IValidationPolicy<IReadOnlyCollection<ShoppingCartLine>> shoppingCartLineValidationPolicy)
        {
            return await shoppingCartLineValidationPolicy.Validate(lines);
        }

        [FlowStep(order: 8, bpmnId: "Gateway_UpdateShoppingCart_LinesValid")]
        public void ThrowValidationExceptionIfLinesInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 9, bpmnId: "Task_UpdateShoppingCart_Save")]
        public async Task<ShoppingCart> SaveShoppingCart(ShoppingCart shoppingCart, IShoppingCartRepository shoppingCartRepository)
        {
            return await shoppingCartRepository.UpdateShoppingCart(shoppingCart);
        }

        [FlowStep(order: 10, bpmnId: "Task_UpdateShoppingCart_MapResponse")]
        public ShoppingCartResponseDto MapToResponse(ShoppingCart shoppingCart)
        {
            return MappingConfig.MapToResponse(shoppingCart);
        }
    }
}
