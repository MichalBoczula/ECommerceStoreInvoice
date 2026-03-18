using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;

namespace ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate
{
    public sealed class Order
    {
        public Guid Id { get; init; }
        public Guid ClientId { get; init; }
        public IReadOnlyCollection<OrderLine> Lines { get; init; }
        public DateTime CreatedAt { get; init; }
        public OrderStatus Status { get; private set; }
        public string Currency { get; init; }
        public decimal TotalAmount { get; init; }

        public Order(Guid clientId, IEnumerable<OrderLine> lines, string currency)
        {
            var orderLines = lines.ToList();
            Id = Guid.NewGuid();
            ClientId = clientId;
            Lines = orderLines.AsReadOnly();
            CreatedAt = DateTime.UtcNow;
            Status = OrderStatus.Created;
            Currency = currency;
            TotalAmount = orderLines.Sum(x => x.TotalAmount);
        }

        public void MarkAsPaid()
        {
            Status = OrderStatus.Paid;
        }

        public void Cancel()
        {
            Status = OrderStatus.Cancelled;
        }
    }
}
