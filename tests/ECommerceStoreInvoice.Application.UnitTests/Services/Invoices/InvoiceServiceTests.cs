using Microsoft.Extensions.Logging;
using ECommerceStoreInvoice.Application.Services.Abstract.ClientDataVersions;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using ECommerceStoreInvoice.Application.Services.Concrete.Invoices;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
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
        var generationRepositoryMock = new Mock<IInvoiceGenerationRepository>(MockBehavior.Strict);
        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var clientDataVersionServiceMock = new Mock<IClientDataVersionService>(MockBehavior.Strict);
        var invoicePdfServiceMock = new Mock<IInvoicePdfService>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var invoiceStatusValidationPolicyMock = new Mock<IValidationPolicy<InvoiceOrderStatusValidationContext>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<InvoiceService>>(MockBehavior.Loose);

        guidValidationPolicyMock
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(invalidResult);

        var sut = new InvoiceService(
            invoiceRepositoryMock.Object,
            generationRepositoryMock.Object,
            orderRepositoryMock.Object,
            clientDataVersionServiceMock.Object,
            invoicePdfServiceMock.Object,
            guidValidationPolicyMock.Object,
            invoiceStatusValidationPolicyMock.Object,
            loggerMock.Object);

        await Should.ThrowAsync<ValidationException>(() => sut.CreateInvoiceForOrder(clientId, orderId));

        guidValidationPolicyMock.Verify(policy => policy.Validate(clientId), Times.Once);
        guidValidationPolicyMock.Verify(policy => policy.Validate(orderId), Times.Never);
        orderRepositoryMock.Verify(repo => repo.GetOrderWithProductVersionsById(It.IsAny<Guid>()), Times.Never);
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
        var generationRepositoryMock = new Mock<IInvoiceGenerationRepository>(MockBehavior.Strict);
        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var clientDataVersionServiceMock = new Mock<IClientDataVersionService>(MockBehavior.Strict);
        var invoicePdfServiceMock = new Mock<IInvoicePdfService>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var invoiceStatusValidationPolicyMock = new Mock<IValidationPolicy<InvoiceOrderStatusValidationContext>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<InvoiceService>>(MockBehavior.Loose);

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
            generationRepositoryMock.Object,
            orderRepositoryMock.Object,
            clientDataVersionServiceMock.Object,
            invoicePdfServiceMock.Object,
            guidValidationPolicyMock.Object,
            invoiceStatusValidationPolicyMock.Object,
            loggerMock.Object);

        await Should.ThrowAsync<ValidationException>(() => sut.CreateInvoiceForOrder(clientId, orderId));

        guidValidationPolicyMock.Verify(policy => policy.Validate(clientId), Times.Once);
        guidValidationPolicyMock.Verify(policy => policy.Validate(orderId), Times.Once);
        orderRepositoryMock.Verify(repo => repo.GetOrderWithProductVersionsById(It.IsAny<Guid>()), Times.Never);
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
        var generationRepositoryMock = new Mock<IInvoiceGenerationRepository>(MockBehavior.Strict);
        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var clientDataVersionServiceMock = new Mock<IClientDataVersionService>(MockBehavior.Strict);
        var invoicePdfServiceMock = new Mock<IInvoicePdfService>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var invoiceStatusValidationPolicyMock = new Mock<IValidationPolicy<InvoiceOrderStatusValidationContext>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<InvoiceService>>(MockBehavior.Loose);

        guidValidationPolicyMock
            .Setup(policy => policy.Validate(invoiceId))
            .ReturnsAsync(invalidResult);

        var sut = new InvoiceService(
            invoiceRepositoryMock.Object,
            generationRepositoryMock.Object,
            orderRepositoryMock.Object,
            clientDataVersionServiceMock.Object,
            invoicePdfServiceMock.Object,
            guidValidationPolicyMock.Object,
            invoiceStatusValidationPolicyMock.Object,
            loggerMock.Object);

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
            OrderStatus.Paid);

        IReadOnlyCollection<ProductVersion> productVersions = [];

        var existingInvoice = Invoice.Rehydrate(
            Guid.NewGuid(),
            orderId,
            Guid.NewGuid(),
            "file:///invoices/existing.pdf",
            DateTime.UtcNow.AddMinutes(-5));

        var invoiceRepositoryMock = new Mock<IInvoiceRepository>(MockBehavior.Strict);
        var generationRepositoryMock = new Mock<IInvoiceGenerationRepository>(MockBehavior.Strict);
        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var clientDataVersionServiceMock = new Mock<IClientDataVersionService>(MockBehavior.Strict);
        var invoicePdfServiceMock = new Mock<IInvoicePdfService>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var invoiceStatusValidationPolicyMock = new Mock<IValidationPolicy<InvoiceOrderStatusValidationContext>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<InvoiceService>>(MockBehavior.Loose);

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
            .Setup(repo => repo.GetOrderWithProductVersionsById(orderId))
            .ReturnsAsync((existingOrder, productVersions));

        invoiceRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetInvoiceByOrderId(orderId))
            .ReturnsAsync(existingInvoice);

        var sut = new InvoiceService(
            invoiceRepositoryMock.Object,
            generationRepositoryMock.Object,
            orderRepositoryMock.Object,
            clientDataVersionServiceMock.Object,
            invoicePdfServiceMock.Object,
            guidValidationPolicyMock.Object,
            invoiceStatusValidationPolicyMock.Object,
            loggerMock.Object);

        await Should.ThrowAsync<ResourceAlreadyExistsException>(() => sut.CreateInvoiceForOrder(clientId, orderId));

        guidValidationPolicyMock.Verify(policy => policy.Validate(clientId), Times.Once);
        guidValidationPolicyMock.Verify(policy => policy.Validate(orderId), Times.Once);
        orderRepositoryMock.Verify(repo => repo.GetOrderWithProductVersionsById(orderId), Times.Once);
        invoiceRepositoryMock.Verify(repo => repo.GetInvoiceByOrderId(orderId), Times.Once);
        invoiceStatusValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<InvoiceOrderStatusValidationContext>()), Times.Never);
        clientDataVersionServiceMock.Verify(service => service.GetByClientId(It.IsAny<Guid>()), Times.Never);
        invoicePdfServiceMock.Verify(service => service.GenerateInvoicePdf(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Order>(), It.IsAny<IReadOnlyCollection<ProductVersion>>(), It.IsAny<ClientDataVersionResponseDto?>()), Times.Never);
        generationRepositoryMock.Verify(repo => repo.TryClaimAsync(It.IsAny<Invoice>(), It.IsAny<Guid>()), Times.Never);
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
            OrderStatus.Paid);

        IReadOnlyCollection<ProductVersion> productVersions = [];

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
        var generationRepositoryMock = new Mock<IInvoiceGenerationRepository>(MockBehavior.Strict);
        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var clientDataVersionServiceMock = new Mock<IClientDataVersionService>(MockBehavior.Strict);
        var invoicePdfServiceMock = new Mock<IInvoicePdfService>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var invoiceStatusValidationPolicyMock = new Mock<IValidationPolicy<InvoiceOrderStatusValidationContext>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<InvoiceService>>(MockBehavior.Loose);

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
            .Setup(repo => repo.GetOrderWithProductVersionsById(orderId))
            .ReturnsAsync((order, productVersions));

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

        generationRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.TryClaimAsync(It.Is<Invoice>(invoice => invoice.OrderId == orderId && invoice.ClientDataVersionId == clientDataVersionId), It.IsAny<Guid>()))
            .ReturnsAsync((Invoice invoice, Guid attemptId) => new InvoiceGenerationClaim(invoice.Id, orderId, clientDataVersionId, attemptId));

        invoicePdfServiceMock
            .InSequence(sequence)
            .Setup(service => service.GenerateInvoicePdf(It.IsAny<Guid>(), It.IsAny<Guid>(), order, productVersions, clientDataVersion))
            .ReturnsAsync(storageUrl);

        generationRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.CompleteAsync(It.IsAny<InvoiceGenerationClaim>(), storageUrl))
            .ReturnsAsync(createdInvoice);

        var sut = new InvoiceService(
            invoiceRepositoryMock.Object,
            generationRepositoryMock.Object,
            orderRepositoryMock.Object,
            clientDataVersionServiceMock.Object,
            invoicePdfServiceMock.Object,
            guidValidationPolicyMock.Object,
            invoiceStatusValidationPolicyMock.Object,
            loggerMock.Object);

        var response = await sut.CreateInvoiceForOrder(clientId, orderId);

        guidValidationPolicyMock.Verify(policy => policy.Validate(clientId), Times.Once);
        guidValidationPolicyMock.Verify(policy => policy.Validate(orderId), Times.Once);
        orderRepositoryMock.Verify(repo => repo.GetOrderWithProductVersionsById(orderId), Times.Once);
        invoiceRepositoryMock.Verify(repo => repo.GetInvoiceByOrderId(orderId), Times.Once);
        invoiceStatusValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<InvoiceOrderStatusValidationContext>()), Times.Once);
        clientDataVersionServiceMock.Verify(service => service.GetByClientId(clientId), Times.Once);
        invoicePdfServiceMock.Verify(service => service.GenerateInvoicePdf(It.IsAny<Guid>(), It.IsAny<Guid>(), order, productVersions, clientDataVersion), Times.Once);
        generationRepositoryMock.Verify(repo => repo.CompleteAsync(It.IsAny<InvoiceGenerationClaim>(), storageUrl), Times.Once);

        response.ShouldNotBeNull();
        response.Id.ShouldBe(createdInvoice.Id);
        response.OrderId.ShouldBe(orderId);
        response.ClietDataVersionId.ShouldBe(clientDataVersionId);
        response.StorageUrl.ShouldBe(storageUrl);
        response.CreatedAt.ShouldBe(createdInvoice.CreatedAt);
    }

    [Fact]
    public async Task CreateInvoiceForOrder_WhenGenerationFails_ShouldReleaseClaimAndDeleteAttemptPdf()
    {
        var clientId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var order = Order.Rehydrate(orderId, clientId, [], DateTime.UtcNow, DateTime.UtcNow, OrderStatus.Paid);
        var clientData = new ClientDataVersionResponseDto
        {
            Id = Guid.NewGuid(), ClientId = clientId, ClientName = "Test Client", PostalCode = "00-000",
            City = "Warsaw", Street = "Main", BuildingNumber = "1", ApartmentNumber = "",
            PhoneNumber = "123456789", PhonePrefix = "+48", AddressEmail = "test@example.com",
            CreatedAt = DateTime.UtcNow
        };
        var invoiceRepositoryMock = new Mock<IInvoiceRepository>(MockBehavior.Strict);
        var generationRepositoryMock = new Mock<IInvoiceGenerationRepository>(MockBehavior.Strict);
        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var clientServiceMock = new Mock<IClientDataVersionService>(MockBehavior.Strict);
        var pdfServiceMock = new Mock<IInvoicePdfService>(MockBehavior.Strict);
        var guidPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var statusPolicyMock = new Mock<IValidationPolicy<InvoiceOrderStatusValidationContext>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<InvoiceService>>(MockBehavior.Loose);
        InvoiceGenerationClaim? claim = null;

        guidPolicyMock.Setup(x => x.Validate(It.IsAny<Guid>())).ReturnsAsync(new ValidationResult());
        orderRepositoryMock.Setup(x => x.GetOrderWithProductVersionsById(orderId))
            .ReturnsAsync((order, (IReadOnlyCollection<ProductVersion>)[]));
        invoiceRepositoryMock.Setup(x => x.GetInvoiceByOrderId(orderId)).ReturnsAsync((Invoice?)null);
        statusPolicyMock.Setup(x => x.Validate(It.IsAny<InvoiceOrderStatusValidationContext>()))
            .ReturnsAsync(new ValidationResult());
        clientServiceMock.Setup(x => x.GetByClientId(clientId)).ReturnsAsync(clientData);
        generationRepositoryMock.Setup(x => x.TryClaimAsync(It.IsAny<Invoice>(), It.IsAny<Guid>()))
            .ReturnsAsync((Invoice invoice, Guid attemptId) => claim = new InvoiceGenerationClaim(invoice.Id, orderId, clientData.Id, attemptId));
        pdfServiceMock.Setup(x => x.GenerateInvoicePdf(It.IsAny<Guid>(), It.IsAny<Guid>(), order,
                It.IsAny<IReadOnlyCollection<ProductVersion>>(), clientData))
            .ThrowsAsync(new IOException("PDF failed"));
        generationRepositoryMock.Setup(x => x.ReleaseAsync(It.IsAny<InvoiceGenerationClaim>())).Returns(Task.CompletedTask);
        pdfServiceMock.Setup(x => x.DeleteGeneratedPdf(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.CompletedTask);

        var sut = new InvoiceService(invoiceRepositoryMock.Object, generationRepositoryMock.Object,
            orderRepositoryMock.Object, clientServiceMock.Object, pdfServiceMock.Object,
            guidPolicyMock.Object, statusPolicyMock.Object, loggerMock.Object);

        await Should.ThrowAsync<IOException>(() => sut.CreateInvoiceForOrder(clientId, orderId));
        claim.ShouldNotBeNull();
        generationRepositoryMock.Verify(x => x.ReleaseAsync(claim), Times.Once);
        pdfServiceMock.Verify(x => x.DeleteGeneratedPdf(claim.InvoiceId, claim.AttemptId), Times.Once);
        generationRepositoryMock.Verify(x => x.CompleteAsync(It.IsAny<InvoiceGenerationClaim>(), It.IsAny<string>()), Times.Never);
    }
    [Fact]
    public async Task CreateInvoiceForOrder_WhenCompletionCommitsButAcknowledgementIsLost_ShouldKeepPdf()
    {
        var clientId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var order = Order.Rehydrate(orderId, clientId, [], DateTime.UtcNow, null, OrderStatus.Paid);
        var clientData = new ClientDataVersionResponseDto
        {
            Id = Guid.NewGuid(), ClientId = clientId, ClientName = "Test Client", PostalCode = "00-000",
            City = "Warsaw", Street = "Main", BuildingNumber = "1", ApartmentNumber = "",
            PhoneNumber = "123456789", PhonePrefix = "+48", AddressEmail = "test@example.com",
            CreatedAt = DateTime.UtcNow
        };
        var claim = new InvoiceGenerationClaim(Guid.NewGuid(), orderId, clientData.Id, Guid.NewGuid());
        const string storageUrl = "file:///invoices/committed.pdf";
        var committed = false;

        var invoices = new Mock<IInvoiceRepository>(MockBehavior.Strict);
        invoices.Setup(x => x.GetInvoiceByOrderId(orderId)).ReturnsAsync((Invoice?)null);
        var generation = new Mock<IInvoiceGenerationRepository>(MockBehavior.Strict);
        generation.Setup(x => x.TryClaimAsync(It.IsAny<Invoice>(), It.IsAny<Guid>())).ReturnsAsync(claim);
        generation.Setup(x => x.CompleteAsync(claim, storageUrl)).Returns(() =>
        {
            committed = true;
            return Task.FromException<Invoice>(new IOException("Completion committed but the acknowledgement was lost."));
        });
        generation.Setup(x => x.ReleaseAsync(claim)).Returns(Task.CompletedTask);
        var orders = new Mock<IOrderRepository>(MockBehavior.Strict);
        orders.Setup(x => x.GetOrderWithProductVersionsById(orderId))
            .ReturnsAsync((order, (IReadOnlyCollection<ProductVersion>)[]));
        var clients = new Mock<IClientDataVersionService>(MockBehavior.Strict);
        clients.Setup(x => x.GetByClientId(clientId)).ReturnsAsync(clientData);
        var pdf = new Mock<IInvoicePdfService>(MockBehavior.Strict);
        pdf.Setup(x => x.GenerateInvoicePdf(claim.InvoiceId, claim.AttemptId, order,
            It.IsAny<IReadOnlyCollection<ProductVersion>>(), clientData)).ReturnsAsync(storageUrl);
        var guidPolicy = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        guidPolicy.Setup(x => x.Validate(It.IsAny<Guid>())).ReturnsAsync(new ValidationResult());
        var statusPolicy = new Mock<IValidationPolicy<InvoiceOrderStatusValidationContext>>(MockBehavior.Strict);
        statusPolicy.Setup(x => x.Validate(It.IsAny<InvoiceOrderStatusValidationContext>()))
            .ReturnsAsync(new ValidationResult());

        var sut = new InvoiceService(invoices.Object, generation.Object, orders.Object, clients.Object,
            pdf.Object, guidPolicy.Object, statusPolicy.Object, Mock.Of<ILogger<InvoiceService>>());

        await Should.ThrowAsync<IOException>(() => sut.CreateInvoiceForOrder(clientId, orderId));

        committed.ShouldBeTrue();
        generation.Verify(x => x.ReleaseAsync(claim), Times.Once);
        pdf.Verify(x => x.DeleteGeneratedPdf(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

}
