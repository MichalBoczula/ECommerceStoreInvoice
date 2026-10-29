namespace ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders
{
    /// <summary>An order line tied to its immutable product snapshot.</summary>
    public sealed record OrderLineResponseDto
    {
        /// <summary>Immutable product snapshot UUID.</summary>
        public required Guid ProductVersionId { get; init; }
        /// <summary>Exact ordered int32 quantity.</summary>
        public required int Quantity { get; init; }
        /// <summary>Complete product snapshot captured at checkout; later catalog changes do not alter it.</summary>
        public required ProductVersionResponseDto ProductVersion { get; init; }
        /// <summary>Snapshot unit-price amount multiplied by quantity, serialized as a JSON number.</summary>
        public required decimal LineTotalAmount { get; init; }
    }
}