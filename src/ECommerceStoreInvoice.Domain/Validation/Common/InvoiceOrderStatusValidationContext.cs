using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;

namespace ECommerceStoreInvoice.Domain.Validation.Common
{
    public readonly record struct InvoiceOrderStatusValidationContext(Order Order);
}
