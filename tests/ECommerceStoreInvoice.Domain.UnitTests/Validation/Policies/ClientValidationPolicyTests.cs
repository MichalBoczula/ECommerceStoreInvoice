using ECommerceStoreInvoice.Domain.Validation.Concrete.Policies;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Policies
{
    public class ClientValidationPolicyTests
    {
        [Fact]
        public async Task Validate_ClientIdIsEmpty_ShouldReturnValidationError()
        {
            // Arrange
            var policy = new ClientValidationPolicy();

            // Act
            var result = await policy.Validate(Guid.Empty);

            // Assert
            result.GetValidationErrors().Count.ShouldBe(1);
            result.GetValidationErrors().ShouldContain(e => e.Name == "ClientIdIsEmptyValidationRule");
            result.GetValidationErrors().ShouldContain(e => e.Message == "ClientId cannot be empty Guid.");
            result.GetValidationErrors().ShouldContain(e => e.Entity == "ShoppingCart");
        }

        [Fact]
        public async Task Validate_ClientIdIsNotEmpty_ShouldBeValid()
        {
            // Arrange
            var policy = new ClientValidationPolicy();

            // Act
            var result = await policy.Validate(Guid.NewGuid());

            // Assert
            result.IsValid.ShouldBeTrue();
            result.GetValidationErrors().Count.ShouldBe(0);
        }

        [Fact]
        public void Describe_ShouldReturnPolicyWithClientRuleDescriptor()
        {
            // Arrange
            var policy = new ClientValidationPolicy();

            // Act
            var descriptor = policy.Describe();

            // Assert
            descriptor.PolicyName.ShouldBe("ClientValidationPolicy");
            descriptor.Rules.Count.ShouldBe(1);

            descriptor.Rules[0].RuleName.ShouldBe("ClientIdIsEmptyValidationRule");
            descriptor.Rules[0].Rules.Count.ShouldBe(1);
            descriptor.Rules[0].Rules[0].Message.ShouldBe("ClientId cannot be empty Guid.");
            descriptor.Rules[0].Rules[0].Name.ShouldBe("ClientIdIsEmptyValidationRule");
            descriptor.Rules[0].Rules[0].Entity.ShouldBe("ShoppingCart");
        }
    }
}
