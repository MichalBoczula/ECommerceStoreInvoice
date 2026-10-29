using System.Collections.Concurrent;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Infrastructure.Context;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ECommerceStoreInvoice.Acceptance.Tests;

internal sealed record OrderWriteObservation(string Attempt, Guid ScopeId, string Stage, string? SessionId, bool SessionActive,
    string? ExceptionType, Dictionary<string, BsonDocument[]>? Inside, Dictionary<string, BsonDocument[]> Outside);

/// <summary>Bounded test gates around actual repositories/sessions; never replaces a write or transaction.</summary>
internal sealed class OrderConcurrencyProbe(Action<string, object> record, Func<Task<Dictionary<string, BsonDocument[]>>> snapshot)
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource> _arrived = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource> _release = new();
    internal ConcurrentQueue<OrderWriteObservation> Events { get; } = new();
    internal string Mode { get; private set; } = string.Empty;
    internal string FailureStage { get; private set; } = string.Empty;
    internal Guid TargetId { get; private set; }
    internal void Configure(string mode, Guid targetId, string failureStage = "") { Mode = mode; TargetId = targetId; FailureStage = failureStage; }
    private static TaskCompletionSource Gate() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal Task WaitAsync(string attempt) => _arrived.GetOrAdd(attempt, _ => Gate()).Task.WaitAsync(TimeSpan.FromSeconds(45));
    internal void Resume(string attempt) => _release.GetOrAdd(attempt, _ => Gate()).TrySetResult();
    internal void ResumeAll() { foreach (var key in new[] { "first", "second" }) Resume(key); }
    internal async Task HoldAsync(string attempt)
    {
        _arrived.GetOrAdd(attempt, _ => Gate()).TrySetResult();
        await _release.GetOrAdd(attempt, _ => Gate()).Task.WaitAsync(TimeSpan.FromSeconds(45));
    }
    internal async Task ObserveAsync(OrderProbeScope scope, MongoDbContext context, string stage, Exception? exception = null)
    {
        var session = context.CurrentSession;
        Dictionary<string, BsonDocument[]>? inside = null;
        if (exception is null && session?.IsInTransaction == true)
        {
            inside = new(StringComparer.Ordinal);
            foreach (var name in new[] { "shoppingCarts", "orders", "productVersions", "invoices", "clientDataVersions" })
            {
                using var cursor = await context.Database.GetCollection<BsonDocument>(name).FindAsync(session, FilterDefinition<BsonDocument>.Empty);
                inside[name] = (await cursor.ToListAsync()).OrderBy(document => document["_id"].AsBsonBinaryData.ToGuid()).ToArray();
            }
        }
        var value = new OrderWriteObservation(scope.Attempt, scope.Id, stage, session?.ServerSession.Id.ToJson(),
            session?.IsInTransaction == true, exception?.GetType().FullName, inside, await snapshot());
        Events.Enqueue(value);
        record("Observed real order write stage", new { value.Attempt, value.ScopeId, value.Stage, value.SessionId, value.SessionActive, value.ExceptionType,
            inside = inside is null ? null : InvoiceGenerationProbe.JsonSnapshot(inside), outside = InvoiceGenerationProbe.JsonSnapshot(value.Outside) });
    }
    internal async Task AfterStageAsync(OrderProbeScope scope, MongoDbContext context, string stage)
    {
        await ObserveAsync(scope, context, stage);
        if (Mode == "rollback" && stage == FailureStage)
        {
            await HoldAsync(scope.Attempt);
            throw new IOException("INV11_PRIVATE_FAILURE_AFTER_REAL_" + stage);
        }
    }
}

internal sealed class OrderProbeScope(IHttpContextAccessor accessor)
{
    internal Guid Id { get; } = Guid.NewGuid();
    internal string Attempt { get; } = accessor.HttpContext?.Request.Headers["X-Bdd-Attempt"].ToString() ?? string.Empty;
    internal bool Active => Attempt is "first" or "second";
}

