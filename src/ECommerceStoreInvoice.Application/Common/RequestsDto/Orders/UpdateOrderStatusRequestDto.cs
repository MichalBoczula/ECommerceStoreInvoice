namespace ECommerceStoreInvoice.Application.Common.RequestsDto.Orders
{
    /// <summary>Requests a status transition for an existing order. The status member must be present; unknown members are ignored.</summary>
    public sealed record UpdateOrderStatusRequestDto
    {
        /// <summary>Status text parsed by the existing policy. Only Created to Paid or Created to Cancelled is a valid transition; no new transition is implied by the schema.</summary>
        public required string Status { get; init; }
    }
}
