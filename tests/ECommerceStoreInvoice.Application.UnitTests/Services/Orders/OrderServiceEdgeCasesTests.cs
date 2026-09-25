using ECommerceStoreInvoice.Application.Common.RequestsDto.Orders;
using ECommerceStoreInvoice.Application.Services.Concrete.Orders;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.ExternalServices;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace ECommerceStoreInvoice.Application.UnitTests.Services.Orders;

public sealed class OrderServiceEdgeCasesTests
{
    [Fact]
    public async Task GetOrderByOrderId_WhenIdIsEmpty_DoesNotReadRepository()
    {
        var setup = new Setup();
        setup.RejectEmptyId();

        await Should.ThrowAsync<ValidationException>(() => setup.Service.GetOrderByOrderId(Guid.Empty));

        setup.Orders.Verify(x => x.GetOrderWithProductVersionsById(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrderStatus_WhenIdIsEmpty_DoesNotReadOrSaveOrder()
    {
        var setup = new Setup();
        setup.RejectEmptyId();

        await Should.ThrowAsync<ValidationException>(() => setup.Service.UpdateOrderStatus(
            Guid.Empty, new UpdateOrderStatusRequestDto { Status = "Paid" }));

        setup.Orders.Verify(x => x.GetOrderWithProductVersionsById(It.IsAny<Guid>()), Times.Never);
        setup.Orders.Verify(x => x.UpdateOrder(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrderStatus_WhenOrderDoesNotExist_DoesNotValidateTransitionOrSave()
    {
        var setup = new Setup();
        var orderId = Guid.NewGuid();
        setup.Orders.Setup(x => x.GetOrderWithProductVersionsById(orderId))
            .ReturnsAsync(((Order Order, IReadOnlyCollection<ProductVersion> ProductVersions)?)null);

        await Should.ThrowAsync<ResourceNotFoundException>(() => setup.Service.UpdateOrderStatus(
            orderId, new UpdateOrderStatusRequestDto { Status = "Paid" }));

        setup.UpdatePolicy.Verify(x => x.Validate(It.IsAny<(Order order, OrderStatus newStatus)>()), Times.Never);
        setup.Orders.Verify(x => x.UpdateOrder(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrderStatus_WhenCreatedBecomesCancelled_SavesStatusAndPreservesSnapshots()
    {
        var setup = new Setup();
        var orderId = Guid.NewGuid();
        var order = CreateOrder(orderId, OrderStatus.Created);
        var versions = CreateVersions(order);
        setup.Orders.Setup(x => x.GetOrderWithProductVersionsById(orderId))
            .ReturnsAsync((order, versions));
        setup.UpdatePolicy.Setup(x => x.Validate(It.Is<(Order order, OrderStatus newStatus)>(
                value => value.order == order && value.newStatus == OrderStatus.Cancelled)))
            .ReturnsAsync(new ValidationResult());
        setup.Orders.Setup(x => x.UpdateOrder(It.IsAny<Order>()))
            .ReturnsAsync((Order updated) => updated);

        var response = await setup.Service.UpdateOrderStatus(
            orderId, new UpdateOrderStatusRequestDto { Status = "Cancelled" });

        response.Status.ShouldBe("Cancelled");
        response.UpdatedAt.ShouldNotBeNull();
        response.Lines.Single().ProductVersion.Id.ShouldBe(versions.Single().Id);
        setup.Orders.Verify(x => x.UpdateOrder(It.Is<Order>(
            updated => updated.Id == orderId && updated.Status == OrderStatus.Cancelled)), Times.Once);
    }

    [Fact]
    public async Task UpdateOrderStatus_WhenPaidIsRequestedAgain_RejectsWithoutChangingOrder()
    {
        var setup = new Setup();
        var orderId = Guid.NewGuid();
        var order = CreateOrder(orderId, OrderStatus.Paid);
        var originalUpdatedAt = order.UpdatedAt;
        var versions = CreateVersions(order);
        setup.Orders.Setup(x => x.GetOrderWithProductVersionsById(orderId))
            .ReturnsAsync((order, versions));
        var invalid = new ValidationResult();
        invalid.AddValidationError(new ValidationError
        {
            Entity = nameof(Order),
            Name = nameof(Order.Status),
            Message = "Only transitions from Created to Paid or Cancelled are allowed."
        });
        setup.UpdatePolicy.Setup(x => x.Validate(It.Is<(Order order, OrderStatus newStatus)>(
                value => value.order == order && value.newStatus == OrderStatus.Paid)))
            .ReturnsAsync(invalid);

        await Should.ThrowAsync<ValidationException>(() => setup.Service.UpdateOrderStatus(
            orderId, new UpdateOrderStatusRequestDto { Status = "Paid" }));

        order.Status.ShouldBe(OrderStatus.Paid);
        order.UpdatedAt.ShouldBe(originalUpdatedAt);
        setup.Orders.Verify(x => x.UpdateOrder(It.IsAny<Order>()), Times.Never);
    }

    private static Order CreateOrder(Guid id, OrderStatus status)
    {
        var updatedAt = status == OrderStatus.Created ? (DateTime?)null : DateTime.UtcNow.AddHours(-1);
        return Order.Rehydrate(
            id, Guid.NewGuid(), [new OrderLine(Guid.NewGuid(), 2)],
            DateTime.UtcNow.AddDays(-1), updatedAt, status);
    }

    private static IReadOnlyCollection<ProductVersion> CreateVersions(Order order) =>
        order.Lines.Select(line => ProductVersion.Rehydrate(
            line.ProductVersionId, true, DateTime.UtcNow.AddDays(-1), null,
            Guid.NewGuid(), new Money(10m, "USD"), "Phone", "Brand")).ToList();

    private sealed class Setup
    {
        public Mock<IOrderRepository> Orders { get; } = new();
        public Mock<IValidationPolicy<Guid>> GuidPolicy { get; } = new();
        public Mock<IValidationPolicy<(Order order, OrderStatus newStatus)>> UpdatePolicy { get; } = new();
        public OrderService Service { get; }

        public Setup()
        {
            GuidPolicy.Setup(x => x.Validate(It.IsAny<Guid>())).ReturnsAsync(new ValidationResult());
            Service = new OrderService(
                Orders.Object,
                new Mock<IOrderWriteTransaction>().Object,
                new Mock<IProductVersionRepository>().Object,
                new Mock<IShoppingCartRepository>().Object,
                GuidPolicy.Object,
                new Mock<IValidationPolicy<Order>>().Object,
                UpdatePolicy.Object,
                Mock.Of<ILogger<OrderService>>(),
                new Mock<IProductServiceClient>().Object,
                new Mock<IValidationPolicy<ProductVersion>>().Object);
        }

        public void RejectEmptyId()
        {
            var invalid = new ValidationResult();
            invalid.AddValidationError(new ValidationError
            {
                Entity = nameof(Guid),
                Name = "Id",
                Message = "Id cannot be empty"
            });
            GuidPolicy.Setup(x => x.Validate(Guid.Empty)).ReturnsAsync(invalid);
        }
    }
}
