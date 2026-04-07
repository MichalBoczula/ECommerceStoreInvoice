using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.Orders;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Rules.Orders
{
    public class InvoiceOrderStatusValidationRuleTests
    {
        [Fact]
        public async Task IsValid_WhenOrderStatusIsPaid_ShouldReturnNoErrors()
        {
            var rule = new InvoiceOrderStatusValidationRule();
            var validationResult = new ValidationResult();
            var order = CreateOrder(OrderStatus.Paid);

            await rule.IsValid(new InvoiceOrderStatusValidationContext(order), validationResult);

            validationResult.IsValid.ShouldBeTrue();
            validationResult.GetValidationErrors().Count.ShouldBe(0);
        }

        [Theory]
        [InlineData(OrderStatus.Created)]
        [InlineData(OrderStatus.Cancelled)]
        public async Task IsValid_WhenOrderStatusIsNotPaid_ShouldReturnError(OrderStatus orderStatus)
        {
            var rule = new InvoiceOrderStatusValidationRule();
            var validationResult = new ValidationResult();
            var order = CreateOrder(orderStatus);

            await rule.IsValid(new InvoiceOrderStatusValidationContext(order), validationResult);

            validationResult.IsValid.ShouldBeFalse();
            validationResult.GetValidationErrors().Count.ShouldBe(1);
            validationResult.GetValidationErrors().First().Name.ShouldBe("InvoiceOrderStatusValidationRule");
        }

        private static Order CreateOrder(OrderStatus status)
        {
            var line = new OrderLine(Guid.NewGuid(), "Keyboard", "Logi", new Money(100, "USD"), 1);

            return Order.Rehydrate(
                Guid.NewGuid(),
                Guid.NewGuid(),
                [line],
                DateTime.UtcNow,
                DateTime.UtcNow,
                status,
                new Money(100, "USD"));
        }
    }
}
