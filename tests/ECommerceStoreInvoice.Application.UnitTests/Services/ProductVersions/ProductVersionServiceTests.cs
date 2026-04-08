using ECommerceStoreInvoice.Application.Common.RequestsDto.ProductVersions;
using ECommerceStoreInvoice.Application.Services.Concrete.ProductVersions;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
using Moq;
using Shouldly;

namespace ECommerceStoreInvoice.Application.UnitTests.Services.ProductVersions;

public sealed class ProductVersionServiceTests
{
    [Fact]
    public async Task CreateProductVersion_WhenRequestIsValid_ShouldValidatePersistAndReturnResponse()
    {
        // Arrange
        var request = new CreateProductVersionRequestDto
        {
            ProductId = Guid.NewGuid(),
            PriceAmount = 499.99m,
            PriceCurrency = "usd",
            Name = "iPhone 20",
            Brand = "Apple"
        };

        var validationResult = new ValidationResult();
        var createdProductVersion = ProductVersion.Rehydrate(
            Guid.NewGuid(),
            true,
            DateTime.UtcNow,
            null,
            request.ProductId,
            new Money(request.PriceAmount, request.PriceCurrency.ToUpperInvariant()),
            request.Name,
            request.Brand);

        var productVersionRepositoryMock = new Mock<IProductVersionRepository>(MockBehavior.Strict);
        var productVersionValidationPolicyMock = new Mock<IValidationPolicy<ProductVersion>>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);

        productVersionValidationPolicyMock
            .Setup(policy => policy.Validate(It.IsAny<ProductVersion>()))
            .ReturnsAsync(validationResult);

        productVersionRepositoryMock
            .Setup(repo => repo.CreateProductVersion(It.IsAny<ProductVersion>()))
            .ReturnsAsync(createdProductVersion);

        var sut = new ProductVersionService(
            productVersionRepositoryMock.Object,
            productVersionValidationPolicyMock.Object,
            guidValidationPolicyMock.Object);

        // Act
        var response = await sut.CreateProductVersion(request);

        // Assert
        productVersionValidationPolicyMock.Verify(policy => policy.Validate(It.Is<ProductVersion>(pv =>
            pv.ProductId == request.ProductId &&
            pv.Price.Amount == request.PriceAmount &&
            pv.Price.Currency == request.PriceCurrency.ToUpperInvariant() &&
            pv.Name == request.Name &&
            pv.Brand == request.Brand)), Times.Once);

