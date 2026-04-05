using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Policies;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Policies.ClientDataVersions
{
    public class ClientDataVersionValidationPolicyTests
    {
        [Fact]
        public async Task Validate_ClientDataVersionWithInvalidPhoneAndEmail_ShouldReturnErrors()
        {
            // Arrange
            var policy = new ClientDataVersionValidationPolicy();
            var invalidClientDataVersion = CreateClientDataVersion(" ", " ", "not-an-email");

            // Act
            var result = await policy.Validate(invalidClientDataVersion);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.GetValidationErrors().Count.ShouldBe(3);
            result.GetValidationErrors().ShouldContain(e => e.Name == "ClientDataVersionPhoneValidationRule" && e.Message == "Phone number cannot be null or whitespace.");
            result.GetValidationErrors().ShouldContain(e => e.Name == "ClientDataVersionPhoneValidationRule" && e.Message == "Phone prefix cannot be null or whitespace.");
            result.GetValidationErrors().ShouldContain(e => e.Name == "ClientDataVersionEmailValidationRule");
        }

        [Fact]
        public async Task Validate_ClientDataVersionIsValid_ShouldReturnNoErrors()
        {
            // Arrange
            var policy = new ClientDataVersionValidationPolicy();
            var validClientDataVersion = CreateClientDataVersion("123456789", "48", "client@example.com");

            // Act
            var result = await policy.Validate(validClientDataVersion);

            // Assert
            result.IsValid.ShouldBeTrue();
            result.GetValidationErrors().Count.ShouldBe(0);
        }

        [Fact]
        public void Describe_ShouldIncludeAllUnderlyingRuleDescriptors()
        {
            // Arrange
            var policy = new ClientDataVersionValidationPolicy();

            // Act
            var descriptor = policy.Describe();

            // Assert
            descriptor.PolicyName.ShouldBe("ClientDataVersionValidationPolicy");
            descriptor.Rules.Count.ShouldBe(2);
            descriptor.Rules.ShouldContain(r => r.RuleName == "ClientDataVersionPhoneValidationRule");
            descriptor.Rules.ShouldContain(r => r.RuleName == "ClientDataVersionEmailValidationRule");
        }

        private static ClientDataVersion CreateClientDataVersion(string phoneNumber, string phonePrefix, string addressEmail)
        {
            var address = new Address("00-001", "New York", "Main St", "10A", "2");
            return new ClientDataVersion(Guid.NewGuid(), address, phoneNumber, phonePrefix, addressEmail);
        }
    }
}
