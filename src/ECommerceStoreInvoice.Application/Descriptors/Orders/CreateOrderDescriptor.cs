using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.ExternalServices;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Descriptors.Orders
{
    internal sealed record CreateOrder;

    [Documentation("inv.flow.create-order", "Fetch catalog products once, capture immutable product snapshots and atomically save snapshots/order/cleared cart in a Mongo transaction.")]

    internal sealed class CreateOrderDescriptor : FlowDescriberBase<CreateOrder>
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

        [FlowStep(order: 3, bpmnId: "LoadShoppingCart", description: "Load the cart belonging to the selected client.")]
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

        [FlowStep(order: 5, bpmnId: "GetRequestedProductIds", description: "Read distinct product identifiers from the original cart lines without changing them.")]
        public IReadOnlyCollection<Guid> GetRequestedProductIds(ShoppingCart shoppingCart)
        {
            return shoppingCart.Lines.Select(line => line.ProductId).Distinct().ToArray();
        }

        [FlowStep(order: 6, bpmnId: "FetchExternalProducts", description: "Fetch all distinct requested catalog products in one adapter call.")]
        public async Task<IReadOnlyCollection<ExternalProductSnapshot>> FetchExternalProducts(
            IReadOnlyCollection<Guid> requestedIds, IProductServiceClient productServiceClient)
        {
            return await productServiceClient.GetProductsByIds(requestedIds);
        }

        [FlowStep(order: 7, bpmnId: "EnsureAllProductsExist", description: "Reject missing requested catalog products before snapshot or order writes.")]
        public void ThrowNotFoundExceptionIfProductMissing(
            IReadOnlyCollection<Guid> requestedIds, IReadOnlyCollection<ExternalProductSnapshot> externalProducts)
        {
            var foundIds = externalProducts.Select(product => product.ProductId).ToHashSet();
            var missingId = requestedIds.FirstOrDefault(id => !foundIds.Contains(id));
            if (missingId != Guid.Empty)
                throw new ResourceNotFoundException(nameof(FetchExternalProducts), missingId, nameof(ExternalProductSnapshot));
        }

        [FlowStep(order: 8, bpmnId: "CreateProductSnapshots", description: "Create distinct immutable snapshots in the original cart-line order from fetched catalog values.")]
        public IReadOnlyCollection<ProductVersion> CreateProductSnapshots(
            ShoppingCart shoppingCart, IReadOnlyCollection<ExternalProductSnapshot> externalProducts)
        {
            var productsById = externalProducts.ToDictionary(product => product.ProductId);
            return shoppingCart.Lines.Select(line =>
            {
                var product = productsById[line.ProductId];
                return new ProductVersion(product.ProductId, product.Price, product.Name, product.Brand);
            }).ToList();
        }

        [FlowStep(order: 9, bpmnId: "ValidateProductSnapshots", description: "Validate each newly constructed product snapshot before any transactional write.")]
        public async Task<IReadOnlyCollection<ValidationResult>> ValidateProductSnapshots(
            IReadOnlyCollection<ProductVersion> productVersions,
            IValidationPolicy<ProductVersion> productVersionValidationPolicy)
        {
            var results = new List<ValidationResult>();
            foreach (var version in productVersions)
                results.Add(await productVersionValidationPolicy.Validate(version));

            return results;
        }

        [FlowStep(order: 10, bpmnId: "EnsureProductSnapshotsValid", description: "Reject invalid catalog-derived snapshots before starting the order write transaction.")]
        public void ThrowValidationExceptionIfProductSnapshotInvalid(
            IReadOnlyCollection<ValidationResult> validationResults)
        {
            var firstInvalid = validationResults.FirstOrDefault(result => !result.IsValid);
            if (firstInvalid is not null)
                throw new ValidationException(firstInvalid);
        }

        [FlowStep(order: 11, bpmnId: "MapOrderDomain", description: "Construct the create order domain values without saving them.")]
        public Order MapToDomain(ShoppingCart shoppingCart, IReadOnlyCollection<ProductVersion> productVersions)
        {
            return MappingConfig.MapToDomain(shoppingCart, productVersions);
        }

        [FlowStep(order: 12, bpmnId: "ValidateOrder", description: "Validate the constructed order before starting its write transaction.")]
        public async Task<ValidationResult> ValidateOrder(Order order, IValidationPolicy<Order> orderValidationPolicy)
        {
            return await orderValidationPolicy.Validate(order);
        }

        [FlowStep(order: 13, bpmnId: "IsOrderValid", description: "Reject the flow with the complete registered validation errors when order invalid.")]
        public void ThrowValidationExceptionIfOrderInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 14, bpmnId: "BeginOrderWriteTransaction", description: "Begin the real Mongo transaction that owns the order, snapshot and cart writes.")]
        public async Task BeginOrderWriteTransaction(IOrderWriteTransaction transaction)
        {
            await transaction.BeginAsync();
        }

        [FlowStep(order: 15, bpmnId: "SaveProductVersions", description: "Stage every immutable product snapshot in the active order transaction.")]
        public async Task<IReadOnlyCollection<ProductVersion>> SaveProductVersions(
            IReadOnlyCollection<ProductVersion> productVersions,
            IProductVersionRepository productVersionRepository)
        {
            return await productVersionRepository.CreateProductVersions(productVersions);
        }

        [FlowStep(order: 16, bpmnId: "SaveOrder", description: "Persist the order using the repository contract; a stale status replacement throws a conflict.")]
        public async Task<Order> SaveOrder(Order order, IOrderRepository orderRepository)
        {
            return await orderRepository.CreateOrder(order);
        }

        [FlowStep(order: 17, bpmnId: "ClearShoppingCart", description: "Clear the source cart only after constructing the order from its original lines.")]
        public void ClearShoppingCart(ShoppingCart shoppingCart)
        {
            shoppingCart.Clear();
        }

        [FlowStep(order: 18, bpmnId: "SaveShoppingCart", description: "Persist the target cart through the repository and active transaction where applicable.")]
        public async Task SaveShoppingCart(ShoppingCart shoppingCart, IShoppingCartRepository shoppingCartRepository)
        {
            await shoppingCartRepository.UpdateShoppingCart(shoppingCart);
        }

        [FlowStep(order: 19, bpmnId: "CommitOrderWriteTransaction", description: "Commit all staged snapshot, order and cart writes as one transaction.")]
        public async Task CommitOrderWriteTransaction(IOrderWriteTransaction transaction)
        {
            await transaction.CommitAsync();
        }

        [FlowStep(order: 20, bpmnId: "RollbackOrderWriteTransactionOnFailure", description: "Abort staged writes after a transaction failure and clear the active session.")]
        public async Task RollbackOrderWriteTransactionOnFailure(IOrderWriteTransaction transaction)
        {
            await transaction.RollbackAsync();
        }

        [FlowStep(order: 21, bpmnId: "MapOrderResponse", description: "Return the complete create order response from the domain values; this description is not execution tracing.")]
        public OrderResponseDto MapToResponse(Order order, IReadOnlyCollection<ProductVersion> productVersions)
        {
            var productVersionDtos = productVersions
                .Select(MappingConfig.MapToResponse)
                .ToList();

            return MappingConfig.MapToResponse(order, productVersionDtos);
        }
    }
}