        productVersionRepositoryMock.Verify(repo => repo.CreateProductVersion(It.IsAny<ProductVersion>()), Times.Once);
        guidValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<Guid>()), Times.Never);

        response.ShouldNotBeNull();
        response.Id.ShouldBe(createdProductVersion.Id);
        response.ProductId.ShouldBe(request.ProductId);
        response.PriceAmount.ShouldBe(request.PriceAmount);
        response.PriceCurrency.ShouldBe(request.PriceCurrency.ToUpperInvariant());
        response.Name.ShouldBe(request.Name);
        response.Brand.ShouldBe(request.Brand);
        response.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateProductVersion_WhenValidationFails_ShouldThrowValidationExceptionAndNotPersist()
    {
        // Arrange
        var request = new CreateProductVersionRequestDto
        {
            ProductId = Guid.Empty,
            PriceAmount = -1,
            PriceCurrency = "",
            Name = "",
            Brand = ""
        };

        var invalidResult = new ValidationResult();
        invalidResult.AddValidationError(new ValidationError
        {
            Entity = nameof(ProductVersion),
            Name = nameof(ProductVersion.ProductId),
            Message = "ProductId cannot be empty"
        });

        var productVersionRepositoryMock = new Mock<IProductVersionRepository>(MockBehavior.Strict);
        var productVersionValidationPolicyMock = new Mock<IValidationPolicy<ProductVersion>>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);

        productVersionValidationPolicyMock
            .Setup(policy => policy.Validate(It.IsAny<ProductVersion>()))
            .ReturnsAsync(invalidResult);

        var sut = new ProductVersionService(
            productVersionRepositoryMock.Object,
            productVersionValidationPolicyMock.Object,
            guidValidationPolicyMock.Object);

        // Act / Assert
        await Should.ThrowAsync<ValidationException>(() => sut.CreateProductVersion(request));

        productVersionRepositoryMock.Verify(repo => repo.CreateProductVersion(It.IsAny<ProductVersion>()), Times.Never);
        guidValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetProductVersionById_WhenIdIsValidAndEntityExists_ShouldReturnResponse()
    {
        // Arrange
        var id = Guid.NewGuid();
        var validationResult = new ValidationResult();
        var productVersion = ProductVersion.Rehydrate(
            id,
            true,
            DateTime.UtcNow,
            null,
            Guid.NewGuid(),
            new Money(799.00m, "EUR"),
            "Galaxy Ultra",
            "Samsung");

        var productVersionRepositoryMock = new Mock<IProductVersionRepository>(MockBehavior.Strict);
        var productVersionValidationPolicyMock = new Mock<IValidationPolicy<ProductVersion>>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(id))
            .ReturnsAsync(validationResult);

        productVersionRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetProductVersionById(id))
            .ReturnsAsync(productVersion);

        var sut = new ProductVersionService(
            productVersionRepositoryMock.Object,
            productVersionValidationPolicyMock.Object,
            guidValidationPolicyMock.Object);

        // Act
        var response = await sut.GetProductVersionById(id);

        // Assert
        guidValidationPolicyMock.Verify(policy => policy.Validate(id), Times.Once);
        productVersionRepositoryMock.Verify(repo => repo.GetProductVersionById(id), Times.Once);
        productVersionValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<ProductVersion>()), Times.Never);

        response.ShouldNotBeNull();
        response.Id.ShouldBe(productVersion.Id);
        response.ProductId.ShouldBe(productVersion.ProductId);
        response.PriceAmount.ShouldBe(productVersion.Price.Amount);
        response.PriceCurrency.ShouldBe(productVersion.Price.Currency);
        response.Name.ShouldBe(productVersion.Name);
        response.Brand.ShouldBe(productVersion.Brand);
        response.IsActive.ShouldBe(productVersion.IsActive);
    }

    [Fact]
    public async Task GetProductVersionById_WhenIdValidationFails_ShouldThrowValidationExceptionAndNotLoadEntity()
    {
        // Arrange
        var id = Guid.Empty;

        var invalidResult = new ValidationResult();
        invalidResult.AddValidationError(new ValidationError
        {
            Entity = nameof(Guid),
            Name = "id",
            Message = "Id cannot be empty"
        });

        var productVersionRepositoryMock = new Mock<IProductVersionRepository>(MockBehavior.Strict);
        var productVersionValidationPolicyMock = new Mock<IValidationPolicy<ProductVersion>>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);

        guidValidationPolicyMock
            .Setup(policy => policy.Validate(id))
            .ReturnsAsync(invalidResult);

        var sut = new ProductVersionService(
            productVersionRepositoryMock.Object,
            productVersionValidationPolicyMock.Object,
            guidValidationPolicyMock.Object);

        // Act / Assert
        await Should.ThrowAsync<ValidationException>(() => sut.GetProductVersionById(id));

        productVersionRepositoryMock.Verify(repo => repo.GetProductVersionById(It.IsAny<Guid>()), Times.Never);
        productVersionValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<ProductVersion>()), Times.Never);
    }

    [Fact]
    public async Task GetProductVersionById_WhenEntityDoesNotExist_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var validationResult = new ValidationResult();

        var productVersionRepositoryMock = new Mock<IProductVersionRepository>(MockBehavior.Strict);
        var productVersionValidationPolicyMock = new Mock<IValidationPolicy<ProductVersion>>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(id))
            .ReturnsAsync(validationResult);

        productVersionRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetProductVersionById(id))
            .ReturnsAsync((ProductVersion?)null);

        var sut = new ProductVersionService(
            productVersionRepositoryMock.Object,
            productVersionValidationPolicyMock.Object,
            guidValidationPolicyMock.Object);

        // Act / Assert
        var ex = await Should.ThrowAsync<ResourceNotFoundException>(() => sut.GetProductVersionById(id));

        ex.ResourceId.ShouldBe(id);
        ex.ResourceType.ShouldBe(nameof(ProductVersion));

        guidValidationPolicyMock.Verify(policy => policy.Validate(id), Times.Once);
        productVersionRepositoryMock.Verify(repo => repo.GetProductVersionById(id), Times.Once);
        productVersionValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<ProductVersion>()), Times.Never);
    }
}
