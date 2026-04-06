using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ClientDataVersions;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Rules.ClientDataVersions
{
    public class ClientDataVersionPhoneValidationRuleTests
    {
        [Fact]
        public async Task IsValid_PhoneNumberIsWhitespace_ShouldReturnValidationError()
        {
            // Arrange
            var rule = new ClientDataVersionPhoneValidationRule();
            var validationResult = new ValidationResult();
            var clientDataVersion = CreateClientDataVersion(" ", "48");

            // Act
            await rule.IsValid(clientDataVersion, validationResult);

            // Assert
            validationResult.GetValidationErrors().Count.ShouldBe(1);
            validationResult.GetValidationErrors().First().Message.ShouldBe("Phone number cannot be null or whitespace.");
        }

        [Fact]
        public async Task IsValid_PhonePrefixHasNonDigits_ShouldReturnValidationError()
        {
            // Arrange
            var rule = new ClientDataVersionPhoneValidationRule();
            var validationResult = new ValidationResult();
            var clientDataVersion = CreateClientDataVersion("123456789", "+48");

            // Act
            await rule.IsValid(clientDataVersion, validationResult);

            // Assert
            validationResult.GetValidationErrors().Count.ShouldBe(1);
            validationResult.GetValidationErrors().First().Message.ShouldBe("Phone prefix must contain digits only.");
        }

        [Fact]
        public async Task IsValid_PhoneNumberIsZero_ShouldReturnValidationError()
        {
            // Arrange
            var rule = new ClientDataVersionPhoneValidationRule();
            var validationResult = new ValidationResult();
            var clientDataVersion = CreateClientDataVersion("0", "48");

            // Act
            await rule.IsValid(clientDataVersion, validationResult);

            // Assert
            validationResult.GetValidationErrors().Count.ShouldBe(1);
            validationResult.GetValidationErrors().First().Message.ShouldBe("Phone number must be greater than 0.");
        }

        [Fact]
        public async Task IsValid_PhoneNumberAndPrefixAreValid_ShouldReturnNoErrors()
        {
            // Arrange
            var rule = new ClientDataVersionPhoneValidationRule();
            var validationResult = new ValidationResult();
            var clientDataVersion = CreateClientDataVersion("123456789", "48");

            // Act
            await rule.IsValid(clientDataVersion, validationResult);

            // Assert
            validationResult.IsValid.ShouldBeTrue();
            validationResult.GetValidationErrors().Count.ShouldBe(0);
        }

        [Fact]
        public void Describe_ShouldReturnAllPhoneRuleDescriptors()
        {
            // Arrange
            var rule = new ClientDataVersionPhoneValidationRule();

            // Act
            var descriptors = rule.Describe();

            // Assert
            descriptors.Count.ShouldBe(6);
            descriptors.ShouldContain(d => d.Name == "ClientDataVersionPhoneValidationRule" && d.Message == "Phone number cannot be null or whitespace.");
            descriptors.ShouldContain(d => d.Name == "ClientDataVersionPhoneValidationRule" && d.Message == "Phone prefix must be greater than 0.");
            descriptors.ShouldAllBe(d => d.Entity == "ClientDataVersion");
        }

        private static ClientDataVersion CreateClientDataVersion(string phoneNumber, string phonePrefix)
        {
            var address = new Address("00-001", "New York", "Main St", "10A", "2");
            return new ClientDataVersion(Guid.NewGuid(), "John Doe", address, phoneNumber, phonePrefix, "client@example.com");
        }
    }
}
