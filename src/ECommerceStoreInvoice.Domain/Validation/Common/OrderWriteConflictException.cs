namespace ECommerceStoreInvoice.Domain.Validation.Common;

public sealed class OrderWriteConflictException(Guid orderId)
    : Exception($"Order '{orderId}' was changed by another request.")
{
    public Guid OrderId { get; } = orderId;
}
