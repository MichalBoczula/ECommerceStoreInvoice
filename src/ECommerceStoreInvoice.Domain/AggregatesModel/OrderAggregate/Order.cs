using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;

namespace ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate
{
    public sealed class Order
    {
        public Guid Id { get; init; }
        public Guid ClientId { get; init; }
        public IReadOnlyCollection<OrderLine> Lines { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; private set; }
        public OrderStatus Status { get; private set; }
        public Money Total { get; private set; }

        public Order(Guid clientId, IReadOnlyCollection<OrderLine> lines)
        {
            Id = Guid.NewGuid();
            ClientId = clientId;
            Lines = lines;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow; 
            Status = OrderStatus.Created;
            Total = new(Lines.Sum(x => x.Total.Amount), Lines.FirstOrDefault()?.Total.Currency ?? "USD");
        }

        public void ChangeOrderStatus(OrderStatus status)
        {
            Status = status;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
