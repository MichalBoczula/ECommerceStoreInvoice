using ECommerceStoreInvoice.Infrastructure.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ECommerceStoreInvoice.Infrastructure.Health;

internal sealed class MongoReadinessHealthCheck(
    IMongoClient client,
    IOptions<MongoDbSettings> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext healthCheckContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var admin = client.GetDatabase("admin");
            var hello = await admin.RunCommandAsync<BsonDocument>(
                new BsonDocument("hello", 1), cancellationToken: cancellationToken);

            if (!SupportsTransactionTopology(hello))
            {
                return HealthCheckResult.Unhealthy("MongoDB writable replica set or router with sessions is unavailable.");
            }

            await client.GetDatabase(options.Value.DatabaseName).RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1), cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy("MongoDB is unavailable for transactions.");
        }
    }

    internal static bool SupportsTransactionTopology(BsonDocument hello)
    {
        var replicaSet = hello.TryGetValue("setName", out var setName) && setName.IsString &&
            !string.IsNullOrWhiteSpace(setName.AsString);
        var router = hello.TryGetValue("msg", out var message) && message.IsString && message.AsString == "isdbgrid";
        return (replicaSet || router) &&
            hello.TryGetValue("isWritablePrimary", out var primary) && primary.IsBoolean && primary.AsBoolean &&
            hello.TryGetValue("logicalSessionTimeoutMinutes", out var sessions) && sessions.IsNumeric && sessions.ToDouble() > 0;
    }
}
