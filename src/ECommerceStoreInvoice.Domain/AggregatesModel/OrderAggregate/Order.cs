using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;

namespace ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate
{
    public sealed class Order
    {
        public Guid Id { get; private set; }
        public Guid ClientId { get; private set; }
        public IReadOnlyCollection<OrderLine> Lines { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public OrderStatus Status { get; private set; }

        public Order(Guid clientId, IReadOnlyCollection<OrderLine> lines)
        {
            Id = Guid.NewGuid();
            ClientId = clientId;
            Lines = lines ?? throw new ArgumentNullException(nameof(lines));
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = null;
            Status = OrderStatus.Created;
        }

        public static Order Rehydrate(
            Guid id,
            Guid clientId,
            IReadOnlyCollection<OrderLine> lines,
            DateTime createdAt,
            DateTime? updatedAt,
            OrderStatus status)
        {
            return new Order(id, clientId, lines, createdAt, updatedAt, status);
        }

        private Order(
            Guid id,
            Guid clientId,
            IReadOnlyCollection<OrderLine> lines,
            DateTime createdAt,
            DateTime? updatedAt,
            OrderStatus status)
        {
            Id = id;
            ClientId = clientId;
            Lines = lines;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            Status = status;
        }

        public void ChangeStatus(OrderStatus newStatus)
        {
            Status = newStatus;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}