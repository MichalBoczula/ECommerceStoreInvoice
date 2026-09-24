using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;
using ECommerceStoreInvoice.Infrastructure.Context;
using ECommerceStoreInvoice.Infrastructure.Mapping;
using ECommerceStoreInvoice.Infrastructure.Persistence.Invoices;
using MongoDB.Driver;

namespace ECommerceStoreInvoice.Infrastructure.Repositories;

internal sealed class InvoiceGenerationRepository(MongoDbContext context) : IInvoiceGenerationRepository
{
    private static readonly TimeSpan Lease = TimeSpan.FromMinutes(10);

    public async Task<InvoiceGenerationClaim?> TryClaimAsync(Invoice reservation, Guid attemptId)
    {
        var now = DateTime.UtcNow;
        var document = InvoiceMapping.MapToDocument(reservation) with
        {
            GenerationStatus = "Generating",
            GenerationAttemptId = attemptId.ToString("N"),
            GenerationLeaseUntil = now.Add(Lease)
        };

        try
        {
            await context.Invoices.InsertOneAsync(document);
            return new InvoiceGenerationClaim(document.Id, document.OrderId, document.ClientDataVersionId, attemptId);
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // An existing claim can only be reused after failure or after its lease expires.
        }

        var filter = Builders<InvoiceDocument>.Filter;
        var eligible = filter.And(
            filter.Eq(x => x.OrderId, reservation.OrderId),
            filter.Or(
                filter.Eq(x => x.GenerationStatus, "Failed"),
                filter.And(
                    filter.Eq(x => x.GenerationStatus, "Generating"),
                    filter.Lte(x => x.GenerationLeaseUntil, now))));
        var update = Builders<InvoiceDocument>.Update
            .Set(x => x.GenerationStatus, "Generating")
            .Set(x => x.GenerationAttemptId, attemptId.ToString("N"))
            .Set(x => x.GenerationLeaseUntil, now.Add(Lease))
            .Set(x => x.ClientDataVersionId, reservation.ClientDataVersionId)
            .Set(x => x.StorageUrl, string.Empty);
        var claimed = await context.Invoices.FindOneAndUpdateAsync(
            eligible, update, new FindOneAndUpdateOptions<InvoiceDocument> { ReturnDocument = ReturnDocument.After });

        return claimed is null
            ? null
            : new InvoiceGenerationClaim(claimed.Id, claimed.OrderId, claimed.ClientDataVersionId, attemptId);
    }

    public async Task<Invoice> CompleteAsync(InvoiceGenerationClaim claim, string storageUrl)
    {
        var filter = Builders<InvoiceDocument>.Filter;
        var owner = filter.And(
            filter.Eq(x => x.Id, claim.InvoiceId),
            filter.Eq(x => x.GenerationStatus, "Generating"),
            filter.Eq(x => x.GenerationAttemptId, claim.AttemptId.ToString("N")));
        var update = Builders<InvoiceDocument>.Update
            .Set(x => x.StorageUrl, storageUrl)
            .Set(x => x.GenerationStatus, "Completed")
            .Set(x => x.GenerationAttemptId, null)
            .Set(x => x.GenerationLeaseUntil, null);
        var completed = await context.Invoices.FindOneAndUpdateAsync(
            owner, update, new FindOneAndUpdateOptions<InvoiceDocument> { ReturnDocument = ReturnDocument.After });

        if (completed is null)
            throw new InvalidOperationException($"Invoice generation claim for order '{claim.OrderId}' is no longer owned by this attempt.");

        return InvoiceMapping.MapToDomain(completed);
    }

    public async Task ReleaseAsync(InvoiceGenerationClaim claim)
    {
        var filter = Builders<InvoiceDocument>.Filter;
        var owner = filter.And(
            filter.Eq(x => x.Id, claim.InvoiceId),
            filter.Eq(x => x.GenerationStatus, "Generating"),
            filter.Eq(x => x.GenerationAttemptId, claim.AttemptId.ToString("N")));
        var update = Builders<InvoiceDocument>.Update
            .Set(x => x.GenerationStatus, "Failed")
            .Set(x => x.GenerationLeaseUntil, null);
        await context.Invoices.UpdateOneAsync(owner, update);
    }
}
