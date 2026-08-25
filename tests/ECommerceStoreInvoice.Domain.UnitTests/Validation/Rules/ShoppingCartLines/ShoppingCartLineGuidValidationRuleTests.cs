using Shouldly;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.ShoppingCarts;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Rules.ShoppingCartLines
{
    public class ShoppingCartLineGuidValidationRuleTests
    {
        [Fact]
        public async Task IsValid_ShoppingCartLineIdIsEmpty_ShouldReturnError()
        {
            //Arrange
            var rule = new ShoppingCartLineGuidValidationRule();
            var validationResult = new ValidationResult();

            var shoppingCartLine = new ShoppingCartLine(Guid.Empty, 1);

            //Act
            await rule.IsValid(shoppingCartLine, validationResult);

            //Assert
            validationResult.GetValidationErrors().Count.ShouldBe(1);

            var error = validationResult.GetValidationErrors().First();
            error.Message.ShouldContain("ShoppingCartLine Id cannot be empty Guid.");
            error.Name.ShouldContain("ShoppingCartLineGuidValidationRule");
            error.Entity.ShouldContain("ShoppingCartLine");
        }

        [Fact]
        public async Task IsValid_ShoppingCartLineIdIsNotEmpty_ShouldNotReturnError()
        {
            //Arrange
            var rule = new ShoppingCartLineGuidValidationRule();
            var validationResult = new ValidationResult();

            var shoppingCartLine = new ShoppingCartLine(Guid.NewGuid(), 1);

            //Act
            await rule.IsValid(shoppingCartLine, validationResult);

            //Assert
            validationResult.GetValidationErrors().Count.ShouldBe(0);
        }

        [Fact]
        public void Describe_ShouldReturnCorrectRule()
        {
            //Arrange
            var rule = new ShoppingCartLineGuidValidationRule();

            //Act
            var result = rule.Describe();

            //Assert
            result.Count.ShouldBe(1);

            var desc = result.First();
            desc.Message.ShouldBe("ShoppingCartLine Id cannot be empty Guid.");
            desc.Name.ShouldBe("ShoppingCartLineGuidValidationRule");
            desc.Entity.ShouldBe("ShoppingCartLine");
        }
    }
}