internal sealed class ObservedOrderRepository(IOrderRepository inner, MongoDbContext context, OrderConcurrencyProbe probe, OrderProbeScope scope) : IOrderRepository
{
    public async Task<Order> CreateOrder(Order order)
    {
        var result = await inner.CreateOrder(order);
        if (scope.Active) await probe.AfterStageAsync(scope, context, "Order");
        return result;
    }
    public async Task<Order> UpdateOrder(Order order)
    {
        if (!scope.Active || probe.Mode != "status" || order.Id != probe.TargetId) return await inner.UpdateOrder(order);
        await probe.ObserveAsync(scope, context, "StatusValidated");
        await probe.HoldAsync(scope.Attempt);
        try
        {
            var result = await inner.UpdateOrder(order);
            await probe.ObserveAsync(scope, context, "StatusCommitted");
            return result;
        }
        catch (Exception exception) { await probe.ObserveAsync(scope, context, "StatusRejected", exception); throw; }
    }
    public Task<Order?> GetOrderByOrderId(Guid id) => inner.GetOrderByOrderId(id);
    public Task<IReadOnlyCollection<Order>> GetOrdersByClientId(Guid id) => inner.GetOrdersByClientId(id);
    public async Task<(Order Order, IReadOnlyCollection<ProductVersion> ProductVersions)?> GetOrderWithProductVersionsById(Guid id)
    {
        var result = await inner.GetOrderWithProductVersionsById(id);
        if (scope.Active && probe.Mode == "status" && id == probe.TargetId && result is { } loaded)
            await probe.ObserveAsync(scope, context, "ReadCreated:" + loaded.Order.Status);
        return result;
    }
    public Task<IReadOnlyCollection<(Order Order, IReadOnlyCollection<ProductVersion> ProductVersions)>> GetOrdersWithProductVersionsByClientId(Guid id) => inner.GetOrdersWithProductVersionsByClientId(id);
}

internal sealed class ObservedProductVersionRepository(IProductVersionRepository inner, MongoDbContext context, OrderConcurrencyProbe probe, OrderProbeScope scope) : IProductVersionRepository
{
    public async Task<IReadOnlyCollection<ProductVersion>> CreateProductVersions(IReadOnlyCollection<ProductVersion> versions)
    {
        var result = await inner.CreateProductVersions(versions);
        if (scope.Active) await probe.AfterStageAsync(scope, context, "Snapshots");
        return result;
    }
    public Task<ProductVersion> CreateProductVersion(ProductVersion version) => inner.CreateProductVersion(version);
    public Task<ProductVersion?> GetProductVersionById(Guid id) => inner.GetProductVersionById(id);
    public Task<IReadOnlyCollection<ProductVersion>> GetProductVersionsByIds(IReadOnlyCollection<Guid> ids) => inner.GetProductVersionsByIds(ids);
}

internal sealed class ObservedShoppingCartRepository(IShoppingCartRepository inner, MongoDbContext context, OrderConcurrencyProbe probe, OrderProbeScope scope) : IShoppingCartRepository
{
    public Task<ShoppingCart?> GetShoppingCartByClientId(Guid id) => inner.GetShoppingCartByClientId(id);
    public Task<ShoppingCart> CreateShoppingCart(ShoppingCart cart) => inner.CreateShoppingCart(cart);
    public async Task<ShoppingCart> UpdateShoppingCart(ShoppingCart cart)
    {
        if (scope.Active && probe.Mode == "checkout" && cart.ClientId == probe.TargetId)
        {
            await probe.ObserveAsync(scope, context, "BeforeCart");
            await probe.HoldAsync(scope.Attempt);
        }
        try
        {
            var result = await inner.UpdateShoppingCart(cart);
            if (scope.Active) await probe.AfterStageAsync(scope, context, "Cart");
            return result;
        }
        catch (MongoException exception) { if (scope.Active) await probe.ObserveAsync(scope, context, "CartWriteConflict", exception); throw; }
    }
}

internal sealed class ObservedOrderTransaction(IOrderWriteTransaction inner, MongoDbContext context, OrderConcurrencyProbe probe, OrderProbeScope scope) : IOrderWriteTransaction
{
    public async Task BeginAsync(CancellationToken token = default) { await inner.BeginAsync(token); if (scope.Active) await probe.ObserveAsync(scope, context, "Begin"); }
    public async Task CommitAsync(CancellationToken token = default) { await inner.CommitAsync(token); if (scope.Active) await probe.ObserveAsync(scope, context, "Commit"); }
    public async Task RollbackAsync() { await inner.RollbackAsync(); if (scope.Active) await probe.ObserveAsync(scope, context, "Rollback"); }
}
