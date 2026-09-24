using ECommerceStoreInvoice.Application.Services.Concrete.Orders;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.ExternalServices;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace ECommerceStoreInvoice.Application.UnitTests.Services.Orders;

public sealed class OrderProductSnapshotsTests
{
    [Fact]
    public void CreateOrderFlow_DescribesFetchCreateAndValidationAsSeparateSteps()
    {
        var flow = new OrderDescriptorService().GetCreateOrderDescriptor();
        var names = flow.Steps.Select(step => step.StepName).ToList();

        names.Skip(4).Take(6).ShouldBe(new[]
        {
            "GetRequestedProductIds",
            "FetchExternalProducts",
            "ThrowNotFoundExceptionIfProductMissing",
            "CreateProductSnapshots",
            "ValidateProductSnapshots",
            "ThrowValidationExceptionIfProductSnapshotInvalid"
        });
        flow.Steps.Select(step => step.Order).ShouldBe(Enumerable.Range(1, flow.Steps.Count));
    }

    [Fact]
    public async Task CreateOrder_UsesProductsSnapshotsInCartOrderAndPersistsTheirRealPrices()
    {
        var clientId = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var cart = Cart(clientId, (firstId, 2), (secondId, 1));
        var setup = new Setup(cart);

        // Products may return results in a different order than the shopping cart.
        setup.Products.Setup(x => x.GetProductsByIds(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ExternalProductSnapshot(secondId, "Phone B", "Brand B", new Money(49.50m, "USD")),
                new ExternalProductSnapshot(firstId, "Phone A", "Brand A", new Money(125m, "USD"))
            ]);
        IReadOnlyCollection<ProductVersion>? savedVersions = null;
        setup.Versions.Setup(x => x.CreateProductVersions(It.IsAny<IReadOnlyCollection<ProductVersion>>()))
            .ReturnsAsync((IReadOnlyCollection<ProductVersion> versions) =>
            {
                savedVersions = versions;
                return versions;
            });
        setup.Orders.Setup(x => x.CreateOrder(It.IsAny<Order>()))
            .ReturnsAsync((Order order) => order);
        setup.Carts.Setup(x => x.UpdateShoppingCart(It.IsAny<ShoppingCart>()))
            .ReturnsAsync((ShoppingCart shoppingCart) => shoppingCart);

        var response = await setup.Service.CreateOrder(clientId);

        setup.Products.Verify(x => x.GetProductsByIds(
            It.Is<IEnumerable<Guid>>(ids => ids.Count() == 2 && ids.Contains(firstId) && ids.Contains(secondId)),
            It.IsAny<CancellationToken>()), Times.Once);
        savedVersions.ShouldNotBeNull();
        savedVersions.Select(x => x.ProductId).ShouldBe(new[] { firstId, secondId });
        savedVersions.Select(x => x.Name).ShouldBe(new[] { "Phone A", "Phone B" });
        savedVersions.Select(x => x.Brand).ShouldBe(new[] { "Brand A", "Brand B" });
        response.Lines.Select(x => x.ProductVersion.PriceAmount).ShouldBe(new[] { 125m, 49.50m });
        response.TotalAmount.ShouldBe(299.50m);
        response.TotalCurrency.ShouldBe("USD");
        cart.Lines.ShouldBeEmpty();
        setup.Orders.Verify(x => x.CreateOrder(It.IsAny<Order>()), Times.Once);
        setup.Transaction.Verify(x => x.BeginAsync(It.IsAny<CancellationToken>()), Times.Once);
        setup.Transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        setup.Transaction.Verify(x => x.RollbackAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateOrder_WhenProductsOmitsCartItem_DoesNotPersistAnything()
    {
        var clientId = Guid.NewGuid();
        var present = Guid.NewGuid();
        var missing = Guid.NewGuid();
        var cart = Cart(clientId, (present, 1), (missing, 2));
        var setup = new Setup(cart);
        setup.Products.Setup(x => x.GetProductsByIds(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ExternalProductSnapshot(present, "Phone", "Brand", new Money(10m, "USD"))]);

        var error = await Should.ThrowAsync<ResourceNotFoundException>(() => setup.Service.CreateOrder(clientId));

        error.ResourceId.ShouldBe(missing);
        setup.AssertNoWrites();
        cart.Lines.Count.ShouldBe(2);
    }

    [Fact]
    public async Task CreateOrder_WhenProductHasInvalidPrice_DoesNotPersistAnything()
    {
        var clientId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = Cart(clientId, (productId, 1));
        var setup = new Setup(cart);
        setup.Products.Setup(x => x.GetProductsByIds(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ExternalProductSnapshot(productId, "Phone", "Brand", new Money(0m, "USD"))]);
        setup.ProductPolicy.Setup(x => x.Validate(It.IsAny<ProductVersion>()))
            .ReturnsAsync((ProductVersion version) =>
            {
                var result = new ValidationResult();
                if (version.Price.Amount <= 0)
                    result.AddValidationError(new ValidationError { Entity = nameof(ProductVersion), Name = "Price", Message = "Price must be positive" });
                return result;
            });

        await Should.ThrowAsync<ValidationException>(() => setup.Service.CreateOrder(clientId));

        setup.AssertNoWrites();
        cart.Lines.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CreateOrder_WhenOrderValidationFails_DoesNotPersistProductVersions()
    {
        var clientId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = Cart(clientId, (productId, 1));
        var setup = new Setup(cart);
        setup.Products.Setup(x => x.GetProductsByIds(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ExternalProductSnapshot(productId, "Phone", "Brand", new Money(10m, "USD"))]);
        var invalid = new ValidationResult();
        invalid.AddValidationError(new ValidationError { Entity = nameof(Order), Name = "Lines", Message = "Invalid order" });
        setup.OrderPolicy.Setup(x => x.Validate(It.IsAny<Order>())).ReturnsAsync(invalid);

        await Should.ThrowAsync<ValidationException>(() => setup.Service.CreateOrder(clientId));

        setup.AssertNoWrites();
        cart.Lines.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CreateOrder_WhenCartWriteFails_RollsBackOrderTransaction()
    {
        var clientId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var setup = new Setup(Cart(clientId, (productId, 1)));
        setup.Products.Setup(x => x.GetProductsByIds(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ExternalProductSnapshot(productId, "Phone", "Brand", new Money(10m, "USD"))]);
        setup.Versions.Setup(x => x.CreateProductVersions(It.IsAny<IReadOnlyCollection<ProductVersion>>()))
            .ReturnsAsync((IReadOnlyCollection<ProductVersion> versions) => versions);
        setup.Orders.Setup(x => x.CreateOrder(It.IsAny<Order>())).ReturnsAsync((Order order) => order);
        setup.Carts.Setup(x => x.UpdateShoppingCart(It.IsAny<ShoppingCart>()))
            .ThrowsAsync(new InvalidOperationException("Cart write failed"));

        await Should.ThrowAsync<InvalidOperationException>(() => setup.Service.CreateOrder(clientId));

        setup.Transaction.Verify(x => x.BeginAsync(It.IsAny<CancellationToken>()), Times.Once);
        setup.Transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        setup.Transaction.Verify(x => x.RollbackAsync(), Times.Once);
    }

    private static ShoppingCart Cart(Guid clientId, params (Guid Id, int Quantity)[] items)
    {
        var cart = new ShoppingCart(clientId);
        cart.ReplaceLines(items.Select(item => new ShoppingCartLine(item.Id, item.Quantity)));
        return cart;
    }

    private sealed class Setup
    {
        public Mock<IProductServiceClient> Products { get; } = new();
        public Mock<IProductVersionRepository> Versions { get; } = new();
        public Mock<IOrderRepository> Orders { get; } = new();
        public Mock<IOrderWriteTransaction> Transaction { get; } = new();
        public Mock<IShoppingCartRepository> Carts { get; } = new();
        public Mock<IValidationPolicy<ProductVersion>> ProductPolicy { get; } = new();
        public Mock<IValidationPolicy<Order>> OrderPolicy { get; } = new();
        public Mock<IValidationPolicy<(Order order, OrderStatus newStatus)>> UpdateOrderPolicy { get; } = new();
        public Mock<ILogger<OrderService>> Logger { get; } = new();
        public OrderService Service { get; }

        public Setup(ShoppingCart cart)
        {
            var guidPolicy = new Mock<IValidationPolicy<Guid>>();
            guidPolicy.Setup(x => x.Validate(cart.ClientId)).ReturnsAsync(new ValidationResult());
            OrderPolicy.Setup(x => x.Validate(It.IsAny<Order>())).ReturnsAsync(new ValidationResult());
            ProductPolicy.Setup(x => x.Validate(It.IsAny<ProductVersion>())).ReturnsAsync(new ValidationResult());
            Carts.Setup(x => x.GetShoppingCartByClientId(cart.ClientId)).ReturnsAsync(cart);
            Transaction.Setup(x => x.BeginAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            Transaction.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            Transaction.Setup(x => x.RollbackAsync()).Returns(Task.CompletedTask);

            Service = new OrderService(
                Orders.Object, Transaction.Object, Versions.Object, Carts.Object, guidPolicy.Object, OrderPolicy.Object,
                UpdateOrderPolicy.Object, Logger.Object, Products.Object, ProductPolicy.Object);
        }

        public void AssertNoWrites()
        {
            Transaction.Verify(x => x.BeginAsync(It.IsAny<CancellationToken>()), Times.Never);
            Versions.Verify(x => x.CreateProductVersions(It.IsAny<IReadOnlyCollection<ProductVersion>>()), Times.Never);
            Orders.Verify(x => x.CreateOrder(It.IsAny<Order>()), Times.Never);
            Carts.Verify(x => x.UpdateShoppingCart(It.IsAny<ShoppingCart>()), Times.Never);
        }
    }
}
