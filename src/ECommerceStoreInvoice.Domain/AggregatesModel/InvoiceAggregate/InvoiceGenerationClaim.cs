namespace ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;

public sealed record InvoiceGenerationClaim(Guid InvoiceId, Guid OrderId, Guid ClientDataVersionId, Guid AttemptId);
