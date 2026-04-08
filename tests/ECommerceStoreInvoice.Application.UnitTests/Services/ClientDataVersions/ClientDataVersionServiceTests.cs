using ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Services.Concrete.ClientDataVersions;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
using Moq;
using Shouldly;

namespace ECommerceStoreInvoice.Application.UnitTests.Services.ClientDataVersions;

public sealed class ClientDataVersionServiceTests
{
    [Fact]
    public async Task Create_WhenRequestIsValid_ShouldValidatePersistAndReturnResponse()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var request = new CreateClientDataVersionRequestDto
        {
            ClientName = "John Doe",
            PostalCode = "00-001",
            City = "Warsaw",
            Street = "Main",
            BuildingNumber = "10",
            ApartmentNumber = "5",
            PhoneNumber = "123456789",
            PhonePrefix = "+48",
            AddressEmail = "john.doe@example.com"
        };

        var guidValidationResult = new ValidationResult();
        var clientDataVersionValidationResult = new ValidationResult();

        var clientDataVersionRepositoryMock = new Mock<IClientDataVersionRepository>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var clientDataVersionValidationPolicyMock = new Mock<IValidationPolicy<ClientDataVersion>>(MockBehavior.Strict);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(guidValidationResult);

        clientDataVersionValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(It.Is<ClientDataVersion>(cdv =>
                cdv.ClientId == clientId &&
                cdv.ClientName == request.ClientName &&
                cdv.Address.PostalCode == request.PostalCode &&
                cdv.Address.City == request.City &&
                cdv.Address.Street == request.Street &&
                cdv.Address.BuildingNumber == request.BuildingNumber &&
                cdv.Address.ApartmentNumber == request.ApartmentNumber &&
                cdv.PhoneNumber == request.PhoneNumber &&
                cdv.PhonePrefix == request.PhonePrefix &&
                cdv.AddressEmail == request.AddressEmail)))
            .ReturnsAsync(clientDataVersionValidationResult);

        clientDataVersionRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.Create(It.IsAny<ClientDataVersion>()))
            .Returns(Task.CompletedTask);

        var sut = new ClientDataVersionService(
            clientDataVersionRepositoryMock.Object,
            guidValidationPolicyMock.Object,
            clientDataVersionValidationPolicyMock.Object);

        // Act
        var response = await sut.Create(clientId, request);

        // Assert
        guidValidationPolicyMock.Verify(policy => policy.Validate(clientId), Times.Once);
        clientDataVersionValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<ClientDataVersion>()), Times.Once);
        clientDataVersionRepositoryMock.Verify(repo => repo.Create(It.IsAny<ClientDataVersion>()), Times.Once);

        response.ShouldNotBeNull();
        response.ClientId.ShouldBe(clientId);
        response.ClientName.ShouldBe(request.ClientName);
        response.PostalCode.ShouldBe(request.PostalCode);
        response.City.ShouldBe(request.City);
        response.Street.ShouldBe(request.Street);
        response.BuildingNumber.ShouldBe(request.BuildingNumber);
        response.ApartmentNumber.ShouldBe(request.ApartmentNumber);
        response.PhoneNumber.ShouldBe(request.PhoneNumber);
        response.PhonePrefix.ShouldBe(request.PhonePrefix);
        response.AddressEmail.ShouldBe(request.AddressEmail);
        response.CreatedAt.ShouldBeInRange(DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
    }

    [Fact]
    public async Task Create_WhenClientIdValidationFails_ShouldThrowValidationExceptionAndNotPersist()
    {
        // Arrange
        var clientId = Guid.Empty;
        var request = new CreateClientDataVersionRequestDto
        {
            ClientName = "John Doe",
            PostalCode = "00-001",
            City = "Warsaw",
            Street = "Main",
            BuildingNumber = "10",
            ApartmentNumber = "5",
            PhoneNumber = "123456789",
            PhonePrefix = "+48",
            AddressEmail = "john.doe@example.com"
        };

        var invalidResult = new ValidationResult();
        invalidResult.AddValidationError(new ValidationError
        {
            Entity = nameof(Guid),
            Name = "clientId",
            Message = "ClientId cannot be empty"
        });

        var clientDataVersionRepositoryMock = new Mock<IClientDataVersionRepository>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var clientDataVersionValidationPolicyMock = new Mock<IValidationPolicy<ClientDataVersion>>(MockBehavior.Strict);

        guidValidationPolicyMock
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(invalidResult);

        var sut = new ClientDataVersionService(
            clientDataVersionRepositoryMock.Object,
            guidValidationPolicyMock.Object,
            clientDataVersionValidationPolicyMock.Object);

        // Act / Assert
        await Should.ThrowAsync<ValidationException>(() => sut.Create(clientId, request));

        clientDataVersionValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<ClientDataVersion>()), Times.Never);
        clientDataVersionRepositoryMock.Verify(repo => repo.Create(It.IsAny<ClientDataVersion>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenClientDataVersionValidationFails_ShouldThrowValidationExceptionAndNotPersist()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var request = new CreateClientDataVersionRequestDto
        {
            ClientName = "",
            PostalCode = "00-001",
            City = "Warsaw",
            Street = "Main",
            BuildingNumber = "10",
            ApartmentNumber = "5",
            PhoneNumber = "123456789",
            PhonePrefix = "+48",
            AddressEmail = "invalid-email"
        };

        var guidValidationResult = new ValidationResult();
        var invalidClientDataVersionResult = new ValidationResult();
        invalidClientDataVersionResult.AddValidationError(new ValidationError
        {
            Entity = nameof(ClientDataVersion),
            Name = nameof(ClientDataVersion.ClientName),
            Message = "ClientName cannot be empty"
        });

        var clientDataVersionRepositoryMock = new Mock<IClientDataVersionRepository>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var clientDataVersionValidationPolicyMock = new Mock<IValidationPolicy<ClientDataVersion>>(MockBehavior.Strict);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(guidValidationResult);

        clientDataVersionValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(It.IsAny<ClientDataVersion>()))
            .ReturnsAsync(invalidClientDataVersionResult);

        var sut = new ClientDataVersionService(
            clientDataVersionRepositoryMock.Object,
            guidValidationPolicyMock.Object,
            clientDataVersionValidationPolicyMock.Object);

        // Act / Assert
        await Should.ThrowAsync<ValidationException>(() => sut.Create(clientId, request));

        guidValidationPolicyMock.Verify(policy => policy.Validate(clientId), Times.Once);
        clientDataVersionValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<ClientDataVersion>()), Times.Once);
        clientDataVersionRepositoryMock.Verify(repo => repo.Create(It.IsAny<ClientDataVersion>()), Times.Never);
    }

    [Fact]
    public async Task GetByClientId_WhenClientIdIsValidAndEntityExists_ShouldReturnResponse()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var validationResult = new ValidationResult();
        var clientDataVersion = ClientDataVersion.Rehydrate(
            Guid.NewGuid(),
            clientId,
            "John Doe",
            new Address("00-001", "Warsaw", "Main", "10", "5"),
            "123456789",
            "+48",
            "john.doe@example.com",
            DateTime.UtcNow.AddDays(-1));

        var clientDataVersionRepositoryMock = new Mock<IClientDataVersionRepository>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var clientDataVersionValidationPolicyMock = new Mock<IValidationPolicy<ClientDataVersion>>(MockBehavior.Strict);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(validationResult);

        clientDataVersionRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetByClientId(clientId))
            .ReturnsAsync(clientDataVersion);

        var sut = new ClientDataVersionService(
            clientDataVersionRepositoryMock.Object,
            guidValidationPolicyMock.Object,
            clientDataVersionValidationPolicyMock.Object);

        // Act
        var response = await sut.GetByClientId(clientId);

        // Assert
        guidValidationPolicyMock.Verify(policy => policy.Validate(clientId), Times.Once);
        clientDataVersionRepositoryMock.Verify(repo => repo.GetByClientId(clientId), Times.Once);
        clientDataVersionValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<ClientDataVersion>()), Times.Never);

        response.ShouldNotBeNull();
        response.Id.ShouldBe(clientDataVersion.Id);
        response.ClientId.ShouldBe(clientId);
        response.ClientName.ShouldBe(clientDataVersion.ClientName);
        response.PostalCode.ShouldBe(clientDataVersion.Address.PostalCode);
        response.City.ShouldBe(clientDataVersion.Address.City);
        response.Street.ShouldBe(clientDataVersion.Address.Street);
        response.BuildingNumber.ShouldBe(clientDataVersion.Address.BuildingNumber);
        response.ApartmentNumber.ShouldBe(clientDataVersion.Address.ApartmentNumber);
        response.PhoneNumber.ShouldBe(clientDataVersion.PhoneNumber);
        response.PhonePrefix.ShouldBe(clientDataVersion.PhonePrefix);
        response.AddressEmail.ShouldBe(clientDataVersion.AddressEmail);
        response.CreatedAt.ShouldBe(clientDataVersion.CreatedAt);
    }

    [Fact]
    public async Task GetByClientId_WhenClientIdValidationFails_ShouldThrowValidationExceptionAndNotLoadEntity()
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

        var clientDataVersionRepositoryMock = new Mock<IClientDataVersionRepository>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var clientDataVersionValidationPolicyMock = new Mock<IValidationPolicy<ClientDataVersion>>(MockBehavior.Strict);

        guidValidationPolicyMock
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(invalidResult);

        var sut = new ClientDataVersionService(
            clientDataVersionRepositoryMock.Object,
            guidValidationPolicyMock.Object,
            clientDataVersionValidationPolicyMock.Object);

        // Act / Assert
        await Should.ThrowAsync<ValidationException>(() => sut.GetByClientId(clientId));

        clientDataVersionRepositoryMock.Verify(repo => repo.GetByClientId(It.IsAny<Guid>()), Times.Never);
        clientDataVersionValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<ClientDataVersion>()), Times.Never);
    }

    [Fact]
    public async Task GetByClientId_WhenEntityDoesNotExist_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var validationResult = new ValidationResult();

        var clientDataVersionRepositoryMock = new Mock<IClientDataVersionRepository>(MockBehavior.Strict);
        var guidValidationPolicyMock = new Mock<IValidationPolicy<Guid>>(MockBehavior.Strict);
        var clientDataVersionValidationPolicyMock = new Mock<IValidationPolicy<ClientDataVersion>>(MockBehavior.Strict);

        var sequence = new MockSequence();
        guidValidationPolicyMock
            .InSequence(sequence)
            .Setup(policy => policy.Validate(clientId))
            .ReturnsAsync(validationResult);

        clientDataVersionRepositoryMock
            .InSequence(sequence)
            .Setup(repo => repo.GetByClientId(clientId))
            .ReturnsAsync((ClientDataVersion?)null);

        var sut = new ClientDataVersionService(
            clientDataVersionRepositoryMock.Object,
            guidValidationPolicyMock.Object,
            clientDataVersionValidationPolicyMock.Object);

        // Act / Assert
        var ex = await Should.ThrowAsync<ResourceNotFoundException>(() => sut.GetByClientId(clientId));

        ex.ResourceId.ShouldBe(clientId);
        ex.ResourceType.ShouldBe(nameof(ClientDataVersion));

        guidValidationPolicyMock.Verify(policy => policy.Validate(clientId), Times.Once);
        clientDataVersionRepositoryMock.Verify(repo => repo.GetByClientId(clientId), Times.Once);
        clientDataVersionValidationPolicyMock.Verify(policy => policy.Validate(It.IsAny<ClientDataVersion>()), Times.Never);
    }
}
