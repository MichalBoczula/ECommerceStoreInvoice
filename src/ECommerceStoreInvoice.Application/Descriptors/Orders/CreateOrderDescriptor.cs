using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Descriptors.Orders
{
    internal sealed record CreateOrder;

    internal sealed class CreateOrderDescriptor : FlowDescriberBase<CreateOrder>
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

        [FlowStep(order: 3, bpmnId: "LoadShoppingCart")]
        public async Task<ShoppingCart?> LoadShoppingCart(Guid clientId, IShoppingCartRepository shoppingCartRepository)
        {
            return await shoppingCartRepository.GetShoppingCartByClientId(clientId);
        }

        [FlowStep(order: 4, bpmnId: "IsShoppingCartExists")]
        public void ThrowNotFoundExceptionIfShoppingCartMissing(Guid clientId, ShoppingCart? shoppingCart)
        {
            if (shoppingCart is null)
            {
                throw new ResourceNotFoundException(nameof(LoadShoppingCart), clientId, nameof(ShoppingCart));
            }
        }

        [FlowStep(order: 5, bpmnId: "CreateProductVersions")]
        public async Task<IReadOnlyCollection<ProductVersion>> CreateProductVersions(
            ShoppingCart shoppingCart,
            IProductVersionRepository productVersionRepository)
        {
            var tasks = shoppingCart.Lines
                .Select(line =>
                    productVersionRepository.CreateProductVersion(
                        new ProductVersion(
                            line.ProductId,
                            line.UnitPrice,
                            line.Name,
                            line.Brand)))
                .ToArray();

            return await Task.WhenAll(tasks);
        }

        [FlowStep(order: 6, bpmnId: "MapOrderDomain")]
        public Order MapToDomain(ShoppingCart shoppingCart, IReadOnlyCollection<ProductVersion> productVersions)
        {
            return MappingConfig.MapToDomain(shoppingCart, productVersions);
        }

        [FlowStep(order: 7, bpmnId: "ValidateOrder")]
        public async Task<ValidationResult> ValidateOrder(Order order, IValidationPolicy<Order> orderValidationPolicy)
        {
            return await orderValidationPolicy.Validate(order);
        }

        [FlowStep(order: 8, bpmnId: "IsOrderValid")]
        public void ThrowValidationExceptionIfOrderInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 9, bpmnId: "SaveOrder")]
        public async Task<Order> SaveOrder(Order order, IOrderRepository orderRepository)
        {
            return await orderRepository.CreateOrder(order);
        }

        [FlowStep(order: 10, bpmnId: "ClearShoppingCart")]
        public void ClearShoppingCart(ShoppingCart shoppingCart)
        {
            shoppingCart.Clear();
        }

        [FlowStep(order: 11, bpmnId: "SaveShoppingCart")]
        public async Task SaveShoppingCart(ShoppingCart shoppingCart, IShoppingCartRepository shoppingCartRepository)
        {
            await shoppingCartRepository.UpdateShoppingCart(shoppingCart);
        }

        [FlowStep(order: 12, bpmnId: "MapOrderResponse")]
        public OrderResponseDto MapToResponse(Order order)
        {
            return MappingConfig.MapToResponse(order);
        }
    }
}
