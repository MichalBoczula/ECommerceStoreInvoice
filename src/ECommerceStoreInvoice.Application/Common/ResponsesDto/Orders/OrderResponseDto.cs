namespace ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders
{
    /// <summary>Complete order built from immutable product snapshots. All members are emitted; updatedAt may be null.</summary>
    public sealed record OrderResponseDto
    {
        /// <summary>Order UUID.</summary>
        public required Guid Id { get; init; }
        /// <summary>Client UUID owning the order.</summary>
        public required Guid ClientId { get; init; }
        /// <summary>UTC order creation timestamp.</summary>
        public required DateTime CreatedAt { get; init; }
        /// <summary>UTC status-change timestamp, or null before a status update.</summary>
        public DateTime? UpdatedAt { get; init; }
        /// <summary>Canonical stored status: Created, Paid or Cancelled.</summary>
        public required string Status { get; init; }
        /// <summary>Decimal total of snapshot line amounts, serialized as a JSON number rather than integer minor units.</summary>
        public required decimal TotalAmount { get; init; }
        /// <summary>Currency taken from the first snapshot line. The current model does not enforce a single currency across all lines.</summary>
        public required string TotalCurrency { get; init; }
        /// <summary>Complete ordered snapshot-backed order lines.</summary>
        public required IReadOnlyCollection<OrderLineResponseDto> Lines { get; init; }
    }
}