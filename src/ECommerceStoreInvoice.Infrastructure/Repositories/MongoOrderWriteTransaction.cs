using System.Diagnostics;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Infrastructure.Context;
using MongoDB.Driver;

namespace ECommerceStoreInvoice.Infrastructure.Repositories;

internal sealed class MongoOrderWriteTransaction(MongoDbContext context) : IOrderWriteTransaction
{
    public async Task BeginAsync(CancellationToken cancellationToken = default)
    {
        if (context.CurrentSession is not null)
            throw new InvalidOperationException("An order write transaction is already active.");

        var session = await context.Client.StartSessionAsync(cancellationToken: cancellationToken);
        try
        {
            session.StartTransaction();
            context.CurrentSession = session;
        }
        catch
        {
            session.Dispose();
            throw;
        }
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        var session = context.CurrentSession
            ?? throw new InvalidOperationException("No order write transaction is active.");

        await session.CommitTransactionAsync(cancellationToken);
        context.CurrentSession = null;
        DisposeSession(session);
    }

    public async Task RollbackAsync()
    {
        var session = context.CurrentSession;
        if (session is null)
            return;

        context.CurrentSession = null;
        try
        {
            if (session.IsInTransaction)
            {
                using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await session.AbortTransactionAsync(cleanup.Token);
            }
        }
        catch (Exception exception)
        {
            try
            {
                Trace.TraceWarning("Order transaction rollback failed: {0}", exception.GetType().Name);
            }
            catch
            {
                // A diagnostic listener must not replace the original write failure.
            }
        }
        finally
        {
            DisposeSession(session);
        }
    }

    private static void DisposeSession(IClientSessionHandle session)
    {
        try
        {
            session.Dispose();
        }
        catch (Exception exception)
        {
            try
            {
                Trace.TraceWarning("Order transaction session disposal failed: {0}", exception.GetType().Name);
            }
            catch
            {
                // Preserve the original write or commit result.
            }
        }
    }
}
