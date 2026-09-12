using ECommerceStoreInvoice.Application.Common.RequestsDto.Orders;
using ECommerceStoreInvoice.Application.Services.Concrete.Orders;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate;

namespace ECommerceStoreInvoice.Application.UnitTests.Services.Orders;

public sealed class OrderServiceTests
{
    [Fact(Skip = "Order require reimplementation")]
    public async Task CreateOrder_WhenRequestIsValid_ShouldValidatePersistClearCartAndReturnResponse()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var shoppingCart = BuildShoppingCart(clientId);
        var expectedCreatedProductVersions = shoppingCart.Lines.Count;
        var guidValidationResult = new ValidationResult();
        var orderValidationResult = new ValidationResult();

        ProductVersion CreateProductVersionFromLine(ShoppingCartLine line) =>
            ProductVersion.Rehydrate(
                Guid.NewGuid(),
                true,
                DateTime.UtcNow,
                null,
                line.ProductId,
                new Money(),
                "",
                "");

        var createdProductVersions = shoppingCart.Lines
            .Select(CreateProductVersionFromLine)
            .ToList();

        var createdOrder = Order.Rehydrate(
            Guid.NewGuid(),
            clientId,
            [
                .. createdProductVersions.Zip(shoppingCart.Lines, (productVersion, line) =>
                    new OrderLine(productVersion.Id, line.Quantity))
            ],
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow,
            OrderStatus.Created);

        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var productVersionRepositoryMock = new Mock<IProductVersionRepository>(MockBehavior.Strict);
        var shoppingCartRepositoryMock = new Mock<IShoppingCartRepository>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var orderValidationPolicyMock = new Mock<IValidationPolicy<Order>>(MockBehavior.Strict);
        var updateOrderValidationPolicyMock = new Mock<IValidationPolicy<(Order order, OrderStatus newStatus)>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<OrderService>>(MockBehavior.Loose);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(guidValidationResult);

        shoppingCartRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetShoppingCartByClientId(clientId))
            .ReturnsAsync(shoppingCart);

        foreach (var line in shoppingCart.Lines)
        {
            var productVersion = createdProductVersions.Single(p => p.ProductId == line.ProductId);
            productVersionRepositoryMock
                .Setup(repo => repo.CreateProductVersion(It.Is<ProductVersion>(pv =>
                    pv.ProductId == line.ProductId)))
                .ReturnsAsync(productVersion);
        }

        orderValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(It.Is<Order>(order =>
                order.ClientId == clientId &&
                order.Lines.Count == shoppingCart.Lines.Count)))
            .ReturnsAsync(orderValidationResult);

        orderRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.CreateOrder(It.IsAny<Order>()))
            .ReturnsAsync(createdOrder);

        shoppingCartRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.UpdateShoppingCart(It.Is<ShoppingCart>(cart =>
                cart.ClientId == clientId &&
                cart.Lines.Count == 0)))
            .ReturnsAsync((ShoppingCart cart) => cart);

        var sut = new OrderService(
            orderRepositoryMock.Object,
            productVersionRepositoryMock.Object,
            shoppingCartRepositoryMock.Object,
            guidValidationPolicyMock.Object,
            orderValidationPolicyMock.Object,
            updateOrderValidationPolicyMock.Object,
            loggerMock.Object);

        // Act
        var response = await sut.CreateOrder(clientId);

        // Assert
        guidValidationPolicyMock.Verify(policy => policy.Validate(clientId), Times.Once);
        shoppingCartRepositoryMock.Verify(repo => repo.GetShoppingCartByClientId(clientId), Times.Once);
        productVersionRepositoryMock.Verify(repo => repo.CreateProductVersion(It.IsAny<ProductVersion>()), Times.Exactly(expectedCreatedProductVersions));
        orderValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<Order>()), Times.Once);
        orderRepositoryMock.Verify(repo => repo.CreateOrder(It.IsAny<Order>()), Times.Once);
        shoppingCartRepositoryMock.Verify(repo => repo.UpdateShoppingCart(It.IsAny<ShoppingCart>()), Times.Once);
        updateOrderValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<(Order order, OrderStatus newStatus)>()), Times.Never);

        response.ShouldNotBeNull();
        response.Id.ShouldBe(createdOrder.Id);
        response.ClientId.ShouldBe(clientId);
        response.Status.ShouldBe(OrderStatus.Created.ToString());
        response.Lines.Count.ShouldBe(createdOrder.Lines.Count);
    }

    [Fact]
    public async Task CreateOrder_WhenClientIdValidationFails_ShouldThrowValidationExceptionAndNotLoadData()
    {
        // Arrange
        var clientId = Guid.Empty;

        var invalidResult = new ValidationResult();
        invalidResult.AddValidationError(new ValidationError
        {
            Entity = nameof(Guid),
            Name = "clientId",
            Message = "ClientId cannot be empty"
        });

        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var productVersionRepositoryMock = new Mock<IProductVersionRepository>(MockBehavior.Strict);
        var shoppingCartRepositoryMock = new Mock<IShoppingCartRepository>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var orderValidationPolicyMock = new Mock<IValidationPolicy<Order>>(MockBehavior.Strict);
        var updateOrderValidationPolicyMock = new Mock<IValidationPolicy<(Order order, OrderStatus newStatus)>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<OrderService>>(MockBehavior.Loose);

        guidValidationPolicyMock
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(invalidResult);

        var sut = new OrderService(
            orderRepositoryMock.Object,
            productVersionRepositoryMock.Object,
            shoppingCartRepositoryMock.Object,
            guidValidationPolicyMock.Object,
            orderValidationPolicyMock.Object,
            updateOrderValidationPolicyMock.Object,
            loggerMock.Object);

        // Act / Assert
        await Should.ThrowAsync<ValidationException>(() => sut.CreateOrder(clientId));

        shoppingCartRepositoryMock.Verify(repo => repo.GetShoppingCartByClientId(It.IsAny<Guid>()), Times.Never);
        productVersionRepositoryMock.Verify(repo => repo.CreateProductVersion(It.IsAny<ProductVersion>()), Times.Never);
        orderValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<Order>()), Times.Never);
        orderRepositoryMock.Verify(repo => repo.CreateOrder(It.IsAny<Order>()), Times.Never);
        shoppingCartRepositoryMock.Verify(repo => repo.UpdateShoppingCart(It.IsAny<ShoppingCart>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrder_WhenShoppingCartIsMissing_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var guidValidationResult = new ValidationResult();

        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var productVersionRepositoryMock = new Mock<IProductVersionRepository>(MockBehavior.Strict);
        var shoppingCartRepositoryMock = new Mock<IShoppingCartRepository>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var orderValidationPolicyMock = new Mock<IValidationPolicy<Order>>(MockBehavior.Strict);
        var updateOrderValidationPolicyMock = new Mock<IValidationPolicy<(Order order, OrderStatus newStatus)>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<OrderService>>(MockBehavior.Loose);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(guidValidationResult);

        shoppingCartRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetShoppingCartByClientId(clientId))
            .ReturnsAsync((ShoppingCart?)null);

        var sut = new OrderService(
            orderRepositoryMock.Object,
            productVersionRepositoryMock.Object,
            shoppingCartRepositoryMock.Object,
            guidValidationPolicyMock.Object,
            orderValidationPolicyMock.Object,
            updateOrderValidationPolicyMock.Object,
            loggerMock.Object);

        // Act / Assert
        await Should.ThrowAsync<ResourceNotFoundException>(() => sut.CreateOrder(clientId));

        productVersionRepositoryMock.Verify(repo => repo.CreateProductVersion(It.IsAny<ProductVersion>()), Times.Never);
        orderValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<Order>()), Times.Never);
        orderRepositoryMock.Verify(repo => repo.CreateOrder(It.IsAny<Order>()), Times.Never);
        shoppingCartRepositoryMock.Verify(repo => repo.UpdateShoppingCart(It.IsAny<ShoppingCart>()), Times.Never);
    }

    [Fact(Skip = "Order require reimplementation")]
    public async Task CreateOrder_WhenOrderValidationFails_ShouldThrowValidationExceptionAndNotPersistOrClearCart()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var shoppingCart = BuildShoppingCart(clientId);
        var guidValidationResult = new ValidationResult();

        var invalidOrderValidationResult = new ValidationResult();
        invalidOrderValidationResult.AddValidationError(new ValidationError
        {
            Entity = nameof(Order),
            Name = nameof(Order.Lines),
            Message = "Order must contain lines"
        });

        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var productVersionRepositoryMock = new Mock<IProductVersionRepository>(MockBehavior.Strict);
        var shoppingCartRepositoryMock = new Mock<IShoppingCartRepository>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var orderValidationPolicyMock = new Mock<IValidationPolicy<Order>>(MockBehavior.Strict);
        var updateOrderValidationPolicyMock = new Mock<IValidationPolicy<(Order order, OrderStatus newStatus)>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<OrderService>>(MockBehavior.Loose);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(guidValidationResult);

        shoppingCartRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetShoppingCartByClientId(clientId))
            .ReturnsAsync(shoppingCart);

        foreach (var line in shoppingCart.Lines)
        {
            productVersionRepositoryMock
                .Setup(repo => repo.CreateProductVersion(It.Is<ProductVersion>(pv => pv.ProductId == line.ProductId)))
                .ReturnsAsync(new ProductVersion(line.ProductId, new Money(), "", ""));
        }

        orderValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(It.IsAny<Order>()))
            .ReturnsAsync(invalidOrderValidationResult);

        var sut = new OrderService(
            orderRepositoryMock.Object,
            productVersionRepositoryMock.Object,
            shoppingCartRepositoryMock.Object,
            guidValidationPolicyMock.Object,
            orderValidationPolicyMock.Object,
            updateOrderValidationPolicyMock.Object,
            loggerMock.Object);

        // Act / Assert
        await Should.ThrowAsync<ValidationException>(() => sut.CreateOrder(clientId));

        orderRepositoryMock.Verify(repo => repo.CreateOrder(It.IsAny<Order>()), Times.Never);
        shoppingCartRepositoryMock.Verify(repo => repo.UpdateShoppingCart(It.IsAny<ShoppingCart>()), Times.Never);
    }

    [Fact]
    public async Task GetOrdersByClientId_WhenClientIdIsValid_ShouldReturnResponseCollection()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var guidValidationResult = new ValidationResult();
        var orders = BuildOrders(clientId);
        var ordersWithProductVersions = orders
            .Select(order => (
                Order: order,
                ProductVersions: BuildProductVersions(order)))
            .ToList();

        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var productVersionRepositoryMock = new Mock<IProductVersionRepository>(MockBehavior.Strict);
        var shoppingCartRepositoryMock = new Mock<IShoppingCartRepository>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var orderValidationPolicyMock = new Mock<IValidationPolicy<Order>>(MockBehavior.Strict);
        var updateOrderValidationPolicyMock = new Mock<IValidationPolicy<(Order order, OrderStatus newStatus)>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<OrderService>>(MockBehavior.Loose);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(guidValidationResult);

        orderRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetOrdersWithProductVersionsByClientId(clientId))
            .ReturnsAsync(ordersWithProductVersions);

        var sut = new OrderService(
            orderRepositoryMock.Object,
            productVersionRepositoryMock.Object,
            shoppingCartRepositoryMock.Object,
            guidValidationPolicyMock.Object,
            orderValidationPolicyMock.Object,
            updateOrderValidationPolicyMock.Object,
            loggerMock.Object);

        // Act
        var response = await sut.GetOrdersByClientId(clientId);

        // Assert
        response.Count.ShouldBe(orders.Count);
        response.Select(x => x.Id).ShouldBe(orders.Select(x => x.Id), ignoreOrder: true);

        foreach (var (order, productVersions) in ordersWithProductVersions)
        {
            var orderResponse = response.Single(x => x.Id == order.Id);
            var productVersion = productVersions.Single();
            var orderLine = order.Lines.Single();

            orderResponse.Lines.Count.ShouldBe(order.Lines.Count);
            orderResponse.Lines.Single().ProductVersionId.ShouldBe(productVersion.Id);
            orderResponse.Lines.Single().ProductVersion.Id.ShouldBe(productVersion.Id);
            orderResponse.Lines.Single().LineTotalAmount.ShouldBe(productVersion.Price.Amount * orderLine.Quantity);
            orderResponse.TotalAmount.ShouldBe(productVersion.Price.Amount * orderLine.Quantity);
            orderResponse.TotalCurrency.ShouldBe(productVersion.Price.Currency);
        }

        guidValidationPolicyMock.Verify(policy => policy.Validate(clientId), Times.Once);
        orderRepositoryMock.Verify(repo => repo.GetOrdersWithProductVersionsByClientId(clientId), Times.Once);
        orderValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<Order>()), Times.Never);
        updateOrderValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<(Order order, OrderStatus newStatus)>()), Times.Never);
    }

    [Fact]
    public async Task GetOrdersByClientId_WhenClientIdValidationFails_ShouldThrowValidationExceptionAndNotLoadOrders()
    {
        // Arrange
        var clientId = Guid.Empty;

        var invalidResult = new ValidationResult();
        invalidResult.AddValidationError(new ValidationError
        {
            Entity = nameof(Guid),
            Name = "clientId",
            Message = "ClientId cannot be empty"
        });

        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var productVersionRepositoryMock = new Mock<IProductVersionRepository>(MockBehavior.Strict);
        var shoppingCartRepositoryMock = new Mock<IShoppingCartRepository>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var orderValidationPolicyMock = new Mock<IValidationPolicy<Order>>(MockBehavior.Strict);
        var updateOrderValidationPolicyMock = new Mock<IValidationPolicy<(Order order, OrderStatus newStatus)>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<OrderService>>(MockBehavior.Loose);

        guidValidationPolicyMock
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(invalidResult);

        var sut = new OrderService(
            orderRepositoryMock.Object,
            productVersionRepositoryMock.Object,
            shoppingCartRepositoryMock.Object,
            guidValidationPolicyMock.Object,
            orderValidationPolicyMock.Object,
            updateOrderValidationPolicyMock.Object,
            loggerMock.Object);

        // Act / Assert
        await Should.ThrowAsync<ValidationException>(() => sut.GetOrdersByClientId(clientId));

        orderRepositoryMock.Verify(repo => repo.GetOrdersWithProductVersionsByClientId(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetOrderByOrderId_WhenOrderIdIsValidAndEntityExists_ShouldReturnResponse()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var guidValidationResult = new ValidationResult();
        var order = BuildOrder(orderId, clientId, OrderStatus.Paid);
        var productVersions = BuildProductVersions(order);

        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var productVersionRepositoryMock = new Mock<IProductVersionRepository>(MockBehavior.Strict);
        var shoppingCartRepositoryMock = new Mock<IShoppingCartRepository>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var orderValidationPolicyMock = new Mock<IValidationPolicy<Order>>(MockBehavior.Strict);
        var updateOrderValidationPolicyMock = new Mock<IValidationPolicy<(Order order, OrderStatus newStatus)>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<OrderService>>(MockBehavior.Loose);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(orderId))
            .ReturnsAsync(guidValidationResult);

        orderRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetOrderWithProductVersionsById(orderId))
            .ReturnsAsync((order, productVersions));

        var sut = new OrderService(
            orderRepositoryMock.Object,
            productVersionRepositoryMock.Object,
            shoppingCartRepositoryMock.Object,
            guidValidationPolicyMock.Object,
            orderValidationPolicyMock.Object,
            updateOrderValidationPolicyMock.Object,
            loggerMock.Object);

        // Act
        var response = await sut.GetOrderByOrderId(orderId);

        // Assert
        response.Id.ShouldBe(order.Id);
        response.ClientId.ShouldBe(clientId);
        response.Status.ShouldBe(OrderStatus.Paid.ToString());

        guidValidationPolicyMock.Verify(policy => policy.Validate(orderId), Times.Once);
        orderRepositoryMock.Verify(repo => repo.GetOrderWithProductVersionsById(orderId), Times.Once);
        updateOrderValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<(Order order, OrderStatus newStatus)>()), Times.Never);
    }

    [Fact]
    public async Task GetOrderByOrderId_WhenEntityDoesNotExist_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var guidValidationResult = new ValidationResult();

        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var productVersionRepositoryMock = new Mock<IProductVersionRepository>(MockBehavior.Strict);
        var shoppingCartRepositoryMock = new Mock<IShoppingCartRepository>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var orderValidationPolicyMock = new Mock<IValidationPolicy<Order>>(MockBehavior.Strict);
        var updateOrderValidationPolicyMock = new Mock<IValidationPolicy<(Order order, OrderStatus newStatus)>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<OrderService>>(MockBehavior.Loose);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(orderId))
            .ReturnsAsync(guidValidationResult);

        orderRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetOrderWithProductVersionsById(orderId))
            .ReturnsAsync(((Order Order, IReadOnlyCollection<ProductVersion> ProductVersions)?)null);

        var sut = new OrderService(
            orderRepositoryMock.Object,
            productVersionRepositoryMock.Object,
            shoppingCartRepositoryMock.Object,
            guidValidationPolicyMock.Object,
            orderValidationPolicyMock.Object,
            updateOrderValidationPolicyMock.Object,
            loggerMock.Object);

        // Act / Assert
        await Should.ThrowAsync<ResourceNotFoundException>(() => sut.GetOrderByOrderId(orderId));

        orderRepositoryMock.Verify(repo => repo.GetOrderWithProductVersionsById(orderId), Times.Once);
    }

    [Fact]
    public async Task UpdateOrderStatus_WhenRequestIsValid_ShouldValidatePolicyUpdateAndReturnResponse()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var request = new UpdateOrderStatusRequestDto { Status = "Paid" };

        var guidValidationResult = new ValidationResult();
        var statusTransitionValidationResult = new ValidationResult();
        var existingOrder = BuildOrder(orderId, Guid.NewGuid(), OrderStatus.Created);
        var productVersions = BuildProductVersions(existingOrder);

        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var productVersionRepositoryMock = new Mock<IProductVersionRepository>(MockBehavior.Strict);
        var shoppingCartRepositoryMock = new Mock<IShoppingCartRepository>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var orderValidationPolicyMock = new Mock<IValidationPolicy<Order>>(MockBehavior.Strict);
        var updateOrderValidationPolicyMock = new Mock<IValidationPolicy<(Order order, OrderStatus newStatus)>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<OrderService>>(MockBehavior.Loose);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(orderId))
            .ReturnsAsync(guidValidationResult);

        orderRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetOrderWithProductVersionsById(orderId))
            .ReturnsAsync((existingOrder, productVersions));

        updateOrderValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(It.Is<(Order order, OrderStatus newStatus)>(ctx =>
                ctx.order == existingOrder &&
                ctx.newStatus == OrderStatus.Paid)))
            .ReturnsAsync(statusTransitionValidationResult);

        orderRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.UpdateOrder(It.Is<Order>(order =>
                order.Id == orderId &&
                order.Status == OrderStatus.Paid)))
            .ReturnsAsync((Order order) => order);

        var sut = new OrderService(
            orderRepositoryMock.Object,
            productVersionRepositoryMock.Object,
            shoppingCartRepositoryMock.Object,
            guidValidationPolicyMock.Object,
            orderValidationPolicyMock.Object,
            updateOrderValidationPolicyMock.Object,
            loggerMock.Object);

        // Act
        var response = await sut.UpdateOrderStatus(orderId, request);

        // Assert
        guidValidationPolicyMock.Verify(policy => policy.Validate(orderId), Times.Once);
        orderRepositoryMock.Verify(repo => repo.GetOrderWithProductVersionsById(orderId), Times.Once);
        updateOrderValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<(Order order, OrderStatus newStatus)>()), Times.Once);
        orderRepositoryMock.Verify(repo => repo.UpdateOrder(It.IsAny<Order>()), Times.Once);
        orderValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<Order>()), Times.Never);

        response.Id.ShouldBe(orderId);
        response.Status.ShouldBe(OrderStatus.Paid.ToString());
        response.Lines.Count.ShouldBe(existingOrder.Lines.Count);
        response.Lines.Single().ProductVersion.Id.ShouldBe(productVersions.Single().Id);
    }

    [Fact]
    public async Task UpdateOrderStatus_WhenStatusTransitionValidationFails_ShouldThrowValidationExceptionAndNotPersist()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var request = new UpdateOrderStatusRequestDto { Status = "Cancelled" };
        var existingOrder = BuildOrder(orderId, Guid.NewGuid(), OrderStatus.Paid);
        var productVersions = BuildProductVersions(existingOrder);

        var guidValidationResult = new ValidationResult();
        var invalidTransitionResult = new ValidationResult();
        invalidTransitionResult.AddValidationError(new ValidationError
        {
            Entity = nameof(Order),
            Name = nameof(Order.Status),
            Message = "Invalid status transition"
        });

        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var productVersionRepositoryMock = new Mock<IProductVersionRepository>(MockBehavior.Strict);
        var shoppingCartRepositoryMock = new Mock<IShoppingCartRepository>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var orderValidationPolicyMock = new Mock<IValidationPolicy<Order>>(MockBehavior.Strict);
        var updateOrderValidationPolicyMock = new Mock<IValidationPolicy<(Order order, OrderStatus newStatus)>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<OrderService>>(MockBehavior.Loose);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(orderId))
            .ReturnsAsync(guidValidationResult);

        orderRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetOrderWithProductVersionsById(orderId))
            .ReturnsAsync((existingOrder, productVersions));

        updateOrderValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(It.Is<(Order order, OrderStatus newStatus)>(ctx =>
                ctx.order == existingOrder &&
                ctx.newStatus == OrderStatus.Cancelled)))
            .ReturnsAsync(invalidTransitionResult);

        var sut = new OrderService(
            orderRepositoryMock.Object,
            productVersionRepositoryMock.Object,
            shoppingCartRepositoryMock.Object,
            guidValidationPolicyMock.Object,
            orderValidationPolicyMock.Object,
            updateOrderValidationPolicyMock.Object,
            loggerMock.Object);

        // Act / Assert
        await Should.ThrowAsync<ValidationException>(() => sut.UpdateOrderStatus(orderId, request));

        orderRepositoryMock.Verify(repo => repo.UpdateOrder(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrderStatus_WhenStatusStringIsInvalid_ShouldThrowValidationExceptionAndNotCallStatusPolicy()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var request = new UpdateOrderStatusRequestDto { Status = "WrongStatus" };
        var existingOrder = BuildOrder(orderId, Guid.NewGuid(), OrderStatus.Created);
        var productVersions = BuildProductVersions(existingOrder);
        var guidValidationResult = new ValidationResult();

        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var productVersionRepositoryMock = new Mock<IProductVersionRepository>(MockBehavior.Strict);
        var shoppingCartRepositoryMock = new Mock<IShoppingCartRepository>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var orderValidationPolicyMock = new Mock<IValidationPolicy<Order>>(MockBehavior.Strict);
        var updateOrderValidationPolicyMock = new Mock<IValidationPolicy<(Order order, OrderStatus newStatus)>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<OrderService>>(MockBehavior.Loose);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(orderId))
            .ReturnsAsync(guidValidationResult);

        orderRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetOrderWithProductVersionsById(orderId))
            .ReturnsAsync((existingOrder, productVersions));

        var sut = new OrderService(
            orderRepositoryMock.Object,
            productVersionRepositoryMock.Object,
            shoppingCartRepositoryMock.Object,
            guidValidationPolicyMock.Object,
            orderValidationPolicyMock.Object,
            updateOrderValidationPolicyMock.Object,
            loggerMock.Object);

        // Act / Assert
        await Should.ThrowAsync<ValidationException>(() => sut.UpdateOrderStatus(orderId, request));

        updateOrderValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<(Order order, OrderStatus newStatus)>()), Times.Never);
        orderRepositoryMock.Verify(repo => repo.UpdateOrder(It.IsAny<Order>()), Times.Never);
    }

    private static ShoppingCart BuildShoppingCart(Guid clientId)
    {
        return ShoppingCart.Rehydrate(
            Guid.NewGuid(),
            clientId,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddMinutes(-1),
            [
                new ShoppingCartLine(Guid.NewGuid(), 1),
                new ShoppingCartLine(Guid.NewGuid(), 2)
            ]);
    }

    private static IReadOnlyCollection<Order> BuildOrders(Guid clientId)
    {
        return
        [
            BuildOrder(Guid.NewGuid(), clientId, OrderStatus.Created),
            BuildOrder(Guid.NewGuid(), clientId, OrderStatus.Paid)
        ];
    }

    private static Order BuildOrder(Guid orderId, Guid clientId, OrderStatus status)
    {
        var line = new OrderLine(Guid.NewGuid(), 2);
        return Order.Rehydrate(
            orderId,
            clientId,
            [line],
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddHours(-1),
            status);
    }

    private static IReadOnlyCollection<ProductVersion> BuildProductVersions(Order order)
    {
        return order.Lines
            .Select(line => ProductVersion.Rehydrate(
                line.ProductVersionId,
                true,
                DateTime.UtcNow.AddDays(-1),
                null,
                Guid.NewGuid(),
                new Money(10m, "USD"),
                "Test product",
                "Test brand"))
            .ToList();
    }
}
