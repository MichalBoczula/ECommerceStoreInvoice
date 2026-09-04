using ECommerceStoreInvoice.Application.Common.RequestsDto.ProductVersions;
using ECommerceStoreInvoice.Application.Services.Concrete.ProductVersions;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.ExternalServices;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
using Microsoft.Extensions.Logging;
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
            ProductId = Guid.NewGuid()
        };

        var externalSnapshot = new ExternalProductSnapshot(
            request.ProductId,
            "iPhone 20",
            "Apple",
            new Money(499.99m, "USD")
        );

        var validationResult = new ValidationResult();
        var createdProductVersion = ProductVersion.Rehydrate(
            Guid.NewGuid(),
            true,
            DateTime.UtcNow,
            null,
            request.ProductId,
            externalSnapshot.Price,
            externalSnapshot.Name,
            externalSnapshot.Brand);

        var productVersionRepositoryMock = new Mock<IProductVersionRepository>(MockBehavior.Strict);
        var productServiceClientMock = new Mock<IProductServiceClient>(MockBehavior.Strict);
        var productVersionValidationPolicyMock = new Mock<IValidationPolicy<ProductVersion>>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<ProductVersionService>>();

        productServiceClientMock
            .Setup(client => client.GetProductsByIds(It.Is<IEnumerable<Guid>>(ids => ids.Contains(request.ProductId))))
            .ReturnsAsync([externalSnapshot]);

        productVersionValidationPolicyMock
            .Setup(policy => policy.Validate(It.IsAny<ProductVersion>()))
            .ReturnsAsync(validationResult);

        productVersionRepositoryMock
            .Setup(repo => repo.CreateProductVersion(It.IsAny<ProductVersion>()))
            .ReturnsAsync(createdProductVersion);

        var sut = new ProductVersionService(
            productVersionRepositoryMock.Object,
            productServiceClientMock.Object,
            productVersionValidationPolicyMock.Object,
            guidValidationPolicyMock.Object,
            loggerMock.Object);

        // Act
        var response = await sut.CreateProductVersion(request);

        // Assert
        productServiceClientMock.Verify(client => client.GetProductsByIds(It.IsAny<IEnumerable<Guid>>()), Times.Once);

        productVersionValidationPolicyMock.Verify(policy => policy.Validate(It.Is<ProductVersion>(pv =>
            pv.ProductId == request.ProductId &&
            pv.Price.Amount == externalSnapshot.Price.Amount &&
            pv.Price.Currency == externalSnapshot.Price.Currency &&
            pv.Name == externalSnapshot.Name &&
            pv.Brand == externalSnapshot.Brand)), Times.Once);

        productVersionRepositoryMock.Verify(repo => repo.CreateProductVersion(It.IsAny<ProductVersion>()), Times.Once);
        guidValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<Guid>()), Times.Never);

        response.ShouldNotBeNull();
        response.Id.ShouldBe(createdProductVersion.Id);
        response.ProductId.ShouldBe(request.ProductId);
        response.PriceAmount.ShouldBe(externalSnapshot.Price.Amount);
        response.PriceCurrency.ShouldBe(externalSnapshot.Price.Currency);
        response.Name.ShouldBe(externalSnapshot.Name);
        response.Brand.ShouldBe(externalSnapshot.Brand);
        response.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateProductVersion_WhenExternalProductNotFound_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var request = new CreateProductVersionRequestDto
        {
            ProductId = Guid.NewGuid()
        };

        var productVersionRepositoryMock = new Mock<IProductVersionRepository>(MockBehavior.Strict);
        var productServiceClientMock = new Mock<IProductServiceClient>(MockBehavior.Strict);
        var productVersionValidationPolicyMock = new Mock<IValidationPolicy<ProductVersion>>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<ProductVersionService>>();

        // Return empty list to simulate 404 / missing product
        productServiceClientMock
            .Setup(client => client.GetProductsByIds(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(Array.Empty<ExternalProductSnapshot>());

        var sut = new ProductVersionService(
            productVersionRepositoryMock.Object,
            productServiceClientMock.Object,
            productVersionValidationPolicyMock.Object,
            guidValidationPolicyMock.Object,
            loggerMock.Object);

        // Act / Assert
        var ex = await Should.ThrowAsync<ResourceNotFoundException>(() => sut.CreateProductVersion(request));

        ex.ResourceId.ShouldBe(request.ProductId);
        ex.ResourceType.ShouldBe(nameof(ExternalProductSnapshot));

        productServiceClientMock.Verify(client => client.GetProductsByIds(It.IsAny<IEnumerable<Guid>>()), Times.Once);
        productVersionValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<ProductVersion>()), Times.Never);
        productVersionRepositoryMock.Verify(repo => repo.CreateProductVersion(It.IsAny<ProductVersion>()), Times.Never);
    }

    [Fact]
    public async Task CreateProductVersion_WhenValidationFails_ShouldThrowValidationExceptionAndNotPersist()
    {
        // Arrange
        var request = new CreateProductVersionRequestDto
        {
            ProductId = Guid.Empty
        };

        var externalSnapshot = new ExternalProductSnapshot(
            request.ProductId,
            "",
            "",
            new Money(-1, "USD")
        );

        var invalidResult = new ValidationResult();
        invalidResult.AddValidationError(new ValidationError
        {
            Entity = nameof(ProductVersion),
            Name = nameof(ProductVersion.ProductId),
            Message = "ProductId cannot be empty"
        });

        var productVersionRepositoryMock = new Mock<IProductVersionRepository>(MockBehavior.Strict);
        var productServiceClientMock = new Mock<IProductServiceClient>(MockBehavior.Strict);
        var productVersionValidationPolicyMock = new Mock<IValidationPolicy<ProductVersion>>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<ProductVersionService>>();

        productServiceClientMock
            .Setup(client => client.GetProductsByIds(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([externalSnapshot]);

        productVersionValidationPolicyMock
            .Setup(policy => policy.Validate(It.IsAny<ProductVersion>()))
            .ReturnsAsync(invalidResult);

        var sut = new ProductVersionService(
            productVersionRepositoryMock.Object,
            productServiceClientMock.Object,
            productVersionValidationPolicyMock.Object,
            guidValidationPolicyMock.Object,
            loggerMock.Object);

        // Act / Assert
        await Should.ThrowAsync<ValidationException>(() => sut.CreateProductVersion(request));

        productServiceClientMock.Verify(client => client.GetProductsByIds(It.IsAny<IEnumerable<Guid>>()), Times.Once);
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
        var productServiceClientMock = new Mock<IProductServiceClient>(MockBehavior.Strict);
        var productVersionValidationPolicyMock = new Mock<IValidationPolicy<ProductVersion>>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<ProductVersionService>>();

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
            productServiceClientMock.Object,
            productVersionValidationPolicyMock.Object,
            guidValidationPolicyMock.Object,
            loggerMock.Object);

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
        var productServiceClientMock = new Mock<IProductServiceClient>(MockBehavior.Strict);
        var productVersionValidationPolicyMock = new Mock<IValidationPolicy<ProductVersion>>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<ProductVersionService>>();

        guidValidationPolicyMock
            .Setup(policy => policy.Validate(id))
            .ReturnsAsync(invalidResult);

        var sut = new ProductVersionService(
            productVersionRepositoryMock.Object,
            productServiceClientMock.Object,
            productVersionValidationPolicyMock.Object,
            guidValidationPolicyMock.Object,
            loggerMock.Object);

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
        var productServiceClientMock = new Mock<IProductServiceClient>(MockBehavior.Strict);
        var productVersionValidationPolicyMock = new Mock<IValidationPolicy<ProductVersion>>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<ProductVersionService>>();

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
            productServiceClientMock.Object,
            productVersionValidationPolicyMock.Object,
            guidValidationPolicyMock.Object,
            loggerMock.Object);

        // Act / Assert
        var ex = await Should.ThrowAsync<ResourceNotFoundException>(() => sut.GetProductVersionById(id));

        ex.ResourceId.ShouldBe(id);
        ex.ResourceType.ShouldBe(nameof(ProductVersion));

        guidValidationPolicyMock.Verify(policy => policy.Validate(id), Times.Once);
        productVersionRepositoryMock.Verify(repo => repo.GetProductVersionById(id), Times.Once);
        productVersionValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<ProductVersion>()), Times.Never);
    }

    [Fact]
    public async Task CreateMultipleProductVersions_WhenRequestIsValid_ShouldValidatePersistAndReturnResponses()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();

        var request = new CreateMultipleProductVersionsRequestDto
        {
            ProductIds = [productId1, productId2]
        };

        var snapshot1 = new ExternalProductSnapshot(productId1, "Phone 1", "Brand A", new Money(100m, "USD"));
        var snapshot2 = new ExternalProductSnapshot(productId2, "Phone 2", "Brand B", new Money(200m, "USD"));

        var createdPv1 = ProductVersion.Rehydrate(Guid.NewGuid(), true, DateTime.UtcNow, null, productId1, snapshot1.Price, snapshot1.Name, snapshot1.Brand);
        var createdPv2 = ProductVersion.Rehydrate(Guid.NewGuid(), true, DateTime.UtcNow, null, productId2, snapshot2.Price, snapshot2.Name, snapshot2.Brand);

        var productVersionRepositoryMock = new Mock<IProductVersionRepository>(MockBehavior.Strict);
        var productServiceClientMock = new Mock<IProductServiceClient>(MockBehavior.Strict);
        var productVersionValidationPolicyMock = new Mock<IValidationPolicy<ProductVersion>>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<ProductVersionService>>();

        productServiceClientMock
            .Setup(client => client.GetProductsByIds(It.Is<IEnumerable<Guid>>(ids => ids.Contains(productId1) && ids.Contains(productId2))))
            .ReturnsAsync([snapshot1, snapshot2]);

        productVersionValidationPolicyMock
            .Setup(policy => policy.Validate(It.IsAny<ProductVersion>()))
            .ReturnsAsync(new ValidationResult());

        productVersionRepositoryMock
            .Setup(repo => repo.CreateProductVersion(It.Is<ProductVersion>(pv => pv.ProductId == productId1)))
            .ReturnsAsync(createdPv1);

        productVersionRepositoryMock
            .Setup(repo => repo.CreateProductVersion(It.Is<ProductVersion>(pv => pv.ProductId == productId2)))
            .ReturnsAsync(createdPv2);

        var sut = new ProductVersionService(
            productVersionRepositoryMock.Object,
            productServiceClientMock.Object,
            productVersionValidationPolicyMock.Object,
            guidValidationPolicyMock.Object,
            loggerMock.Object);

        // Act
        var responses = await sut.CreateMultipleProductVersions(request);

        // Assert
        responses.ShouldNotBeNull();
        responses.Count.ShouldBe(2);

        productServiceClientMock.Verify(client => client.GetProductsByIds(It.IsAny<IEnumerable<Guid>>()), Times.Once);
        productVersionValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<ProductVersion>()), Times.Exactly(2));
        productVersionRepositoryMock.Verify(repo => repo.CreateProductVersion(It.IsAny<ProductVersion>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateMultipleProductVersions_WhenExternalServiceReturnsLessProducts_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var existingProductId = Guid.NewGuid();
        var missingProductId = Guid.NewGuid();

        var request = new CreateMultipleProductVersionsRequestDto
        {
            ProductIds = [existingProductId, missingProductId]
        };

        var snapshot1 = new ExternalProductSnapshot(existingProductId, "Phone 1", "Brand A", new Money(100m, "USD"));

        var productVersionRepositoryMock = new Mock<IProductVersionRepository>(MockBehavior.Strict);
        var productServiceClientMock = new Mock<IProductServiceClient>(MockBehavior.Strict);
        var productVersionValidationPolicyMock = new Mock<IValidationPolicy<ProductVersion>>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<ProductVersionService>>();

        productServiceClientMock
            .Setup(client => client.GetProductsByIds(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([snapshot1]);

        var sut = new ProductVersionService(
            productVersionRepositoryMock.Object,
            productServiceClientMock.Object,
            productVersionValidationPolicyMock.Object,
            guidValidationPolicyMock.Object,
            loggerMock.Object);

        // Act / Assert
        var ex = await Should.ThrowAsync<ResourceNotFoundException>(() => sut.CreateMultipleProductVersions(request));

        ex.ResourceId.ShouldBe(missingProductId);
        ex.ResourceType.ShouldBe(nameof(ExternalProductSnapshot));

        productServiceClientMock.Verify(client => client.GetProductsByIds(It.IsAny<IEnumerable<Guid>>()), Times.Once);
        productVersionValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<ProductVersion>()), Times.Never);
        productVersionRepositoryMock.Verify(repo => repo.CreateProductVersion(It.IsAny<ProductVersion>()), Times.Never);
    }

    [Fact]
    public async Task CreateMultipleProductVersions_WhenDomainValidationFails_ShouldThrowValidationExceptionAndNotPersist()
    {
        // Arrange
        var emptyProductId = Guid.Empty;

        var request = new CreateMultipleProductVersionsRequestDto
        {
            ProductIds = [emptyProductId]
        };

        var externalSnapshot = new ExternalProductSnapshot(emptyProductId, "Invalid Phone", "Brand A", new Money(100m, "USD"));

        var invalidResult = new ValidationResult();
        invalidResult.AddValidationError(new ValidationError
        {
            Entity = nameof(ProductVersion),
            Name = nameof(ProductVersion.ProductId),
            Message = "ProductId cannot be empty"
        });

        var productVersionRepositoryMock = new Mock<IProductVersionRepository>(MockBehavior.Strict);
        var productServiceClientMock = new Mock<IProductServiceClient>(MockBehavior.Strict);
        var productVersionValidationPolicyMock = new Mock<IValidationPolicy<ProductVersion>>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<ProductVersionService>>();

        productServiceClientMock
            .Setup(client => client.GetProductsByIds(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([externalSnapshot]);

        productVersionValidationPolicyMock
            .Setup(policy => policy.Validate(It.IsAny<ProductVersion>()))
            .ReturnsAsync(invalidResult);

        var sut = new ProductVersionService(
            productVersionRepositoryMock.Object,
            productServiceClientMock.Object,
            productVersionValidationPolicyMock.Object,
            guidValidationPolicyMock.Object,
            loggerMock.Object);

        // Act / Assert
        await Should.ThrowAsync<ValidationException>(() => sut.CreateMultipleProductVersions(request));

        productServiceClientMock.Verify(client => client.GetProductsByIds(It.IsAny<IEnumerable<Guid>>()), Times.Once);
        productVersionValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<ProductVersion>()), Times.Once);

        productVersionRepositoryMock.Verify(repo => repo.CreateProductVersion(It.IsAny<ProductVersion>()), Times.Never);
    }
}