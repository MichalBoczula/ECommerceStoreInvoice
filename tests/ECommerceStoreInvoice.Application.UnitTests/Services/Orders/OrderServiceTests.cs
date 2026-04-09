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
using Moq;
using Shouldly;

namespace ECommerceStoreInvoice.Application.UnitTests.Services.Orders;

public sealed class OrderServiceTests
{
    [Fact]
    public async Task CreateOrder_WhenRequestIsValid_ShouldValidatePersistClearCartAndReturnResponse()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var shoppingCart = BuildShoppingCart(clientId);
        var guidValidationResult = new ValidationResult();
        var orderValidationResult = new ValidationResult();

        ProductVersion CreateProductVersionFromLine(ShoppingCartLine line) =>
            ProductVersion.Rehydrate(
                Guid.NewGuid(),
                true,
                DateTime.UtcNow,
                null,
                line.ProductId,
                line.UnitPrice,
                line.Name,
                line.Brand);

        var createdProductVersions = shoppingCart.Lines
            .Select(CreateProductVersionFromLine)
            .ToList();

        var createdOrder = Order.Rehydrate(
            Guid.NewGuid(),
            clientId,
            [
                .. createdProductVersions.Zip(shoppingCart.Lines, (productVersion, line) =>
                    new OrderLine(productVersion.Id, line.Name, line.Brand, line.UnitPrice, line.Quantity))
            ],
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow,
            OrderStatus.Created,
            new Money(shoppingCart.Total.Amount, shoppingCart.Total.Currency));

        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var productVersionRepositoryMock = new Mock<IProductVersionRepository>(MockBehavior.Strict);
        var shoppingCartRepositoryMock = new Mock<IShoppingCartRepository>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var orderValidationPolicyMock = new Mock<IValidationPolicy<Order>>(MockBehavior.Strict);
        var updateOrderValidationPolicyMock = new Mock<IValidationPolicy<(Order order, OrderStatus newStatus)>>(MockBehavior.Strict);

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
                .InSequence(sequence)
                .Setup(repo => repo.CreateProductVersion(It.Is<ProductVersion>(pv =>
                    pv.ProductId == line.ProductId &&
                    pv.Name == line.Name &&
                    pv.Brand == line.Brand &&
                    pv.Price.Amount == line.UnitPrice.Amount &&
                    pv.Price.Currency == line.UnitPrice.Currency)))
                .ReturnsAsync(productVersion);
        }

        orderValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(It.Is<Order>(order =>
                order.ClientId == clientId &&
                order.Lines.Count == shoppingCart.Lines.Count &&
                order.Total.Amount == shoppingCart.Total.Amount &&
                order.Total.Currency == shoppingCart.Total.Currency)))
            .ReturnsAsync(orderValidationResult);

        orderRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.CreateOrder(It.IsAny<Order>()))
            .ReturnsAsync(createdOrder);

        shoppingCartRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.UpdateShoppingCart(It.Is<ShoppingCart>(cart =>
                cart.ClientId == clientId &&
                cart.Lines.Count == 0 &&
                cart.Total.Amount == 0m &&
                cart.Total.Currency == shoppingCart.Total.Currency)))
            .ReturnsAsync((ShoppingCart cart) => cart);

        var sut = new OrderService(
            orderRepositoryMock.Object,
            productVersionRepositoryMock.Object,
            shoppingCartRepositoryMock.Object,
            guidValidationPolicyMock.Object,
            orderValidationPolicyMock.Object,
            updateOrderValidationPolicyMock.Object);

        // Act
        var response = await sut.CreateOrder(clientId);

        // Assert
        guidValidationPolicyMock.Verify(policy => policy.Validate(clientId), Times.Once);
        shoppingCartRepositoryMock.Verify(repo => repo.GetShoppingCartByClientId(clientId), Times.Once);
        productVersionRepositoryMock.Verify(repo => repo.CreateProductVersion(It.IsAny<ProductVersion>()), Times.Exactly(shoppingCart.Lines.Count));
        orderValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<Order>()), Times.Once);
        orderRepositoryMock.Verify(repo => repo.CreateOrder(It.IsAny<Order>()), Times.Once);
        shoppingCartRepositoryMock.Verify(repo => repo.UpdateShoppingCart(It.IsAny<ShoppingCart>()), Times.Once);
        updateOrderValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<(Order order, OrderStatus newStatus)>()), Times.Never);

        response.ShouldNotBeNull();
        response.Id.ShouldBe(createdOrder.Id);
        response.ClientId.ShouldBe(clientId);
        response.Status.ShouldBe(OrderStatus.Created.ToString());
        response.TotalAmount.ShouldBe(createdOrder.Total.Amount);
        response.TotalCurrency.ShouldBe(createdOrder.Total.Currency);
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

        guidValidationPolicyMock
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(invalidResult);

        var sut = new OrderService(
            orderRepositoryMock.Object,
            productVersionRepositoryMock.Object,
            shoppingCartRepositoryMock.Object,
            guidValidationPolicyMock.Object,
            orderValidationPolicyMock.Object,
            updateOrderValidationPolicyMock.Object);

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
            updateOrderValidationPolicyMock.Object);

        // Act / Assert
        await Should.ThrowAsync<ResourceNotFoundException>(() => sut.CreateOrder(clientId));

        productVersionRepositoryMock.Verify(repo => repo.CreateProductVersion(It.IsAny<ProductVersion>()), Times.Never);
        orderValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<Order>()), Times.Never);
        orderRepositoryMock.Verify(repo => repo.CreateOrder(It.IsAny<Order>()), Times.Never);
        shoppingCartRepositoryMock.Verify(repo => repo.UpdateShoppingCart(It.IsAny<ShoppingCart>()), Times.Never);
    }

    [Fact]
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
                .InSequence(sequence)
                .Setup(repo => repo.CreateProductVersion(It.Is<ProductVersion>(pv => pv.ProductId == line.ProductId)))
                .ReturnsAsync(new ProductVersion(line.ProductId, line.UnitPrice, line.Name, line.Brand));
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
            updateOrderValidationPolicyMock.Object);

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

        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var productVersionRepositoryMock = new Mock<IProductVersionRepository>(MockBehavior.Strict);
        var shoppingCartRepositoryMock = new Mock<IShoppingCartRepository>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var orderValidationPolicyMock = new Mock<IValidationPolicy<Order>>(MockBehavior.Strict);
        var updateOrderValidationPolicyMock = new Mock<IValidationPolicy<(Order order, OrderStatus newStatus)>>(MockBehavior.Strict);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(guidValidationResult);

        orderRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetOrdersByClientId(clientId))
            .ReturnsAsync(orders);

        var sut = new OrderService(
            orderRepositoryMock.Object,
            productVersionRepositoryMock.Object,
            shoppingCartRepositoryMock.Object,
            guidValidationPolicyMock.Object,
            orderValidationPolicyMock.Object,
            updateOrderValidationPolicyMock.Object);

        // Act
        var response = await sut.GetOrdersByClientId(clientId);

        // Assert
        response.Count.ShouldBe(orders.Count);
        response.Select(x => x.Id).ShouldBe(orders.Select(x => x.Id), ignoreOrder: true);

        guidValidationPolicyMock.Verify(policy => policy.Validate(clientId), Times.Once);
        orderRepositoryMock.Verify(repo => repo.GetOrdersByClientId(clientId), Times.Once);
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

        guidValidationPolicyMock
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(invalidResult);

        var sut = new OrderService(
            orderRepositoryMock.Object,
            productVersionRepositoryMock.Object,
            shoppingCartRepositoryMock.Object,
            guidValidationPolicyMock.Object,
            orderValidationPolicyMock.Object,
            updateOrderValidationPolicyMock.Object);

        // Act / Assert
        await Should.ThrowAsync<ValidationException>(() => sut.GetOrdersByClientId(clientId));

        orderRepositoryMock.Verify(repo => repo.GetOrdersByClientId(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetOrderByOrderId_WhenOrderIdIsValidAndEntityExists_ShouldReturnResponse()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var guidValidationResult = new ValidationResult();
        var order = BuildOrder(orderId, clientId, OrderStatus.Paid);

        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var productVersionRepositoryMock = new Mock<IProductVersionRepository>(MockBehavior.Strict);
        var shoppingCartRepositoryMock = new Mock<IShoppingCartRepository>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var orderValidationPolicyMock = new Mock<IValidationPolicy<Order>>(MockBehavior.Strict);
        var updateOrderValidationPolicyMock = new Mock<IValidationPolicy<(Order order, OrderStatus newStatus)>>(MockBehavior.Strict);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(orderId))
            .ReturnsAsync(guidValidationResult);

        orderRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetOrderByOrderId(orderId))
            .ReturnsAsync(order);

        var sut = new OrderService(
            orderRepositoryMock.Object,
            productVersionRepositoryMock.Object,
            shoppingCartRepositoryMock.Object,
            guidValidationPolicyMock.Object,
            orderValidationPolicyMock.Object,
            updateOrderValidationPolicyMock.Object);

        // Act
        var response = await sut.GetOrderByOrderId(orderId);

        // Assert
        response.Id.ShouldBe(order.Id);
        response.ClientId.ShouldBe(clientId);
        response.Status.ShouldBe(OrderStatus.Paid.ToString());

        guidValidationPolicyMock.Verify(policy => policy.Validate(orderId), Times.Once);
        orderRepositoryMock.Verify(repo => repo.GetOrderByOrderId(orderId), Times.Once);
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

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(orderId))
            .ReturnsAsync(guidValidationResult);

        orderRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetOrderByOrderId(orderId))
            .ReturnsAsync((Order?)null);

        var sut = new OrderService(
            orderRepositoryMock.Object,
            productVersionRepositoryMock.Object,
            shoppingCartRepositoryMock.Object,
            guidValidationPolicyMock.Object,
            orderValidationPolicyMock.Object,
            updateOrderValidationPolicyMock.Object);

        // Act / Assert
        await Should.ThrowAsync<ResourceNotFoundException>(() => sut.GetOrderByOrderId(orderId));

        orderRepositoryMock.Verify(repo => repo.GetOrderByOrderId(orderId), Times.Once);
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

        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var productVersionRepositoryMock = new Mock<IProductVersionRepository>(MockBehavior.Strict);
        var shoppingCartRepositoryMock = new Mock<IShoppingCartRepository>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var orderValidationPolicyMock = new Mock<IValidationPolicy<Order>>(MockBehavior.Strict);
        var updateOrderValidationPolicyMock = new Mock<IValidationPolicy<(Order order, OrderStatus newStatus)>>(MockBehavior.Strict);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(orderId))
            .ReturnsAsync(guidValidationResult);

        orderRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetOrderByOrderId(orderId))
            .ReturnsAsync(existingOrder);

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
            updateOrderValidationPolicyMock.Object);

        // Act
        var response = await sut.UpdateOrderStatus(orderId, request);

        // Assert
        guidValidationPolicyMock.Verify(policy => policy.Validate(orderId), Times.Once);
        orderRepositoryMock.Verify(repo => repo.GetOrderByOrderId(orderId), Times.Once);
        updateOrderValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<(Order order, OrderStatus newStatus)>()), Times.Once);
        orderRepositoryMock.Verify(repo => repo.UpdateOrder(It.IsAny<Order>()), Times.Once);
        orderValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<Order>()), Times.Never);

        response.Id.ShouldBe(orderId);
        response.Status.ShouldBe(OrderStatus.Paid.ToString());
    }

    [Fact]
    public async Task UpdateOrderStatus_WhenStatusTransitionValidationFails_ShouldThrowValidationExceptionAndNotPersist()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var request = new UpdateOrderStatusRequestDto { Status = "Cancelled" };
        var existingOrder = BuildOrder(orderId, Guid.NewGuid(), OrderStatus.Paid);

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

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(orderId))
            .ReturnsAsync(guidValidationResult);

        orderRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetOrderByOrderId(orderId))
            .ReturnsAsync(existingOrder);

        updateOrderValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate((existingOrder, OrderStatus.Cancelled)))
            .ReturnsAsync(invalidTransitionResult);

        var sut = new OrderService(
            orderRepositoryMock.Object,
            productVersionRepositoryMock.Object,
            shoppingCartRepositoryMock.Object,
            guidValidationPolicyMock.Object,
            orderValidationPolicyMock.Object,
            updateOrderValidationPolicyMock.Object);

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
        var guidValidationResult = new ValidationResult();

        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var productVersionRepositoryMock = new Mock<IProductVersionRepository>(MockBehavior.Strict);
        var shoppingCartRepositoryMock = new Mock<IShoppingCartRepository>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var orderValidationPolicyMock = new Mock<IValidationPolicy<Order>>(MockBehavior.Strict);
        var updateOrderValidationPolicyMock = new Mock<IValidationPolicy<(Order order, OrderStatus newStatus)>>(MockBehavior.Strict);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(orderId))
            .ReturnsAsync(guidValidationResult);

        orderRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetOrderByOrderId(orderId))
            .ReturnsAsync(existingOrder);

        var sut = new OrderService(
            orderRepositoryMock.Object,
            productVersionRepositoryMock.Object,
            shoppingCartRepositoryMock.Object,
            guidValidationPolicyMock.Object,
            orderValidationPolicyMock.Object,
            updateOrderValidationPolicyMock.Object);

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
                new ShoppingCartLine(Guid.NewGuid(), "Phone", "Apple", new Money(1999.99m, "USD"), 1),
                new ShoppingCartLine(Guid.NewGuid(), "Watch", "Apple", new Money(799.00m, "USD"), 2)
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
        var line = new OrderLine(Guid.NewGuid(), "Phone", "Apple", new Money(100m, "USD"), 2);
        return Order.Rehydrate(
            orderId,
            clientId,
            [line],
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddHours(-1),
            status,
            new Money(line.Total.Amount, line.Total.Currency));
    }
}
