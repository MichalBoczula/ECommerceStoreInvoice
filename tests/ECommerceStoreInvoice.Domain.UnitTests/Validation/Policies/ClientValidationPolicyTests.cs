using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ShoppingCarts;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Policies
{
    public class ClientValidationPolicyTests
    {
        [Fact]
        public async Task Validate_GuidIsEmpty_ShouldReturnError()
        {
            //Arrange
            var policy = new ClientIdIsEmptyValidationRule();
            var validationResult = new ValidationResult();

            //Act
            await policy.IsValid(Guid.Empty, validationResult);

            //Assert
            validationResult.GetValidationErrors().Count.ShouldBe(1);
            validationResult.GetValidationErrors().ShouldContain(e => e.Name == "ClientIdIsEmptyValidationRule");
        }

        [Fact]
        public async Task Validate_GuidIsNotEmpty_ShouldBeValid()
        {
            //Arrange
            var policy = new ClientIdIsEmptyValidationRule();
            var validationResult = new ValidationResult();

            //Act
            await policy.IsValid(Guid.NewGuid(), validationResult);

            //Assert
            validationResult.GetValidationErrors().Count.ShouldBe(0);
        }

        [Fact]
        public void Describe_ShouldReturnDescriptionForAllRules()
        {
            //Arrange
            var policy = new ClientIdIsEmptyValidationRule();

            //Act
            var result = policy.Describe();

            //Assert
            result.Count.ShouldBe(1);
            result[0].Name.ShouldBe("ClientIdIsEmptyValidationRule");
            result[0].Message.ShouldBe("ClientId cannot be empty Guid.");
            result[0].Entity.ShouldBe("ShoppingCart");
        }
    }
}