using ECommerceStoreInvoice.Application.Services.Abstract.ClientDataVersions;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using ECommerceStoreInvoice.Application.Services.Concrete.Invoices;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
using Moq;
using Shouldly;

namespace ECommerceStoreInvoice.Application.UnitTests.Services.Invoices;

public sealed class InvoiceServiceTests
{
    [Fact]
    public async Task CreateInvoiceForOrder_WhenClientIdValidationFails_ShouldThrowValidationExceptionAndStopFlow()
    {
        var clientId = Guid.Empty;
        var orderId = Guid.NewGuid();

        var invalidResult = new ValidationResult();
        invalidResult.AddValidationError(new ValidationError
        {
            Entity = nameof(Guid),
            Name = "clientId",
            Message = "ClientId cannot be empty"
        });

        var invoiceRepositoryMock = new Mock<IInvoiceRepository>(MockBehavior.Strict);
        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var clientDataVersionServiceMock = new Mock<IClientDataVersionService>(MockBehavior.Strict);
        var invoicePdfServiceMock = new Mock<IInvoicePdfService>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var invoiceStatusValidationPolicyMock = new Mock<IValidationPolicy<InvoiceOrderStatusValidationContext>>(MockBehavior.Strict);

        guidValidationPolicyMock
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(invalidResult);

        var sut = new InvoiceService(
            invoiceRepositoryMock.Object,
            orderRepositoryMock.Object,
            clientDataVersionServiceMock.Object,
            invoicePdfServiceMock.Object,
            guidValidationPolicyMock.Object,
            invoiceStatusValidationPolicyMock.Object);

        await Should.ThrowAsync<ValidationException>(() => sut.CreateInvoiceForOrder(clientId, orderId));

        guidValidationPolicyMock.Verify(policy => policy.Validate(clientId), Times.Once);
        guidValidationPolicyMock.Verify(policy => policy.Validate(orderId), Times.Never);
        orderRepositoryMock.Verify(repo => repo.GetOrderByOrderId(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CreateInvoiceForOrder_WhenOrderIdValidationFails_ShouldThrowValidationExceptionAndStopFlow()
    {
        var clientId = Guid.NewGuid();
        var orderId = Guid.Empty;

        var validResult = new ValidationResult();
        var invalidResult = new ValidationResult();
        invalidResult.AddValidationError(new ValidationError
        {
            Entity = nameof(Guid),
            Name = "orderId",
            Message = "OrderId cannot be empty"
        });

        var invoiceRepositoryMock = new Mock<IInvoiceRepository>(MockBehavior.Strict);
        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var clientDataVersionServiceMock = new Mock<IClientDataVersionService>(MockBehavior.Strict);
        var invoicePdfServiceMock = new Mock<IInvoicePdfService>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var invoiceStatusValidationPolicyMock = new Mock<IValidationPolicy<InvoiceOrderStatusValidationContext>>(MockBehavior.Strict);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(validResult);

        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(orderId))
            .ReturnsAsync(invalidResult);

        var sut = new InvoiceService(
            invoiceRepositoryMock.Object,
            orderRepositoryMock.Object,
            clientDataVersionServiceMock.Object,
            invoicePdfServiceMock.Object,
            guidValidationPolicyMock.Object,
            invoiceStatusValidationPolicyMock.Object);

        await Should.ThrowAsync<ValidationException>(() => sut.CreateInvoiceForOrder(clientId, orderId));

        guidValidationPolicyMock.Verify(policy => policy.Validate(clientId), Times.Once);
        guidValidationPolicyMock.Verify(policy => policy.Validate(orderId), Times.Once);
        orderRepositoryMock.Verify(repo => repo.GetOrderByOrderId(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetInvoiceById_WhenInvoiceIdValidationFails_ShouldThrowValidationExceptionAndNotLoadInvoice()
    {
        var invoiceId = Guid.Empty;

        var invalidResult = new ValidationResult();
        invalidResult.AddValidationError(new ValidationError
        {
            Entity = nameof(Guid),
            Name = "invoiceId",
            Message = "InvoiceId cannot be empty"
        });

        var invoiceRepositoryMock = new Mock<IInvoiceRepository>(MockBehavior.Strict);
        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var clientDataVersionServiceMock = new Mock<IClientDataVersionService>(MockBehavior.Strict);
        var invoicePdfServiceMock = new Mock<IInvoicePdfService>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var invoiceStatusValidationPolicyMock = new Mock<IValidationPolicy<InvoiceOrderStatusValidationContext>>(MockBehavior.Strict);

        guidValidationPolicyMock
            .Setup(policy => policy.Validate(invoiceId))
            .ReturnsAsync(invalidResult);

        var sut = new InvoiceService(
            invoiceRepositoryMock.Object,
            orderRepositoryMock.Object,
            clientDataVersionServiceMock.Object,
            invoicePdfServiceMock.Object,
            guidValidationPolicyMock.Object,
            invoiceStatusValidationPolicyMock.Object);

        await Should.ThrowAsync<ValidationException>(() => sut.GetInvoiceById(invoiceId));

        guidValidationPolicyMock.Verify(policy => policy.Validate(invoiceId), Times.Once);
        invoiceRepositoryMock.Verify(repo => repo.GetInvoiceById(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CreateInvoiceForOrder_WhenInvoiceAlreadyExists_ShouldThrowResourceAlreadyExistsExceptionAndSkipPdfGeneration()
    {
        var clientId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var validResult = new ValidationResult();
        var existingOrder = Order.Rehydrate(
            orderId,
            clientId,
            [],
            DateTime.UtcNow.AddMinutes(-10),
            DateTime.UtcNow,
            OrderStatus.Paid,
            new Money(0m, "USD"));
        var existingInvoice = Invoice.Rehydrate(
            Guid.NewGuid(),
            orderId,
            Guid.NewGuid(),
            "file:///invoices/existing.pdf",
            DateTime.UtcNow.AddMinutes(-5));

        var invoiceRepositoryMock = new Mock<IInvoiceRepository>(MockBehavior.Strict);
        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var clientDataVersionServiceMock = new Mock<IClientDataVersionService>(MockBehavior.Strict);
        var invoicePdfServiceMock = new Mock<IInvoicePdfService>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var invoiceStatusValidationPolicyMock = new Mock<IValidationPolicy<InvoiceOrderStatusValidationContext>>(MockBehavior.Strict);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(validResult);

        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(orderId))
            .ReturnsAsync(validResult);

        orderRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetOrderByOrderId(orderId))
            .ReturnsAsync(existingOrder);

        invoiceRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetInvoiceByOrderId(orderId))
            .ReturnsAsync(existingInvoice);

        var sut = new InvoiceService(
            invoiceRepositoryMock.Object,
            orderRepositoryMock.Object,
            clientDataVersionServiceMock.Object,
            invoicePdfServiceMock.Object,
            guidValidationPolicyMock.Object,
            invoiceStatusValidationPolicyMock.Object);

        await Should.ThrowAsync<ResourceAlreadyExistsException>(() => sut.CreateInvoiceForOrder(clientId, orderId));

        guidValidationPolicyMock.Verify(policy => policy.Validate(clientId), Times.Once);
        guidValidationPolicyMock.Verify(policy => policy.Validate(orderId), Times.Once);
        orderRepositoryMock.Verify(repo => repo.GetOrderByOrderId(orderId), Times.Once);
        invoiceRepositoryMock.Verify(repo => repo.GetInvoiceByOrderId(orderId), Times.Once);
        invoiceStatusValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<InvoiceOrderStatusValidationContext>()), Times.Never);
        clientDataVersionServiceMock.Verify(service => service.GetByClientId(It.IsAny<Guid>()), Times.Never);
        invoicePdfServiceMock.Verify(service => service.GenerateInvoicePdf(It.IsAny<Order>(), It.IsAny<ClientDataVersionResponseDto?>()), Times.Never);
        invoiceRepositoryMock.Verify(repo => repo.CreateInvoice(It.IsAny<Invoice>()), Times.Never);
    }

    [Fact]
    public async Task CreateInvoiceForOrder_WhenRequestIsValid_ShouldCreateInvoiceAndReturnResponse()
    {
        var clientId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var clientDataVersionId = Guid.NewGuid();
        var storageUrl = "file:///invoices/generated.pdf";

        var guidValidationResult = new ValidationResult();
        var orderStatusValidationResult = new ValidationResult();
        var order = Order.Rehydrate(
            orderId,
            clientId,
            [],
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow,
            OrderStatus.Paid,
            new Money(0m, "USD"));
        var clientDataVersion = new ClientDataVersionResponseDto
        {
            Id = clientDataVersionId,
            ClientId = clientId,
            ClientName = "John Doe",
            PostalCode = "00-000",
            City = "Warsaw",
            Street = "Main",
            BuildingNumber = "1",
            ApartmentNumber = "2",
            PhoneNumber = "123456789",
            PhonePrefix = "+48",
            AddressEmail = "john@example.com",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var createdInvoice = Invoice.Rehydrate(
            Guid.NewGuid(),
            orderId,
            clientDataVersionId,
            storageUrl,
            DateTime.UtcNow);

        var invoiceRepositoryMock = new Mock<IInvoiceRepository>(MockBehavior.Strict);
        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var clientDataVersionServiceMock = new Mock<IClientDataVersionService>(MockBehavior.Strict);
        var invoicePdfServiceMock = new Mock<IInvoicePdfService>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var invoiceStatusValidationPolicyMock = new Mock<IValidationPolicy<InvoiceOrderStatusValidationContext>>(MockBehavior.Strict);

        var sequence = new MockSequence();

        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(guidValidationResult);

        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(orderId))
            .ReturnsAsync(guidValidationResult);

        orderRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetOrderByOrderId(orderId))
            .ReturnsAsync(order);

        invoiceRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetInvoiceByOrderId(orderId))
            .ReturnsAsync((Invoice?)null);

        invoiceStatusValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(It.Is<InvoiceOrderStatusValidationContext>(context => context.Order == order)))
            .ReturnsAsync(orderStatusValidationResult);

        clientDataVersionServiceMock
            .InSequence(sequence)
            .Setup(service => service.GetByClientId(clientId))
            .ReturnsAsync(clientDataVersion);

        invoicePdfServiceMock
            .InSequence(sequence)
            .Setup(service => service.GenerateInvoicePdf(order, clientDataVersion))
            .ReturnsAsync(storageUrl);

        invoiceRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.CreateInvoice(It.Is<Invoice>(invoice =>
                invoice.OrderId == orderId &&
                invoice.ClientDataVersionId == clientDataVersionId &&
                invoice.StorageUrl == storageUrl)))
            .ReturnsAsync(createdInvoice);

