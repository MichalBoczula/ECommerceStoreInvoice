using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Policies;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Policies.Orders
{
    public class InvoiceValidationPolicyTests
    {
        [Fact]
        public async Task Validate_WhenOrderIsPaid_ShouldReturnNoErrors()
        {
            var policy = new InvoiceValidationPolicy();
            var order = CreateOrder(OrderStatus.Paid);

            var result = await policy.Validate(new InvoiceOrderStatusValidationContext(order));

            result.IsValid.ShouldBeTrue();
            result.GetValidationErrors().Count.ShouldBe(0);
        }

        [Theory]
        [InlineData(OrderStatus.Created)]
        [InlineData(OrderStatus.Cancelled)]
        public async Task Validate_WhenOrderIsNotPaid_ShouldReturnValidationError(OrderStatus status)
        {
            var policy = new InvoiceValidationPolicy();
            var order = CreateOrder(status);

            var result = await policy.Validate(new InvoiceOrderStatusValidationContext(order));

            result.IsValid.ShouldBeFalse();
            result.GetValidationErrors().Count.ShouldBe(1);
            result.GetValidationErrors().First().Name.ShouldBe("InvoiceOrderStatusValidationRule");
        }

        [Fact]
        public void Describe_ShouldIncludeUnderlyingRule()
        {
            var policy = new InvoiceValidationPolicy();

            var descriptor = policy.Describe();

            descriptor.PolicyName.ShouldBe("InvoiceValidationPolicy");
            descriptor.Rules.Count.ShouldBe(1);
            descriptor.Rules[0].RuleName.ShouldBe("InvoiceOrderStatusValidationRule");
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
