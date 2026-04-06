using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ClientDataVersions;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Rules.ClientDataVersions
{
    public class ClientDataVersionEmailValidationRuleTests
    {
        [Fact]
        public async Task IsValid_AddressEmailIsWhitespace_ShouldReturnValidationError()
        {
            // Arrange
            var rule = new ClientDataVersionEmailValidationRule();
            var validationResult = new ValidationResult();
            var clientDataVersion = CreateClientDataVersion(" ");

            // Act
            await rule.IsValid(clientDataVersion, validationResult);

            // Assert
            validationResult.GetValidationErrors().Count.ShouldBe(1);
            validationResult.GetValidationErrors().First().Message.ShouldBe("Address email cannot be null or whitespace.");
        }

        [Fact]
        public async Task IsValid_AddressEmailHasInvalidFormat_ShouldReturnValidationError()
        {
            // Arrange
            var rule = new ClientDataVersionEmailValidationRule();
            var validationResult = new ValidationResult();
            var clientDataVersion = CreateClientDataVersion("client+alias@example.com");

            // Act
            await rule.IsValid(clientDataVersion, validationResult);

            // Assert
            validationResult.GetValidationErrors().Count.ShouldBe(1);
            validationResult.GetValidationErrors().First().Message.ShouldBe("Address email must use only letters, digits and '.', and include '@' with a domain (for example: user@gmail.com).");
        }

        [Fact]
        public async Task IsValid_AddressEmailHasValidFormat_ShouldReturnNoErrors()
        {
            // Arrange
            var rule = new ClientDataVersionEmailValidationRule();
            var validationResult = new ValidationResult();
            var clientDataVersion = CreateClientDataVersion("client.example@shop.com");

            // Act
            await rule.IsValid(clientDataVersion, validationResult);

            // Assert
            validationResult.IsValid.ShouldBeTrue();
            validationResult.GetValidationErrors().Count.ShouldBe(0);
        }

        [Fact]
        public void Describe_ShouldReturnExpectedDescriptors()
        {
            // Arrange
            var rule = new ClientDataVersionEmailValidationRule();

            // Act
            var descriptors = rule.Describe();

            // Assert
            descriptors.Count.ShouldBe(2);
            descriptors[0].Message.ShouldBe("Address email cannot be null or whitespace.");
            descriptors[0].Name.ShouldBe("ClientDataVersionEmailValidationRule");
            descriptors[0].Entity.ShouldBe("ClientDataVersion");

            descriptors[1].Message.ShouldBe("Address email must use only letters, digits and '.', and include '@' with a domain (for example: user@gmail.com).");
            descriptors[1].Name.ShouldBe("ClientDataVersionEmailValidationRule");
            descriptors[1].Entity.ShouldBe("ClientDataVersion");
        }

        private static ClientDataVersion CreateClientDataVersion(string addressEmail)
        {
            var address = new Address("00-001", "New York", "Main St", "10A", "2");
            return new ClientDataVersion(Guid.NewGuid(), "John Doe", address, "123456789", "48", addressEmail);
        }
    }
}
