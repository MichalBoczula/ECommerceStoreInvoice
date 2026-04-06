using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ClientDataVersions;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Rules.ClientDataVersions
{
    public class ClientDataVersionAddressValidationRuleTests
    {
        [Fact]
        public async Task IsValid_PostalCodeIsWhitespace_ShouldReturnValidationError()
        {
            // Arrange
            var rule = new ClientDataVersionAddressValidationRule();
            var validationResult = new ValidationResult();
            var clientDataVersion = CreateClientDataVersion(new Address(" ", "NewYork", "Main.St", "10A", "2"));

            // Act
            await rule.IsValid(clientDataVersion, validationResult);

            // Assert
            validationResult.GetValidationErrors().Count.ShouldBe(1);
            validationResult.GetValidationErrors().First().Message.ShouldBe("Postal code cannot be null or whitespace.");
        }

        [Theory]
        [InlineData("00001")]
        [InlineData("aa-123")]
        [InlineData("0-000")]
        public async Task IsValid_PostalCodeHasInvalidFormat_ShouldReturnValidationError(string postalCode)
        {
            // Arrange
            var rule = new ClientDataVersionAddressValidationRule();
            var validationResult = new ValidationResult();
            var clientDataVersion = CreateClientDataVersion(new Address(postalCode, "NewYork", "Main.St", "10A", "2"));

            // Act
            await rule.IsValid(clientDataVersion, validationResult);

            // Assert
            validationResult.GetValidationErrors().Count.ShouldBe(1);
            validationResult.GetValidationErrors().First().Message.ShouldBe("Postal code must match pattern nn-nnn (for example: 00-001).");
        }

        [Theory]
        [InlineData("New York")]
        [InlineData("Main-St")]
        [InlineData("10/2")]
        [InlineData("2#")]
        public async Task IsValid_AddressFieldContainsDisallowedCharacters_ShouldReturnValidationError(string invalidValue)
        {
            // Arrange
            var rule = new ClientDataVersionAddressValidationRule();
            var validationResult = new ValidationResult();
            var address = new Address("00-001", invalidValue, "Main.St", "10A", "2");
            var clientDataVersion = CreateClientDataVersion(address);

            // Act
            await rule.IsValid(clientDataVersion, validationResult);

            // Assert
            validationResult.IsValid.ShouldBeFalse();
            validationResult.GetValidationErrors().Count.ShouldBe(1);
            validationResult.GetValidationErrors().First().Name.ShouldBe("ClientDataVersionAddressValidationRule");
        }

        [Fact]
        public async Task IsValid_OptionalAddressFieldsAreWhitespace_ShouldReturnNoErrors()
        {
            // Arrange
            var rule = new ClientDataVersionAddressValidationRule();
            var validationResult = new ValidationResult();
            var address = new Address("00-001", " ", "", "  ", "");
            var clientDataVersion = CreateClientDataVersion(address);

            // Act
            await rule.IsValid(clientDataVersion, validationResult);

            // Assert
            validationResult.IsValid.ShouldBeTrue();
            validationResult.GetValidationErrors().Count.ShouldBe(0);
        }

        [Fact]
        public async Task IsValid_AddressFieldsContainAllowedCharacters_ShouldReturnNoErrors()
        {
            // Arrange
            var rule = new ClientDataVersionAddressValidationRule();
            var validationResult = new ValidationResult();
            var address = new Address("01-234", "NewYork,NY", "Main.St", "10A", "2");
            var clientDataVersion = CreateClientDataVersion(address);

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
            var rule = new ClientDataVersionAddressValidationRule();

            // Act
            var descriptors = rule.Describe();

            // Assert
            descriptors.Count.ShouldBe(6);
            descriptors.ShouldAllBe(d => d.Name == "ClientDataVersionAddressValidationRule");
            descriptors.ShouldAllBe(d => d.Entity == "ClientDataVersion");
            descriptors.ShouldContain(d => d.Message == "Postal code cannot be null or whitespace.");
            descriptors.ShouldContain(d => d.Message == "Postal code must match pattern nn-nnn (for example: 00-001).");
        }

        private static ClientDataVersion CreateClientDataVersion(Address address)
        {
            return new ClientDataVersion(Guid.NewGuid(), "John Doe", address, "123456789", "48", "client@example.com");
        }
    }
}
