namespace ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;

public interface IOrderWriteTransaction
{
    Task BeginAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync();
}
