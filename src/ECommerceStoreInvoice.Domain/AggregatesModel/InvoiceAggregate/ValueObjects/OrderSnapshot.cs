using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;

namespace ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.ValueObjects
{
    public readonly record struct OrderSnapshot
    {
        public IReadOnlyCollection<OrderLineSnapshot> OrderLines { get; init; }
        public Money Total { get; init; }

        public OrderSnapshot(IEnumerable<OrderLineSnapshot> orderLines)
        {
            OrderLines = orderLines.ToList().AsReadOnly();
            Total = TotalCost();
        }

        internal Money TotalCost()
        {
            var currency = OrderLines.First().Total.Currency;
            var amount = OrderLines.Sum(op => op.Total.Amount);
            return new Money(amount, currency);
        }
    }
}