        var sut = new InvoiceService(
            invoiceRepositoryMock.Object,
            orderRepositoryMock.Object,
            clientDataVersionServiceMock.Object,
            invoicePdfServiceMock.Object,
            guidValidationPolicyMock.Object,
            invoiceStatusValidationPolicyMock.Object);

        var response = await sut.CreateInvoiceForOrder(clientId, orderId);

        guidValidationPolicyMock.Verify(policy => policy.Validate(clientId), Times.Once);
        guidValidationPolicyMock.Verify(policy => policy.Validate(orderId), Times.Once);
        orderRepositoryMock.Verify(repo => repo.GetOrderByOrderId(orderId), Times.Once);
        invoiceRepositoryMock.Verify(repo => repo.GetInvoiceByOrderId(orderId), Times.Once);
        invoiceStatusValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<InvoiceOrderStatusValidationContext>()), Times.Once);
        clientDataVersionServiceMock.Verify(service => service.GetByClientId(clientId), Times.Once);
        invoicePdfServiceMock.Verify(service => service.GenerateInvoicePdf(order, clientDataVersion), Times.Once);
        invoiceRepositoryMock.Verify(repo => repo.CreateInvoice(It.IsAny<Invoice>()), Times.Once);

        response.ShouldNotBeNull();
        response.Id.ShouldBe(createdInvoice.Id);
        response.OrderId.ShouldBe(orderId);
        response.ClietDataVersionId.ShouldBe(clientDataVersionId);
        response.StorageUrl.ShouldBe(storageUrl);
        response.CreatedAt.ShouldBe(createdInvoice.CreatedAt);
    }
}
