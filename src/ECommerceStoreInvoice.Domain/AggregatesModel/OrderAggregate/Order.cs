using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;

namespace ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate
{
    public sealed class Order
    {
        public Guid Id { get; }
        public Guid ClientId { get; }
        public IReadOnlyCollection<OrderLine> Lines { get; }
        public DateTime CreatedAt { get; }
        public OrderStatus Status { get; private set; }
        public string Currency { get; }
        public decimal TotalAmount { get; }

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
