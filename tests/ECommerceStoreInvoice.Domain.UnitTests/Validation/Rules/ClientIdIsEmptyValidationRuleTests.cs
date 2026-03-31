using Shouldly;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.Common;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Rules.Common
{
    public class ClientIdIsEmptyValidationRuleTests
    {
        [Fact]
        public async Task IsValid_GuidIsEmpty_ShouldReturnError()
        {
            //Arrange
            var rule = new ClientIdIsEmptyValidationRule("ShoppingCart");
            var validationResult = new ValidationResult();

            //Act
            await rule.IsValid(Guid.Empty, validationResult);

            //Assert
            validationResult.GetValidationErrors().Count.ShouldBe(1);

            var error = validationResult.GetValidationErrors().First();
            error.Message.ShouldContain("ClientId cannot be empty Guid.");
            error.Name.ShouldContain("ClientIdIsEmptyValidationRule");
            error.Entity.ShouldContain("ShoppingCart");
        }

        [Fact]
        public async Task IsValid_GuidIsNotEmpty_ShouldNotReturnError()
        {
            //Arrange
            var rule = new ClientIdIsEmptyValidationRule("ShoppingCart");
            var validationResult = new ValidationResult();

            //Act
            await rule.IsValid(Guid.NewGuid(), validationResult);

            //Assert
            validationResult.GetValidationErrors().Count.ShouldBe(0);
        }

        [Fact]
        public void Describe_ShouldReturnCorrectRule()
        {
            //Arrange
            var rule = new ClientIdIsEmptyValidationRule("ShoppingCart");

            //Act
            var result = rule.Describe();

            //Assert
            result.Count.ShouldBe(1);

            var desc = result.First();
            desc.Message.ShouldBe("ClientId cannot be empty Guid.");
            desc.Name.ShouldBe("ClientIdIsEmptyValidationRule");
            desc.Entity.ShouldBe("ShoppingCart");
        }
    }
}