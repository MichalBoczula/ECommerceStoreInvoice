using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Services.Concrete.ShoppingCarts;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace ECommerceStoreInvoice.Application.UnitTests.Services.ShoppingCarts;

public sealed class ShoppingCartServiceTests
{
    [Fact]
    public async Task GetShoppingCartByClientId_WhenClientIdIsValidAndEntityExists_ShouldReturnResponse()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var validationResult = new ValidationResult();
        var shoppingCart = BuildShoppingCart(clientId);

        var shoppingCartRepositoryMock = new Mock<IShoppingCartRepository>(MockBehavior.Strict);
        var shoppingCartLineValidationPolicyMock = new Mock<IValidationPolicy<IReadOnlyCollection<ShoppingCartLine>>>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<ShoppingCartService>>(MockBehavior.Loose);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(validationResult);

        shoppingCartRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetShoppingCartByClientId(clientId))
            .ReturnsAsync(shoppingCart);

        var sut = new ShoppingCartService(
            shoppingCartRepositoryMock.Object,
            shoppingCartLineValidationPolicyMock.Object,
            guidValidationPolicyMock.Object,
            loggerMock.Object);

        // Act
        var response = await sut.GetShoppingCartByClientId(clientId);

        // Assert
        guidValidationPolicyMock.Verify(policy => policy.Validate(clientId), Times.Once);
        shoppingCartRepositoryMock.Verify(repo => repo.GetShoppingCartByClientId(clientId), Times.Once);
        shoppingCartLineValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<IReadOnlyCollection<ShoppingCartLine>>()), Times.Never);

        response.ShouldNotBeNull();
        response.Id.ShouldBe(shoppingCart.Id);
        response.ClientId.ShouldBe(shoppingCart.ClientId);
        response.Lines.Count.ShouldBe(shoppingCart.Lines.Count);
    }

    [Fact]
    public async Task GetShoppingCartByClientId_WhenClientIdValidationFails_ShouldThrowValidationExceptionAndNotLoadEntity()
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

        var shoppingCartRepositoryMock = new Mock<IShoppingCartRepository>(MockBehavior.Strict);
        var shoppingCartLineValidationPolicyMock = new Mock<IValidationPolicy<IReadOnlyCollection<ShoppingCartLine>>>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<ShoppingCartService>>(MockBehavior.Loose);

        guidValidationPolicyMock
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(invalidResult);

        var sut = new ShoppingCartService(
            shoppingCartRepositoryMock.Object,
            shoppingCartLineValidationPolicyMock.Object,
            guidValidationPolicyMock.Object,
            loggerMock.Object);

        // Act / Assert
        await Should.ThrowAsync<ValidationException>(() => sut.GetShoppingCartByClientId(clientId));

        shoppingCartRepositoryMock.Verify(repo => repo.GetShoppingCartByClientId(It.IsAny<Guid>()), Times.Never);
        shoppingCartLineValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<IReadOnlyCollection<ShoppingCartLine>>()), Times.Never);
    }

    [Fact]
    public async Task GetShoppingCartByClientId_WhenEntityDoesNotExist_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var validationResult = new ValidationResult();

        var shoppingCartRepositoryMock = new Mock<IShoppingCartRepository>(MockBehavior.Strict);
        var shoppingCartLineValidationPolicyMock = new Mock<IValidationPolicy<IReadOnlyCollection<ShoppingCartLine>>>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<ShoppingCartService>>(MockBehavior.Loose);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(validationResult);

        shoppingCartRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetShoppingCartByClientId(clientId))
            .ReturnsAsync((ShoppingCart?)null);

        var sut = new ShoppingCartService(
            shoppingCartRepositoryMock.Object,
            shoppingCartLineValidationPolicyMock.Object,
            guidValidationPolicyMock.Object,
            loggerMock.Object);

        // Act / Assert
        var ex = await Should.ThrowAsync<ResourceNotFoundException>(() => sut.GetShoppingCartByClientId(clientId));

        ex.ResourceId.ShouldBe(clientId);
        ex.ResourceType.ShouldBe(nameof(ShoppingCart));

        guidValidationPolicyMock.Verify(policy => policy.Validate(clientId), Times.Once);
        shoppingCartRepositoryMock.Verify(repo => repo.GetShoppingCartByClientId(clientId), Times.Once);
        shoppingCartLineValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<IReadOnlyCollection<ShoppingCartLine>>()), Times.Never);
    }

    [Fact]
    public async Task CreateShoppingCart_WhenClientIdIsValidAndCartDoesNotExist_ShouldPersistAndReturnResponse()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var validationResult = new ValidationResult();
        var createdShoppingCart = new ShoppingCart(clientId);

        var shoppingCartRepositoryMock = new Mock<IShoppingCartRepository>(MockBehavior.Strict);
        var shoppingCartLineValidationPolicyMock = new Mock<IValidationPolicy<IReadOnlyCollection<ShoppingCartLine>>>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<ShoppingCartService>>(MockBehavior.Loose);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(validationResult);

        shoppingCartRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetShoppingCartByClientId(clientId))
            .ReturnsAsync((ShoppingCart?)null);

        shoppingCartRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.CreateShoppingCart(It.Is<ShoppingCart>(cart => cart.ClientId == clientId)))
            .ReturnsAsync(createdShoppingCart);

        var sut = new ShoppingCartService(
            shoppingCartRepositoryMock.Object,
            shoppingCartLineValidationPolicyMock.Object,
            guidValidationPolicyMock.Object,
            loggerMock.Object);

        // Act
        var response = await sut.CreateShoppingCart(clientId);

        // Assert
        guidValidationPolicyMock.Verify(policy => policy.Validate(clientId), Times.Once);
        shoppingCartRepositoryMock.Verify(repo => repo.GetShoppingCartByClientId(clientId), Times.Once);
        shoppingCartRepositoryMock.Verify(repo => repo.CreateShoppingCart(It.IsAny<ShoppingCart>()), Times.Once);
        shoppingCartLineValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<IReadOnlyCollection<ShoppingCartLine>>()), Times.Never);

        response.ShouldNotBeNull();
        response.Id.ShouldBe(createdShoppingCart.Id);
        response.ClientId.ShouldBe(clientId);
        response.Lines.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateShoppingCart_WhenClientIdValidationFails_ShouldThrowValidationExceptionAndNotPersist()
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

        var shoppingCartRepositoryMock = new Mock<IShoppingCartRepository>(MockBehavior.Strict);
        var shoppingCartLineValidationPolicyMock = new Mock<IValidationPolicy<IReadOnlyCollection<ShoppingCartLine>>>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<ShoppingCartService>>(MockBehavior.Loose);

        guidValidationPolicyMock
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(invalidResult);

        var sut = new ShoppingCartService(
            shoppingCartRepositoryMock.Object,
            shoppingCartLineValidationPolicyMock.Object,
            guidValidationPolicyMock.Object,
            loggerMock.Object);

        // Act / Assert
        await Should.ThrowAsync<ValidationException>(() => sut.CreateShoppingCart(clientId));

        shoppingCartRepositoryMock.Verify(repo => repo.GetShoppingCartByClientId(It.IsAny<Guid>()), Times.Never);
        shoppingCartRepositoryMock.Verify(repo => repo.CreateShoppingCart(It.IsAny<ShoppingCart>()), Times.Never);
        shoppingCartLineValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<IReadOnlyCollection<ShoppingCartLine>>()), Times.Never);
    }

    [Fact]
    public async Task CreateShoppingCart_WhenShoppingCartAlreadyExists_ShouldThrowResourceAlreadyExistsException()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var validationResult = new ValidationResult();
        var existingShoppingCart = BuildShoppingCart(clientId);

        var shoppingCartRepositoryMock = new Mock<IShoppingCartRepository>(MockBehavior.Strict);
        var shoppingCartLineValidationPolicyMock = new Mock<IValidationPolicy<IReadOnlyCollection<ShoppingCartLine>>>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<ShoppingCartService>>(MockBehavior.Loose);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(validationResult);

        shoppingCartRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetShoppingCartByClientId(clientId))
            .ReturnsAsync(existingShoppingCart);

        var sut = new ShoppingCartService(
            shoppingCartRepositoryMock.Object,
            shoppingCartLineValidationPolicyMock.Object,
            guidValidationPolicyMock.Object,
            loggerMock.Object);

        // Act / Assert
        var ex = await Should.ThrowAsync<ResourceAlreadyExistsException>(() => sut.CreateShoppingCart(clientId));

        ex.ResourceId.ShouldBe(clientId);
        ex.ResourceType.ShouldBe(nameof(ShoppingCart));

        shoppingCartRepositoryMock.Verify(repo => repo.CreateShoppingCart(It.IsAny<ShoppingCart>()), Times.Never);
        shoppingCartLineValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<IReadOnlyCollection<ShoppingCartLine>>()), Times.Never);
    }

    [Fact]
    public async Task UpdateShoppingCart_WhenRequestIsValid_ShouldValidatePersistAndReturnResponse()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var product1Id = Guid.NewGuid();
        var product2Id = Guid.NewGuid();

        var request = new UpdateShoppingCartRequestDto
        {
            Lines =
            [
                new ShoppingCartLineRequestDto
                {
                    ProductId = product1Id,
                    Quantity = 2
                },
                new ShoppingCartLineRequestDto
                {
                    ProductId = product2Id,
                    Quantity = 1
                }
            ]
        };

        var shoppingCart = new ShoppingCart(clientId);
        var guidValidationResult = new ValidationResult();
        var linesValidationResult = new ValidationResult();

        var shoppingCartRepositoryMock = new Mock<IShoppingCartRepository>(MockBehavior.Strict);
        var shoppingCartLineValidationPolicyMock = new Mock<IValidationPolicy<IReadOnlyCollection<ShoppingCartLine>>>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<ShoppingCartService>>(MockBehavior.Loose);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(guidValidationResult);

        shoppingCartRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetShoppingCartByClientId(clientId))
            .ReturnsAsync(shoppingCart);

        shoppingCartLineValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(It.Is<IReadOnlyCollection<ShoppingCartLine>>(lines =>
                lines.Count == request.Lines.Count)))
            .ReturnsAsync(linesValidationResult);

        shoppingCartRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.UpdateShoppingCart(It.IsAny<ShoppingCart>()))
            .ReturnsAsync((ShoppingCart persistedCart) => persistedCart);

        var sut = new ShoppingCartService(
            shoppingCartRepositoryMock.Object,
            shoppingCartLineValidationPolicyMock.Object,
            guidValidationPolicyMock.Object,
            loggerMock.Object);

        // Act
        var response = await sut.UpdateShoppingCart(clientId, request);

        // Assert
        guidValidationPolicyMock.Verify(policy => policy.Validate(clientId), Times.Once);
        shoppingCartRepositoryMock.Verify(repo => repo.GetShoppingCartByClientId(clientId), Times.Once);
        shoppingCartLineValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<IReadOnlyCollection<ShoppingCartLine>>()), Times.Once);
        shoppingCartRepositoryMock.Verify(repo => repo.UpdateShoppingCart(It.IsAny<ShoppingCart>()), Times.Once);

        response.ShouldNotBeNull();
        response.Id.ShouldBe(shoppingCart.Id);
        response.ClientId.ShouldBe(clientId);
        response.Lines.Count.ShouldBe(request.Lines.Count);
    }

    [Fact]
    public async Task UpdateShoppingCart_WhenClientIdValidationFails_ShouldThrowValidationExceptionAndNotLoadOrPersist()
    {
        // Arrange
        var clientId = Guid.Empty;
        var request = new UpdateShoppingCartRequestDto
        {
            Lines =
            [
                new ShoppingCartLineRequestDto
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 1
                }
            ]
        };

        var invalidResult = new ValidationResult();
        invalidResult.AddValidationError(new ValidationError
        {
            Entity = nameof(Guid),
            Name = "clientId",
            Message = "ClientId cannot be empty"
        });

        var shoppingCartRepositoryMock = new Mock<IShoppingCartRepository>(MockBehavior.Strict);
        var shoppingCartLineValidationPolicyMock = new Mock<IValidationPolicy<IReadOnlyCollection<ShoppingCartLine>>>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<ShoppingCartService>>(MockBehavior.Loose);

        guidValidationPolicyMock
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(invalidResult);

        var sut = new ShoppingCartService(
            shoppingCartRepositoryMock.Object,
            shoppingCartLineValidationPolicyMock.Object,
            guidValidationPolicyMock.Object,
            loggerMock.Object);

        // Act / Assert
        await Should.ThrowAsync<ValidationException>(() => sut.UpdateShoppingCart(clientId, request));

        shoppingCartRepositoryMock.Verify(repo => repo.GetShoppingCartByClientId(It.IsAny<Guid>()), Times.Never);
        shoppingCartLineValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<IReadOnlyCollection<ShoppingCartLine>>()), Times.Never);
        shoppingCartRepositoryMock.Verify(repo => repo.UpdateShoppingCart(It.IsAny<ShoppingCart>()), Times.Never);
    }

    [Fact]
    public async Task UpdateShoppingCart_WhenShoppingCartDoesNotExist_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var request = new UpdateShoppingCartRequestDto
        {
            Lines =
            [
                new ShoppingCartLineRequestDto
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 1
                }
            ]
        };

        var guidValidationResult = new ValidationResult();

        var shoppingCartRepositoryMock = new Mock<IShoppingCartRepository>(MockBehavior.Strict);
        var shoppingCartLineValidationPolicyMock = new Mock<IValidationPolicy<IReadOnlyCollection<ShoppingCartLine>>>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<ShoppingCartService>>(MockBehavior.Loose);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(guidValidationResult);

        shoppingCartRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetShoppingCartByClientId(clientId))
            .ReturnsAsync((ShoppingCart?)null);

        var sut = new ShoppingCartService(
            shoppingCartRepositoryMock.Object,
            shoppingCartLineValidationPolicyMock.Object,
            guidValidationPolicyMock.Object,
            loggerMock.Object);

        // Act / Assert
        var ex = await Should.ThrowAsync<ResourceNotFoundException>(() => sut.UpdateShoppingCart(clientId, request));

        ex.ResourceId.ShouldBe(clientId);
        ex.ResourceType.ShouldBe(nameof(ShoppingCart));

        shoppingCartLineValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<IReadOnlyCollection<ShoppingCartLine>>()), Times.Never);
        shoppingCartRepositoryMock.Verify(repo => repo.UpdateShoppingCart(It.IsAny<ShoppingCart>()), Times.Never);
    }

    [Fact]
    public async Task UpdateShoppingCart_WhenLinesValidationFails_ShouldThrowValidationExceptionAndNotPersist()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var request = new UpdateShoppingCartRequestDto
        {
            Lines =
            [
                new ShoppingCartLineRequestDto
                {
                    ProductId = Guid.Empty,
                    Quantity = 0
                }
            ]
        };

        var shoppingCart = new ShoppingCart(clientId);
        var guidValidationResult = new ValidationResult();

        var invalidLinesResult = new ValidationResult();
        invalidLinesResult.AddValidationError(new ValidationError
        {
            Entity = nameof(ShoppingCartLine),
            Name = nameof(ShoppingCartLine.Quantity),
            Message = "Quantity must be greater than zero."
        });

        var shoppingCartRepositoryMock = new Mock<IShoppingCartRepository>(MockBehavior.Strict);
        var shoppingCartLineValidationPolicyMock = new Mock<IValidationPolicy<IReadOnlyCollection<ShoppingCartLine>>>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<ShoppingCartService>>(MockBehavior.Loose);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(guidValidationResult);

        shoppingCartRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetShoppingCartByClientId(clientId))
            .ReturnsAsync(shoppingCart);

        shoppingCartLineValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(It.IsAny<IReadOnlyCollection<ShoppingCartLine>>()))
            .ReturnsAsync(invalidLinesResult);

        var sut = new ShoppingCartService(
            shoppingCartRepositoryMock.Object,
            shoppingCartLineValidationPolicyMock.Object,
            guidValidationPolicyMock.Object,
            loggerMock.Object);

        // Act / Assert
        await Should.ThrowAsync<ValidationException>(() => sut.UpdateShoppingCart(clientId, request));

        shoppingCartRepositoryMock.Verify(repo => repo.UpdateShoppingCart(It.IsAny<ShoppingCart>()), Times.Never);
    }

    private static ShoppingCart BuildShoppingCart(Guid clientId)
    {
        return ShoppingCart.Rehydrate(
            Guid.NewGuid(),
            clientId,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow,
            [
                new ShoppingCartLine(Guid.NewGuid(), 1),
                new ShoppingCartLine(Guid.NewGuid(), 2)
            ]);
    }
}